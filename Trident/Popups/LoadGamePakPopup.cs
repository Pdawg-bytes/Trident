namespace Trident.Popups
{
    internal class LoadGamePakPopup(Action<string> loadGamePak) : FileLoadPopup("Load GamePak")
    {
        private readonly Action<string> _loadGamePak = loadGamePak;
        protected override void OnLoad(string path) => _loadGamePak(path);
    }
}