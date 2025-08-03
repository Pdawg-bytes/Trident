namespace Trident.Widgets
{
    public interface IWidget
    {
        public void Render();

        public bool IsVisible { get; set; }
        public string Name { get; }
    }
}