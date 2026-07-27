namespace CrateExpectations.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }

        bool CanInteract { get; }

        void OnFocused();

        void OnUnfocused();

        void Interact(Interactor source);
    }
}
