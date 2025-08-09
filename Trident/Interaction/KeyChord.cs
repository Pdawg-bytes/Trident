using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Trident.Interaction
{
    internal readonly record struct KeyChord(Keys Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
    {
        internal Keys Key { get; } = Key;
        internal bool Ctrl { get; } = Ctrl;
        internal bool Shift { get; } = Shift;
        internal bool Alt { get; } = Alt;

        internal bool Matches(Keys key, bool ctrlDown, bool shiftDown, bool altDown)
            => Key == key && Ctrl == ctrlDown && Shift == shiftDown && Alt == altDown;


        public override int GetHashCode() => HashCode.Combine(Key, Ctrl, Shift, Alt);
    }
}