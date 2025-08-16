using ImGuiNET;
using Trident.Popups;
using Trident.Styling;
using Trident.Widgets;
using Trident.Commands;
using System.Text.Json;
using Trident.Emulation;
using System.Reflection;
using OpenTK.Mathematics;
using Trident.Interaction;
using Trident.Popups.File;
using Trident.Core.Machine;
using System.ComponentModel;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Trident.Windowing
{
    internal class EmulatorWindow : GameWindow
    {
        private GBA _gba;
        private EmulatorThread _emulatorThread;

        private List<IWidget> _widgets = [];
        private readonly PerformancePopup _performanceWidget;

        private ImGuiController _controller;
        private readonly ImGuiStyleConfig _styleConfig;

        private readonly byte[] _fontData;
        private readonly int _fontDataSize = 0;

        private readonly ShortcutManager _shortcutManager = new();

        internal IntPtr WindowHandle;


        internal unsafe EmulatorWindow(GBA gba) : base(new GameWindowSettings(), new NativeWindowSettings())
        {
            _gba = gba;
            _emulatorThread = new(gba);

            _performanceWidget = new(() => _emulatorThread.CurrentSpeed);

            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("Trident.Fonts.FiraCode-Medium.ttf")!)
            {
                if (stream == null)
                    throw new FileNotFoundException("Font resource not found.");

                using MemoryStream ms = new();
                stream.CopyTo(ms);
                _fontData = ms.ToArray();
                _fontDataSize = _fontData.Length;
            }

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
            UpdateFrequency = Math.Min(Monitors.GetPrimaryMonitor().CurrentVideoMode.RefreshRate, 90);

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

            GCHandle handle = GCHandle.Alloc(_fontData, GCHandleType.Pinned);
            _controller = new ImGuiController
            (
                ClientSize.X, ClientSize.Y, 
                handle.AddrOfPinnedObject(), _fontDataSize,
                _styleConfig
            );
            handle.Free();

            KeyDown += args => _controller.KeyEvent(args.Key, true);
            KeyUp += args => _controller.KeyEvent(args.Key, false);

            KeyDown += args =>
            {
                _shortcutManager.UpdateModifierState(args.Key, true);
                _shortcutManager.HandleKeyDown(args.Key);
            };

            KeyUp += args => _shortcutManager.UpdateModifierState(args.Key, false);

            _emulatorThread.Start();

            _shortcutManager.RegisterShortcut(new(Keys.R, Ctrl: true), _emulatorThread.Reset);
            _shortcutManager.RegisterShortcut(new(Keys.P, Ctrl: true), () => _emulatorThread.SetPause(!_emulatorThread.IsPaused()));
            _shortcutManager.RegisterShortcut(new(Keys.P, Shift: true), () => _emulatorThread.SetSpeedCap(!_emulatorThread.IsSpeedCapped()));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _emulatorThread?.Stop();
            _controller.DestroyDeviceObjects();
            _controller.Dispose();
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

            _performanceWidget.Update(e.Time * 1000.0);

            GL.ClearColor(new Color4(20, 20, 20, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            ImGui.DockSpaceOverViewport();
            RenderGUI();

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

                    if (ImGui.MenuItem("Shutdown"))
                        _emulatorThread.Stop();

                    ImGui.Separator();

                    bool paused = _emulatorThread.IsPaused();
                    if (ImGui.MenuItem(paused ? "Play" : "Pause", "Ctrl + P"))
                        _emulatorThread.SetPause(!paused);

                    bool speedCapped = _emulatorThread.IsSpeedCapped();
                    if (ImGui.MenuItem(speedCapped ? "Fast forward" : "Cap speed", "Shift + P"))
                        _emulatorThread.SetSpeedCap(!speedCapped);

                    if (ImGui.MenuItem("Accurate sleep", "", _emulatorThread.AccurateSleep))
                        _emulatorThread.AccurateSleep = !_emulatorThread.AccurateSleep;

                    Tooltips.HelpTooltip("Enforces more precise timing in the GBA's run loop at the cost of CPU time.", -88f);

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

                    ImGui.EndMenu();
                }

                string value = $"{_emulatorThread.CurrentSpeed:F2}".PadLeft(6);
                string speed = $"GBA Speed: {value}%";
                var size = ImGui.CalcTextSize(speed);
                float totalWidth = ImGui.GetWindowWidth();

                ImGui.SameLine(totalWidth - size.X - 22);

                if (ImGui.BeginMenu(speed))
                {
                    PopupManager.Show(_performanceWidget);
                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            PopupManager.Render();

            foreach (var widget in _widgets)
                widget.Render();
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


        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, uint cbAttribute);
    }
}