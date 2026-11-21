namespace ProjectScare.Core
{
    /// <summary>
    /// Something the local player can use by pressing interact. Implementations only ask the host,
    /// which decides whether the use is allowed.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }
}
