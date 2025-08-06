namespace Trident.Popups
{
    internal class LoadGamePakPopup : FileLoadPopup
    {
        internal LoadGamePakPopup() : base("Load GamePak") { }

        protected override void OnLoad(string path)
        {
            // same load the gamepak
            Console.WriteLine($"Loaded GamePak from: {path}");
        }
    }
}