using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Trident.Interaction
{
    internal class ShortcutManager
    {
        private readonly Dictionary<KeyChord, Action> _shortcuts = new();
        private bool _ctrlDown, _shiftDown, _altDown;

        internal void RegisterShortcut(KeyChord chord, Action action) => _shortcuts[chord] = action;

        internal void UpdateModifierState(Keys key, bool isDown)
        {
            switch (key)
            {
                case Keys.LeftControl:
                case Keys.RightControl:
                    _ctrlDown = isDown;
                    break;

                case Keys.LeftShift:
                case Keys.RightShift:
                    _shiftDown = isDown;
                    break;

                case Keys.LeftAlt:
                case Keys.RightAlt:
                    _altDown = isDown;
                    break;
            }
        }

        internal void HandleKeyDown(Keys key)
        {
            foreach (var kvp in _shortcuts)
            {
                if (kvp.Key.Matches(key, _ctrlDown, _shiftDown, _altDown))
                    kvp.Value.Invoke();
            }
        }
    }
}