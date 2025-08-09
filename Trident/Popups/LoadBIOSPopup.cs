namespace Trident.Popups
{
    internal class LoadBIOSPopup(Action<string> loadBios) : FileLoadPopup("Load GBA BIOS")
    {
        private readonly Action<string> _loadBios = loadBios;
        protected override void OnLoad(string path) => _loadBios(path);
    }
}