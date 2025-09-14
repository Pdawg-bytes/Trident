using ImGuiNET;
using System.Numerics;

namespace Trident.Popups
{
    internal class PerformancePopup(Func<double> getGbaSpeed) : IPopup
    {
        internal const string PopupId = "PerformancePopup";
        public bool IsOpen { get; set; }

        private const int BufferSize = 64;
        private readonly float[] _uiFrameTimes  = new float[BufferSize];
        private readonly float[] _gbaFrameTimes = new float[BufferSize];
        private int _frameIndex = 0;

        private double _realRenderTime;

        private readonly Func<double> _getGbaSpeed = getGbaSpeed;


        internal void Update(double uiFrameTimeMs, double uiRenderTimeMs)
        {
            _uiFrameTimes[_frameIndex] = (float)uiFrameTimeMs;
            _realRenderTime = uiRenderTimeMs;

            double gbaFps = _getGbaSpeed() / 100.0 * 59.73;
            _gbaFrameTimes[_frameIndex] = (float)(1000.0 / gbaFps);

            _frameIndex = _frameIndex + 1 & BufferSize - 1;
        }

        public void Open()
        {
            ImGui.OpenPopup(PopupId);
            IsOpen = true;
        }

        public void Render()
        {
            if (!ImGui.IsPopupOpen(PopupId, ImGuiPopupFlags.AnyPopupId))
            {
                IsOpen = false;
                return;
            }

            if (ImGui.BeginPopup(PopupId, ImGuiWindowFlags.AlwaysAutoResize))
            {
                float lastUiFrame = _uiFrameTimes[_frameIndex - 1 + BufferSize & BufferSize - 1];
                float uiFps = 1000.0f / lastUiFrame;

                float lastGbaFrame = _gbaFrameTimes[_frameIndex - 1 + BufferSize & BufferSize - 1];
                float gbaFps = 1000.0f / lastGbaFrame;


                ImGui.TextUnformatted("UI Frametime (ms)");
                ImGui.PlotLines("##UIFrametime", ref _uiFrameTimes[0], BufferSize, _frameIndex, null, 0.0f, 50.0f, new Vector2(ImGui.GetContentRegionAvail().X, 80));

                ImGui.TextUnformatted($"UI FPS: {uiFps:F1}");
                ImGui.TextUnformatted($"UI Render time (ms): {_realRenderTime:F1}");

                ImGui.Separator();

                ImGui.TextUnformatted("GBA Frametime (ms)");
                ImGui.PlotLines("##GBAFrametime", ref _gbaFrameTimes[0], BufferSize, _frameIndex, null, 0.0f, 50.0f, new Vector2(ImGui.GetContentRegionAvail().X, 80));

                ImGui.TextUnformatted($"GBA Speed: {_getGbaSpeed():F2}%");
                ImGui.TextUnformatted($"GBA FPS: {gbaFps:F2}");

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                    IsOpen = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}