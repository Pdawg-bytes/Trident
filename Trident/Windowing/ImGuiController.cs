using ImGuiNET;
using System.Numerics;
using Trident.Styling;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Trident.Windowing;

public class ImGuiController : IDisposable
{
    private bool _frameBegun;
    private int _vertexArray, _vertexBuffer, _indexBuffer;
    private int _vertexBufferSize = 10000, _indexBufferSize = 2000;
    private int _fontTexture, _shader;
    private int _uProjection, _uTexture;

    private int _windowWidth, _windowHeight;
    private readonly List<char> _pressedChars = new();

    internal ImGuiController(
        int width, int height,
        List<(string name, nint ptr, int size, float sizePixels)> fonts,
        out Dictionary<string, ImFontPtr> fontPtrs,
        ImGuiStyleConfig style)
    {
        _windowWidth = width;
        _windowHeight = height;

        ImGui.CreateContext();
        var io = ImGui.GetIO();

        fontPtrs = new Dictionary<string, ImFontPtr>();
        foreach (var (name, ptr, size, sizePixels) in fonts)
            fontPtrs.Add(name, io.Fonts.AddFontFromMemoryTTF(ptr, size, sizePixels));

        ApplyStyle(style);

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        CreateDeviceResources();
        SetPerFrameData(1f / 60f);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    private void ApplyStyle(ImGuiStyleConfig config)
    {
        var style = ImGui.GetStyle();

        style.WindowRounding = config.WindowRounding;
        style.ChildRounding = config.ChildRounding;
        style.FrameRounding = config.FrameRounding;
        style.PopupRounding = config.PopupRounding;
        style.ScrollbarRounding = config.ScrollbarRounding;
        style.GrabRounding = config.GrabRounding;
        style.TabRounding = config.TabRounding;

        style.WindowBorderSize = config.WindowBorderSize;
        style.ChildBorderSize = config.ChildBorderSize;
        style.FrameBorderSize = config.FrameBorderSize;
        style.PopupBorderSize = config.PopupBorderSize;
        style.TabBorderSize = config.TabBorderSize;
        style.TabBarBorderSize = config.TabBarBorderSize;
        style.DockingSeparatorSize = config.DockingSeparatorSize;
        style.SeparatorTextBorderSize = config.SeparatorTextBorderSize;

        style.WindowPadding = new Vector2(config.WindowPadding[0], config.WindowPadding[1]);
        style.FramePadding = new Vector2(config.FramePadding[0], config.FramePadding[1]);
        style.ItemSpacing = new Vector2(config.ItemSpacing[0], config.ItemSpacing[1]);
        style.CellPadding = new Vector2(config.CellPadding[0], config.CellPadding[1]);
        style.WindowTitleAlign = new Vector2(config.WindowTitleAlign[0], config.WindowTitleAlign[1]);
        style.SeparatorTextPadding = new Vector2(config.SeparatorTextPadding[0], config.SeparatorTextPadding[1]);

        foreach (var kvp in config.Colors)
        {
            if (Enum.TryParse(kvp.Key, out ImGuiCol col))
            {
                var v = kvp.Value;
                style.Colors[(int)col] = new Vector4(v[0], v[1], v[2], v[3]);
            }
        }
    }


    private void CreateDeviceResources()
    {
        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, nint.Zero, BufferUsageHint.DynamicDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, nint.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontTexture();

        _shader = CreateProgram("ImGui", GetVertexSource(), GetFragmentSource());
        _uProjection = GL.GetUniformLocation(_shader, "projection_matrix");
        _uTexture = GL.GetUniformLocation(_shader, "in_fontTexture");

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.EnableVertexAttribArray(0); // Pos
        GL.EnableVertexAttribArray(1); // UV
        GL.EnableVertexAttribArray(2); // Color
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.BindVertexArray(0);
    }

    private void RecreateFontTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out _);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        io.Fonts.SetTexID(_fontTexture);
        io.Fonts.ClearTexData();
    }


    public void Update(GameWindow wnd, float dt)
    {
        if (_frameBegun) ImGui.Render();

        SetPerFrameData(dt);
        UpdateInput(wnd);

        _frameBegun = true;
        ImGui.NewFrame();
    }

    private void UpdateInput(GameWindow wnd)
    {
        var io = ImGui.GetIO();
        var mouse = wnd.MouseState;

        io.MousePos = new Vector2(mouse.X, mouse.Y);
        io.MouseDown[0] = mouse.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonDown(MouseButton.Middle);

        foreach (var c in _pressedChars) io.AddInputCharacter(c);
        _pressedChars.Clear();
    }

    public void PressChar(char c) => _pressedChars.Add(c);
    public void MouseScroll(OpenTK.Mathematics.Vector2 offset) { ImGui.GetIO().MouseWheelH = offset.X; ImGui.GetIO().MouseWheel = offset.Y; }
    public void WindowResized(int w, int h) { _windowWidth = w; _windowHeight = h; }


    public void Render()
    {
        if (!_frameBegun) return;
        _frameBegun = false;
        ImGui.Render();

        RenderDrawData(ImGui.GetDrawData());
    }

    private void RenderDrawData(ImDrawDataPtr data)
    {
        if (data.CmdListsCount == 0) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        var io = ImGui.GetIO();
        OpenTK.Mathematics.Matrix4 mvp = OpenTK.Mathematics.Matrix4.CreateOrthographicOffCenter(0, io.DisplaySize.X, io.DisplaySize.Y, 0, -1.0f, 1.0f);

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_uProjection, false, ref mvp);
        GL.Uniform1(_uTexture, 0);
        GL.BindVertexArray(_vertexArray);

        data.ScaleClipRects(io.DisplayFramebufferScale);

        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdList = data.CmdLists[n];

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);

                GL.Scissor((int)cmd.ClipRect.X, _windowHeight - (int)cmd.ClipRect.W, (int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y));

                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)cmd.ElemCount, DrawElementsType.UnsignedShort, (nint)(cmd.IdxOffset * sizeof(ushort)), (int)cmd.VtxOffset);
            }
        }
    }

    private void SetPerFrameData(float dt)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        io.DeltaTime = dt;
    }


    private string GetVertexSource() => @"#version 330 core
        layout(location = 0) in vec2 in_pos;
        layout(location = 1) in vec2 in_uv;
        layout(location = 2) in vec4 in_col;
        uniform mat4 projection_matrix;
        out vec4 color; out vec2 uv;
        void main() { gl_Position = projection_matrix * vec4(in_pos, 0, 1); color = in_col; uv = in_uv; }";

    private string GetFragmentSource() => @"#version 330 core
        uniform sampler2D in_fontTexture;
        in vec4 color; in vec2 uv;
        out vec4 fragColor;
        void main() { fragColor = color * texture(in_fontTexture, uv); }";

    private int CreateProgram(string name, string v, string f)
    {
        int p = GL.CreateProgram();
        int vs = Compile(p, ShaderType.VertexShader, v);
        int fs = Compile(p, ShaderType.FragmentShader, f);
        GL.LinkProgram(p);
        GL.DetachShader(p, vs); GL.DetachShader(p, fs);
        GL.DeleteShader(vs); GL.DeleteShader(fs);
        return p;
    }

    private int Compile(int p, ShaderType t, string s)
    {
        int sh = GL.CreateShader(t);
        GL.ShaderSource(sh, s);
        GL.CompileShader(sh);
        GL.AttachShader(p, sh);
        return sh;
    }


    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
    }


    public void KeyEvent(Keys key, bool isDown) => ImGui.GetIO().AddKeyEvent(TranslateKey(key), isDown);
    private ImGuiKey TranslateKey(Keys key)
    {
        ImGuiKey MapRange(Keys key, Keys startSrc, ImGuiKey startDest)
            => startDest + ((int)key - (int)startSrc);

        return key switch
        {
            >= Keys.F1 and <= Keys.F24 => MapRange(key, Keys.F1, ImGuiKey.F1),
            >= Keys.KeyPad0 and <= Keys.KeyPad9 => MapRange(key, Keys.KeyPad0, ImGuiKey.Keypad0),
            >= Keys.A and <= Keys.Z => MapRange(key, Keys.A, ImGuiKey.A),
            >= Keys.D0 and <= Keys.D9 => MapRange(key, Keys.D0, ImGuiKey._0),
            Keys.LeftShift or Keys.RightShift => ImGuiKey.ModShift,
            Keys.LeftControl or Keys.RightControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt or Keys.RightAlt => ImGuiKey.ModAlt,
            Keys.LeftSuper or Keys.RightSuper => ImGuiKey.ModSuper,
            Keys.Menu => ImGuiKey.Menu,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Enter => ImGuiKey.Enter,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Backspace => ImGuiKey.Backspace,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.Home => ImGuiKey.Home,
            Keys.End => ImGuiKey.End,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.ScrollLock => ImGuiKey.ScrollLock,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Pause => ImGuiKey.Pause,
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.KeyPadDivide => ImGuiKey.KeypadDivide,
            Keys.KeyPadMultiply => ImGuiKey.KeypadMultiply,
            Keys.KeyPadSubtract => ImGuiKey.KeypadSubtract,
            Keys.KeyPadAdd => ImGuiKey.KeypadAdd,
            Keys.KeyPadDecimal => ImGuiKey.KeypadDecimal,
            Keys.KeyPadEnter => ImGuiKey.KeypadEnter,
            Keys.GraveAccent => ImGuiKey.GraveAccent,
            Keys.Minus => ImGuiKey.Minus,
            Keys.Equal => ImGuiKey.Equal,
            Keys.LeftBracket => ImGuiKey.LeftBracket,
            Keys.RightBracket => ImGuiKey.RightBracket,
            Keys.Semicolon => ImGuiKey.Semicolon,
            Keys.Apostrophe => ImGuiKey.Apostrophe,
            Keys.Comma => ImGuiKey.Comma,
            Keys.Period => ImGuiKey.Period,
            Keys.Slash => ImGuiKey.Slash,
            Keys.Backslash => ImGuiKey.Backslash,
            _ => ImGuiKey.None
        };
    }
}