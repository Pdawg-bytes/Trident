namespace Trident.Popups
{
    internal interface IPopup
    {
        void Open();
        void Render();
        bool IsOpen { get; }
    }
}