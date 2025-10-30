using UnityEngine;

public abstract class AttachInteractable : InteractableBase
{
    private AttachHandler attachHandler;
    [SerializeField] private Transform[] attachmentPoints;

    protected override void Awake()
    {
        base.Awake();

        attachHandler = new AttachHandler(attachmentPoints);
    }

    public bool AttachTarget(BalloonAttach target) => attachHandler.AttachTarget(target);
    public bool ReleaseTarget(BalloonAttach target) => attachHandler.ReleaseTarget(target);
    public void ReleaseAll() => attachHandler.ReleaseAll();
    public BalloonAttach GetAttached(int index) => attachHandler.GetAttached(index);
}
