using ImGuiNET;
using Trident.Popups;
using Trident.Styling;
using Trident.Widgets;
using System.Text.Json;
using Trident.Commands;
using Trident.Utilities;
using System.Reflection;
using Trident.Emulation;
using OpenTK.Mathematics;
using System.Diagnostics;
using Trident.Interaction;
using Trident.Popups.File;
using Trident.Core.Machine;
using System.ComponentModel;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Trident.Widgets.Debugger;
using System.Runtime.InteropServices;
using Trident.Core.Hardware.Controller;
using OpenTK.Windowing.GraphicsLibraryFramework;

using static Trident.Fonts.IconConstants;

using FontData = (nint Pointer, int Size, float SizePixels, System.Runtime.InteropServices.GCHandle Handle);

namespace Trident.Windowing;

internal class EmulatorWindow : GameWindow
{
    private const int GBAWidth = 240;
    private const int GBAHeight = 160;

    private readonly GBA _gba;
    private readonly EmulatorThread _emulatorThread;

    private int _framebufferTexture;
    private readonly uint[] _frameUploadBuffer = new uint[GBAWidth * GBAHeight];

    private List<IWidget> _widgets = [];
    private Dictionary<string, List<IWidget>> _widgetGroups = [];

    private readonly PerformancePopup _performancePopup;

    private Dictionary<string, ImFontPtr> _fontPtrs = [];
    private ImFontPtr _monoFont;
    private ImGuiController _controller;
    private readonly ImGuiStyleConfig _styleConfig;

    private readonly ShortcutManager _shortcutManager = new();

    private bool _demoOpen = false;
    private bool _uncappedRefresh = false;
    internal IntPtr WindowHandle;

    internal unsafe EmulatorWindow(GBA gba) : base(new GameWindowSettings(), new NativeWindowSettings())
    {
        _gba = gba;
        _emulatorThread = new(gba);

        _performancePopup = new(() => _emulatorThread.CurrentSpeed);

        var assembly = Assembly.GetExecutingAssembly();

        using (Stream stream = assembly.GetManifestResourceStream("Trident.Styling.ImGuiStyle.json")!)
        {
            if (stream == null)
                throw new FileNotFoundException("Style resource not found.");

            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();

            _styleConfig = JsonSerializer.Deserialize<ImGuiStyleConfig>(json) ?? 
                throw new InvalidDataException("ImGui style configuration was unable to be deserialized.");
        }
        
        string vendor = GL.GetString(StringName.Vendor) ?? "";
        bool isIntel  = vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase);

        if (isIntel && Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // Intel's awful drivers seem to just spin in SwapBuffers, making VSync burn an entire core!
            UpdateFrequency = Monitors.GetPrimaryMonitor().CurrentVideoMode.RefreshRate;
            VSync           = VSyncMode.Off;
        }
        else
        {
            UpdateFrequency = 0;
            VSync           = VSyncMode.On;
        }

        WindowHandle = GLFW.GetWin32Window(WindowPtr);
    }


    protected unsafe override void OnLoad()
    {
        base.OnLoad();
        Title = "Trident";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build >= 22000)
        {
            int useDarkMode = 1;
            DwmSetWindowAttribute(WindowHandle, 20 /*DWMWA_USE_IMMERSIVE_DARK_MODE */, ref useDarkMode, sizeof(int));
        }


        var fonts = new List<(string Name, nint Pointer, int Size, float SizePixels, bool Merge, ushort[] Ranges)>();
        List<GCHandle> handles = [];

        FontData roboto = LoadFont("Trident.Fonts.Roboto-Regular.ttf", 16f);
        fonts.Add(("Roboto", roboto.Pointer, roboto.Size, roboto.SizePixels, false, []));
        handles.Add(roboto.Handle);

        FontData icons = LoadFont("Trident.Fonts.MaterialSymbolsRounded.ttf", 20f);
        fonts.Add(("MaterialRounded", icons.Pointer, icons.Size, icons.SizePixels, true, [ IconMinimum, IconMaximum, 0 ]));
        handles.Add(icons.Handle);

        FontData firaCode = LoadFont("Trident.Fonts.FiraCode-Medium.ttf", 16f);
        fonts.Add(("Fira Code", firaCode.Pointer, firaCode.Size, firaCode.SizePixels, false, []));
        handles.Add(firaCode.Handle);

        _controller = new ImGuiController
        (
            ClientSize.X, ClientSize.Y,
            fonts,
            out _fontPtrs,
            _styleConfig
        );

        foreach (var gcHandle in handles)
            gcHandle.Free();


        KeyDown += args =>
        {
            _controller.KeyEvent(args.Key, true);
            _shortcutManager.UpdateModifierState(args.Key, true);
            _shortcutManager.HandleKeyDown(args.Key);

            if (TryMapKeyToGBA(args.Key, out GBAKey gbaKey))
            {
                _emulatorThread.EnqueueCommand(new KeyPressedCommand(gbaKey, true));
            }
        };

        KeyUp += args =>
        {
            _controller.KeyEvent(args.Key, false);
            _shortcutManager.UpdateModifierState(args.Key, false);

            if (TryMapKeyToGBA(args.Key, out GBAKey gbaKey))
            {
                _emulatorThread.EnqueueCommand(new KeyPressedCommand(gbaKey, false));
            }
        };


        _emulatorThread.Start();

        _shortcutManager.RegisterShortcut(new(Keys.R, Ctrl: true), _emulatorThread.Reset);
        _shortcutManager.RegisterShortcut(new(Keys.P, Ctrl: true), () => _emulatorThread.SetPause(!_emulatorThread.IsPaused()));
        _shortcutManager.RegisterShortcut(new(Keys.P, Shift: true), () => _emulatorThread.SetSpeedCap(!_emulatorThread.IsSpeedCapped()));
        _shortcutManager.RegisterShortcut(new(Keys.F11), () => StepGBA(1));

        InitFramebufferTexture();
        InitWidgets();

        _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.BIOS, @"C:\Users\Pdawg\Downloads\gba_bios.bin"));
        _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.GamePak, @"C:\Users\Pdawg\Downloads\Pokemon - Ruby Version (U) (V1.1)\Pokemon - Ruby Version (U) (V1.1).gba"));
    }

    private void InitFramebufferTexture()
    {
        _framebufferTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                      GBAWidth, GBAHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        _emulatorThread?.Stop();
        _controller.Dispose();
    }


    private void AddWidget(IWidget widget)
    {
        _widgets.Add(widget);

        _widgetGroups = _widgets
            .GroupBy(w => w.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private unsafe void InitWidgets()
    {
        _monoFont = _fontPtrs.GetValueOrDefault("Fira Code");
        if (_monoFont.NativePtr == null)
            throw new InvalidOperationException("Fira Code was not loaded correctly.");

        AddWidget(new CPUStateWidget(_monoFont, _gba.GetCPUSnapshot));
        AddWidget(new BreakpointWidget(_monoFont, _gba.Breakpoints, _emulatorThread.SetPause));
        AddWidget(new DisassemblyWidget(_monoFont, _gba.Disassembler, _gba.Breakpoints));
        AddWidget(new IRQStateWidget(_monoFont, _gba.GetIRQSnapshot));
        AddWidget(new DMAControllerWidget(_monoFont, _gba.GetDMASnapshot));
        AddWidget(new TimerWidget(_monoFont, _gba.GetTimerSnapshot));

        AddWidget(new BackgroundLayerWidget(_monoFont, _gba.GetBackgroundSnapshot, _gba.RenderBGToBuffer));
        AddWidget(new SpriteViewerWidget(_monoFont, _gba.GetSpriteSnapshot, _gba.RenderSpriteToBuffer));
        AddWidget(new PaletteViewerWidget(_monoFont, _gba.GetPaletteSnapshot));

        MemoryViewer memView = new(_monoFont);
        memView.SetReadFunction(address => _gba.DebugRead<byte>(address));
        AddWidget(memView);
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        if (IsExiting) return;

        base.OnRenderFrame(e);
        _controller.Update(this, (float)e.Time);

        GL.ClearColor(new Color4(0, 0, 0, 0));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        long start = Stopwatch.GetTimestamp();
        ImGui.DockSpaceOverViewport();
        RenderGUI();
        long end = Stopwatch.GetTimestamp();

        _performancePopup.Update(e.Time * 1000.0, (end - start) * 1000.0 / Stopwatch.Frequency);

        _controller.Render();
        SwapBuffers();
    }

    private void RenderGUI()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem(IconFileOpen + "Load BIOS"))
                    PopupManager.Show(new LoadBIOSPopup(path => _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.BIOS, path))));

                if (ImGui.MenuItem(IconFileOpen + "Load GamePak"))
                    PopupManager.Show(new LoadGamePakPopup(path => _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.GamePak, path))));

                ImGui.Separator();

                if (ImGui.MenuItem(IconClose + "Close"))
                    Close();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Emulation"))
            {
                if (ImGui.MenuItem(IconRestartAlt + "Reset", "Ctrl + R"))
                    _emulatorThread.Reset();

                ImGui.Separator();

                bool paused = _emulatorThread.IsPaused();
                if (ImGui.MenuItem(paused ? $"{IconPlayArrow}Play" : $"{IconPause}Pause", "Ctrl + P"))
                    _emulatorThread.SetPause(paused = !paused);

                if (paused)
                {
                    if (ImGui.MenuItem(IconStep + "Step", "F11"))
                        StepGBA(1);
                }
                else
                {
                    ImGui.BeginDisabled();
                    ImGui.MenuItem(IconStep + "Step", "F11");
                    ImGui.EndDisabled();
                }

                ImGui.Separator();

                bool speedCapped = _emulatorThread.IsSpeedCapped();
                if (ImGui.MenuItem(speedCapped ? $"{IconFastForward}Fast forward" : $"{IconSpeed}Cap speed", "Shift + P"))
                    _emulatorThread.SetSpeedCap(!speedCapped);

                if (ImGui.MenuItem(IconSleep + "Accurate sleep", "", _emulatorThread.AccurateSleep))
                    _emulatorThread.AccurateSleep = !_emulatorThread.AccurateSleep;

                Tooltips.HelpTooltip("Enforces more precise timing in the GBA's run loop at the cost of CPU time.", -72f);

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Options"))
            {
                if (ImGui.BeginMenu(IconDeveloperBoard + "System"))
                {
                    if (ImGui.MenuItem(IconStepOver + "Skip BIOS", "", _emulatorThread.ShouldSkipBIOS))
                        _emulatorThread.ShouldSkipBIOS = !_emulatorThread.ShouldSkipBIOS;

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Debugger"))
            {
                foreach (var group in _widgetGroups)
                {
                    if (ImGui.BeginMenu(group.Key))
                    {
                        foreach (var widget in group.Value)
                        {
                            bool visible = widget.IsVisible;
                            if (ImGui.MenuItem(widget.Name, null, ref visible))
                                widget.IsVisible = visible;
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMenu();
            }


            Span<char> buf = stackalloc char[16];
            var speedStr   = new StackString(buf);

            speedStr.AppendFormatted(_emulatorThread.CurrentSpeed, "F2");
            speedStr.Append('%');

            ImGui.PushFont(_monoFont);
            float valueSize = ImGui.CalcTextSize(speedStr.AsSpan()).X;

            float totalWidth = ImGui.GetWindowWidth();
            float rightX = totalWidth - valueSize - 14;

            const float menuWidth = 162.0f; 

            ImGui.SameLine(totalWidth - menuWidth);

            bool open = ImGui.BeginMenu("##GbaSpeedMenu");

            float labelX = totalWidth - menuWidth + 10;
            ImGui.SetCursorPosX(labelX);
            ImGui.TextUnformatted("GBA Speed:");

            ImGui.SameLine();
            ImGui.SetCursorPosX(rightX);
            ImGui.TextUnformatted(speedStr.AsSpan());

            if (open)
            {
                PopupManager.Show(_performancePopup);
                ImGui.EndMenu();
            }
            ImGui.PopFont();

            ImGui.EndMainMenuBar();
        }
        ImGui.PopStyleVar();

        PopupManager.Render();

        foreach (var widget in _widgets)
            widget.Render();


        ImGui.Begin("GBA Screen");

        GL.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
        _gba.Framebuffer.CopyFrontPixels(_frameUploadBuffer);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, GBAWidth, GBAHeight,
                 PixelFormat.Bgra, PixelType.UnsignedByte, _frameUploadBuffer);

        ImGui.Image(_framebufferTexture, new System.Numerics.Vector2(480, 320));
        ImGui.End();

        if (_demoOpen)
            ImGui.ShowDemoWindow();
    }


    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _controller!.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _controller!.MouseScroll(e.Offset);
    }

    private bool TryMapKeyToGBA(Keys key, out GBAKey gbaKey)
    {
        switch (key)
        {
            case Keys.Z:
                gbaKey = GBAKey.B;
                return true;
            case Keys.X:
                gbaKey = GBAKey.A;
                return true;

            case Keys.A:
                gbaKey = GBAKey.LB;
                return true;
            case Keys.S:
                gbaKey = GBAKey.RB;
                return true;

            case Keys.Up:
                gbaKey = GBAKey.Up;
                return true;
            case Keys.Right:
                gbaKey = GBAKey.Right;
                return true;
            case Keys.Down:
                gbaKey = GBAKey.Down;
                return true;
            case Keys.Left:
                gbaKey = GBAKey.Left;
                return true;

            case Keys.Backspace:
                gbaKey = GBAKey.Select;
                return true;
            case Keys.Enter:
                gbaKey = GBAKey.Start;
                return true;

            default:
                gbaKey = default;
                return false;
        }
    }


    private void StepGBA(ulong cycles)
    {
        if (_emulatorThread.IsPaused())
        {
            if (_gba.Breakpoints.TryGetLastHit(out uint addr))
                _gba.Breakpoints.Continue(addr);

            _emulatorThread.EnqueueCommand(new StepCommand(cycles));
        }    
    }


    private FontData LoadFont(string resourceName, float sizePixels)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Font resource '{resourceName}' not found.");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] fontData = ms.ToArray();

        GCHandle handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
        return (handle.AddrOfPinnedObject(), fontData.Length, sizePixels, handle);
    }


    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, uint cbAttribute);
}