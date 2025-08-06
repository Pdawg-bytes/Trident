namespace Trident.Popups
{
    internal static class PopupManager
    {
        private static List<IPopup> _activePopups = [];
        private static Queue<IPopup> _pendingPopups = [];

        internal static void Show(IPopup popup) => _pendingPopups.Enqueue(popup);

        internal static void Render()
        {
            while (_pendingPopups.TryDequeue(out var popup))
            {
                popup.Open();
                _activePopups.Add(popup);
            }

            foreach (var popup in _activePopups.ToList())
            {
                popup.Render();
                if (!popup.IsOpen)
                    _activePopups.Remove(popup);
            }
        }
    }
}