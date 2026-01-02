namespace Trident.Widgets;

internal interface IWidget
{
    public void Render();

    public bool IsVisible { get; set; }

    public string Name { get; }
    public string Group { get; }
}