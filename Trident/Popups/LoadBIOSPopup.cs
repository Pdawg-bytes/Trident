namespace Trident.Popups
{
    internal class LoadBIOSPopup : FileLoadPopup
    {
        internal LoadBIOSPopup() : base("Load GBA BIOS") { }

        protected override void OnLoad(string path)
        {
            // actually load the bios
            Console.WriteLine($"Loaded BIOS from: {path}");
        }
    }
}