namespace Trident.Styling
{
    public class ImGuiStyleConfig
    {
        public float WindowRounding { get; set; }
        public float FrameRounding { get; set; }
        public float PopupRounding { get; set; }
        public float GrabRounding { get; set; }
        public float SeparatorTextBorderSize { get; set; }
        public List<float> ItemSpacing { get; set; }
        public List<float> CellPadding { get; set; }
        public List<float> FramePadding { get; set; }
        public List<float> WindowTitleAlign { get; set; }
        public Dictionary<string, List<float>> Colors { get; set; }
    }
}