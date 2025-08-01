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

namespace Trident.Windowing
{
    public class EmulatorWindow : GameWindow
    {
        private GBA _gba;

        private ImGuiController _controller;
        private readonly ImGuiStyleConfig _styleConfig;

        private byte[] _fontData;
        private int _fontDataSize = 0;

        private int _frameCounter = 0;

        public EmulatorWindow(GBA gba) : base
        (
            new GameWindowSettings() { UpdateFrequency = 90.0 },
            new NativeWindowSettings() { APIVersion = new Version(3, 3) }
        )
        {
            _gba = gba;


            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("Trident.Fonts.FiraCode-Medium.ttf"))
            {
                if (stream == null)
                    throw new FileNotFoundException("Font resource not found.");

                using MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                _fontData = ms.ToArray();
                _fontDataSize = _fontData.Length;
            }

            using (Stream stream = assembly.GetManifestResourceStream("Trident.Styling.ImGuiStyle.json"))
            {
                if (stream == null)
                    throw new FileNotFoundException("Style resource not found.");

                using StreamReader reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                _styleConfig = JsonSerializer.Deserialize<ImGuiStyleConfig>(json);
            }


            KeyDown += args => _controller.KeyEvent(args.Key, true);
            KeyUp += args => _controller.KeyEvent(args.Key, false);
        }


        protected unsafe override void OnLoad()
        {
            base.OnLoad();
            Title = "Trident";

            GCHandle handle = GCHandle.Alloc(_fontData, GCHandleType.Pinned);

            _controller = new ImGuiController
            (
                ClientSize.X, ClientSize.Y, 
                handle.AddrOfPinnedObject(), _fontDataSize,
                _styleConfig
            );

            handle.Free();
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
            _controller.Update(this, (float)e.Time);

            _frameCounter++;
            if (_frameCounter == 5)
            {
                Title = $"Trident - UI FPS: {(1.0 / e.Time):F1}";
                _frameCounter = 0;
            }

            GL.ClearColor(new Color4(0, 0, 0, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            ImGui.DockSpaceOverViewport();

            ImGui.ShowDemoWindow();

            _controller.Render();
            SwapBuffers();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _controller.MouseScroll(e.Offset);
        }
    }
}