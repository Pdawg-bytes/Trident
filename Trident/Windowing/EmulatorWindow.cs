using ImGuiNET;
using Trident.Styling;
using System.Text.Json;
using System.Reflection;
using OpenTK.Mathematics;
using Trident.Core.Machine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Trident.Windowing
{
    internal class EmulatorWindow : GameWindow
    {
        private GBA _gba;

        private ImGuiController _controller;
        private readonly ImGuiStyleConfig _styleConfig;

        private readonly byte[] _fontData;
        private readonly int _fontDataSize = 0;

        private int _frameCounter = 0;

        internal IntPtr WindowHandle;

        internal unsafe EmulatorWindow(GBA gba) : base(new GameWindowSettings(), new NativeWindowSettings())
        {
            _gba = gba;
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
            UpdateFrequency = Math.Clamp(Monitors.GetPrimaryMonitor().CurrentVideoMode.RefreshRate, 0, 90);

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
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _frameCounter++;
            if (_frameCounter == 5)
            {
                Title = $"Trident - UI FPS: {(1.0 / e.Time):F1}";
                _frameCounter = 0;
            }

            _controller.Update(this, (float)e.Time);

            GL.ClearColor(new Color4(20, 20, 20, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            ImGui.DockSpaceOverViewport();

            ImGui.ShowDemoWindow();

            _controller.Render();
            SwapBuffers();
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