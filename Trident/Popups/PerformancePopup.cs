using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Emulation;

namespace Trident.Popups;

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

            Span<char> buf = stackalloc char[64];
            var s = new StackString(buf);

            ImGui.TextUnformatted("UI Frametime (ms)");
            ImGui.PlotLines("##UIFrametime", ref _uiFrameTimes[0], BufferSize, _frameIndex, null, 0.0f, 50.0f,
                new Vector2(ImGui.GetContentRegionAvail().X, 80));

            s.Reset();
            s.Append("UI FPS: ");
            s.AppendFormatted(uiFps, "F1");
            ImGui.TextUnformatted(s.AsSpan());

            s.Reset();
            s.Append("UI Render time (ms): ");
            s.AppendFormatted(_realRenderTime, "F1");
            ImGui.TextUnformatted(s.AsSpan());

            ImGui.Separator();

            ImGui.TextUnformatted("GBA Frametime (ms)");
            ImGui.PlotLines("##GBAFrametime", ref _gbaFrameTimes[0], BufferSize, _frameIndex, null, 0.0f, 50.0f,
                new Vector2(ImGui.GetContentRegionAvail().X, 80));

            s.Reset();
            s.Append("GBA Speed: ");
            s.AppendFormatted(_getGbaSpeed(), "F2");
            s.Append('%');
            ImGui.TextUnformatted(s.AsSpan());

            s.Reset();
            s.Append("GBA FPS: ");
            s.AppendFormatted(gbaFps, "F2");
            ImGui.TextUnformatted(s.AsSpan());

            if (ImGui.Button("Close"))
            {
                ImGui.CloseCurrentPopup();
                IsOpen = false;
            }

            ImGui.EndPopup();
        }
    }


    internal void Update(double uiFrameTimeMs, double uiRenderTimeMs)
    {
        _uiFrameTimes[_frameIndex] = (float)uiFrameTimeMs;
        _realRenderTime = uiRenderTimeMs;

        double gbaFps = _getGbaSpeed() / 100.0 * EmulatorThread.Framerate;
        _gbaFrameTimes[_frameIndex] = (float)(1000.0 / gbaFps);

        _frameIndex = _frameIndex + 1 & BufferSize - 1;
    }
}