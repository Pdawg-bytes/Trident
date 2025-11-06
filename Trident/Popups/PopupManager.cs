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


            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.Render();
                if (!popup.IsOpen)
                    _activePopups.RemoveAt(i);
            }
        }
    }
}