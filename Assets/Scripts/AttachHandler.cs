using UnityEngine;

public class AttachHandler
{
    private Transform[] attachmentPoints;
    private bool[] isAttachmentPointOccupied;
    private BalloonAttach[] attachTargets;
    private int capacity;

    public AttachHandler(Transform[] attachmentPoints)
    {
        capacity = attachmentPoints.Length;
        isAttachmentPointOccupied = new bool[capacity];
        attachTargets = new BalloonAttach[capacity];
        this.attachmentPoints = attachmentPoints;

        Debug.Log("Attachment points length: " + attachmentPoints.Length);
        Debug.Log("Attachment point index 0 position: " + attachmentPoints[0].position);
    }

    public bool AttachTarget(BalloonAttach attachTarget)
    {
        Debug.Log("Attempting attachment " + attachTarget.name);
        int index = GetFreeAttachmentPointIndex();
        if (index < 0)
        {
            Debug.Log("No free attach targets");
            return false;
        }

        if (attachmentPoints[index] == null)
            Debug.LogError("Attachment point is null!");

        isAttachmentPointOccupied[index] = true;
        attachTargets[index] = attachTarget;
        attachTarget.Attach(attachmentPoints[index], this);
        return true;
    }

    public bool ReleaseTarget(BalloonAttach releaseTarget)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (attachTargets[i] == releaseTarget)
            {
                isAttachmentPointOccupied[i] = false;
                releaseTarget.Release(true);
                attachTargets[i] = null;
                return true;
            }
        }
        return false;
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < capacity; i++)
        {
            if (attachTargets[i] != null)
            {
                isAttachmentPointOccupied[i] = false;
                attachTargets[i].Release();
                attachTargets[i] = null;
            }
        }
    }

    private int GetFreeAttachmentPointIndex()
    {
        for (int i = 0; i < capacity; i++)
        {
            if (!isAttachmentPointOccupied[i])
                return i;
        }
        return -1;
    }
}
