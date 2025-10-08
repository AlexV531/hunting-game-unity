using UnityEngine;

public abstract class AttachInteractable : InteractableBase
{
    public Transform[] attachmentPoints;
    private bool[] isAttachmentPointOccupied;
    private BalloonAttach[] attachTargets;
    [SerializeField] private int capacity = 1;

    protected override void Awake()
    {
        base.Awake();

        isAttachmentPointOccupied = new bool[capacity];
        attachTargets = new BalloonAttach[capacity];
    }

    // Returns false if no free point
    public bool AttachTarget(BalloonAttach attachTarget)
    {
        int index = GetFreeAttachmentPointIndex();
        if (index < 0)
        {
            Debug.Log("No free attach targets");
            return false;
        }

        isAttachmentPointOccupied[index] = true;
        attachTargets[index] = attachTarget;
        attachTarget.Attach(attachmentPoints[index], this);
        return true;
    }

    // Returns false if attach target is not attached
    public bool ReleaseTarget(BalloonAttach releaseTarget)
    {
        Debug.Log("Releasing target");
        for (int i = 0; i < capacity; i++)
        {
            if (attachTargets[i] == releaseTarget)
            {
                isAttachmentPointOccupied[i] = false;
                Debug.Log("bool array: " + isAttachmentPointOccupied.ToString());
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
            {
                return i;
            }
        }
        return -1;
    }
}
