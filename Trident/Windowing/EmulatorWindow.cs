using ImGuiNET;
using Trident.Popups;
using Trident.Styling;
using Trident.Widgets;
using System.Text.Json;
using Trident.Commands;
using Trident.Emulation;
using System.Reflection;
using System.Diagnostics;
using OpenTK.Mathematics;
using Trident.Popups.File;
using Trident.Interaction;
using Trident.Core.Machine;
using System.ComponentModel;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Trident.Widgets.Debugger;
using System.Runtime.InteropServices;
using Trident.Core.Hardware.Controller;
using OpenTK.Windowing.GraphicsLibraryFramework;

using FontData = (nint Pointer, int Size, float SizePixels, System.Runtime.InteropServices.GCHandle Handle);

namespace Trident.Windowing
{
    internal class EmulatorWindow : GameWindow
    {
        private readonly GBA _gba;
        private readonly EmulatorThread _emulatorThread;

        private int _framebufferTexture;

        private List<IWidget> _widgets = [];
        private Dictionary<string, List<IWidget>> _widgetGroups = [];

        private readonly PerformancePopup _performancePopup;

        private Dictionary<string, ImFontPtr> _fontPtrs = [];
        private ImGuiController _controller;
        private readonly ImGuiStyleConfig _styleConfig;

        private readonly ShortcutManager _shortcutManager = new();

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

            VSync = VSyncMode.Off;
            UpdateFrequency = 60;

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


            var fonts = new List<(string Name, nint Pointer, int Size, float SizePixels)>();
            List<GCHandle> handles = [];

            FontData firaSans = LoadFont("Trident.Fonts.FiraSans-Regular.ttf", 17f);
            fonts.Add(("Fira Sans", firaSans.Pointer, firaSans.Size, firaSans.SizePixels));
            handles.Add(firaSans.Handle);

            FontData firaCode = LoadFont("Trident.Fonts.FiraCode-Medium.ttf", 17f);
            fonts.Add(("Fira Code", firaCode.Pointer, firaCode.Size, firaCode.SizePixels));
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
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _emulatorThread?.Stop();
            _controller.DestroyDeviceObjects();
            _controller.Dispose();
        }


        private void InitFramebufferTexture()
        {
            _framebufferTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          240, 160, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
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
            ImFontPtr monoFont = _fontPtrs.GetValueOrDefault("Fira Code");
            if (monoFont.NativePtr == null)
                throw new InvalidOperationException("Fira Code was not loaded correctly.");

            AddWidget(new CPUStateWidget(monoFont, () => _gba.CPUSnapshot));
            AddWidget(new DisassemblyWidget(monoFont, _gba.Disassembler));
            AddWidget(new IRQStateWidget(monoFont, () => _gba.IRQSnapshot));

            MemoryViewer memView = new(monoFont);
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

            GL.ClearColor(new Color4(20, 20, 20, 255));
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
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load BIOS"))
                        PopupManager.Show(new LoadBIOSPopup(path => _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.BIOS, path))));

                    if (ImGui.MenuItem("Load GamePak"))
                        PopupManager.Show(new LoadGamePakPopup(path => _emulatorThread.EnqueueCommand(new LoadCommand(LoadType.GamePak, path))));

                    ImGui.Separator();

                    if (ImGui.MenuItem("Close"))
                        Close();

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Emulation"))
                {
                    if (ImGui.MenuItem("Reset", "Ctrl + R"))
                        _emulatorThread.Reset();

                    ImGui.Separator();

                    bool paused = _emulatorThread.IsPaused();
                    if (ImGui.MenuItem(paused ? "Play" : "Pause", "Ctrl + P"))
                        _emulatorThread.SetPause(paused = !paused);

                    if (paused)
                    {
                        if (ImGui.MenuItem("Step", "F11"))
                            StepGBA(1);
                    }
                    else
                    {
                        ImGui.BeginDisabled();
                        ImGui.MenuItem("Step", "F11");
                        ImGui.EndDisabled();
                    }

                    ImGui.Separator();

                    bool speedCapped = _emulatorThread.IsSpeedCapped();
                    if (ImGui.MenuItem(speedCapped ? "Fast forward" : "Cap speed", "Shift + P"))
                        _emulatorThread.SetSpeedCap(!speedCapped);

                    if (ImGui.MenuItem("Accurate sleep", "", _emulatorThread.AccurateSleep))
                        _emulatorThread.AccurateSleep = !_emulatorThread.AccurateSleep;

                    Tooltips.HelpTooltip("Enforces more precise timing in the GBA's run loop at the cost of CPU time.", -72f);

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Options"))
                {
                    if (ImGui.BeginMenu("System"))
                    {
                        if (ImGui.MenuItem("Skip BIOS", "", _emulatorThread.ShouldSkipBIOS))
                            _emulatorThread.ShouldSkipBIOS = !_emulatorThread.ShouldSkipBIOS;

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("GUI"))
                    {
                        if (ImGui.MenuItem("Sync to refresh      ", "", _uncappedRefresh))
                        {
                            _uncappedRefresh = !_uncappedRefresh;
                            SetUpdateFrequency(_uncappedRefresh);
                        }

                        Tooltips.HelpTooltip("Allows the GUI to run closer the primary monitor's refresh rate at the cost of CPU time.\nRounds to multiples of 60.", -38f);

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

                string value = $"{_emulatorThread.CurrentSpeed:F2}".PadLeft(6);
                string speed = $"GBA Speed: {value}%";
                var size = ImGui.CalcTextSize(speed);
                float totalWidth = ImGui.GetWindowWidth();

                ImGui.SameLine(totalWidth - size.X - 22);

                if (ImGui.BeginMenu(speed))
                {
                    PopupManager.Show(_performancePopup);
                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            PopupManager.Render();

            foreach (var widget in _widgets)
                widget.Render();


            ImGui.Begin("GBA Screen");

            // TODO: This is not thread-safe and will have tearing issues. Fix later.
            GL.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 240, 160,
                             PixelFormat.Bgra, PixelType.UnsignedByte, _gba.Framebuffer.Pixels);

            ImGui.Image(_framebufferTexture, new System.Numerics.Vector2(480, 320));
            ImGui.End();
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
                _emulatorThread.EnqueueCommand(new StepCommand(cycles));
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


        private void SetUpdateFrequency(bool uncapped)
        {
            if (uncapped)
            {
                int refresh = Monitors.GetPrimaryMonitor().CurrentVideoMode.RefreshRate;
                int rounded = Math.Clamp((int)(Math.Round(refresh / 60.0) * 60), 60, 120);
                UpdateFrequency = rounded;
            }
            else
                UpdateFrequency = 60;
        }


        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, uint cbAttribute);
    }
}