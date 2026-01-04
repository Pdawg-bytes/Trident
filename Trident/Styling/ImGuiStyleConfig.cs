namespace Trident.Styling;

public class ImGuiStyleConfig
{
    public float WindowRounding { get; set; }
    public float ChildRounding { get; set; }
    public float FrameRounding { get; set; }
    public float PopupRounding { get; set; }
    public float ScrollbarRounding { get; set; }
    public float GrabRounding { get; set; }
    public float TabRounding { get; set; }

    public float WindowBorderSize { get; set; }
    public float ChildBorderSize { get; set; }
    public float FrameBorderSize { get; set; }
    public float PopupBorderSize { get; set; }
    public float TabBorderSize { get; set; }
    public float TabBarBorderSize { get; set; }
    public float DockingSeparatorSize { get; set; }
    public float SeparatorTextBorderSize { get; set; }

    public List<float> WindowPadding { get; set; }
    public List<float> FramePadding { get; set; }
    public List<float> ItemSpacing { get; set; }
    public List<float> CellPadding { get; set; }
    public List<float> WindowTitleAlign { get; set; }
    public List<float> SeparatorTextPadding { get; set; }

    public Dictionary<string, List<float>> Colors { get; set; }
}