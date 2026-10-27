using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private struct LayerPixel
    {
        internal ushort Color;
        internal bool   Transparent;
        internal byte   Priority;
        internal byte   Source;
        internal uint   Generation;
        internal bool   SemiTransparent;

        internal readonly bool IsOpaqueIn(uint generation) => Generation == generation && !Transparent;
    }

    private const byte OBJLayer      = 4;
    private const byte BackdropLayer = 5;

    private const byte OBJLayerBit   = 1 << OBJLayer;
    private const byte AllLayersMask = 0x1F;

    private readonly uint[][] ActiveBGs =
    [
        [ 0, 1, 2, 3 ],
        [ 0, 1, 2 ],
        [ 2, 3 ],
        [ 2 ],
        [ 2 ],
        [ 2 ],
    ];


    private readonly LayerPixel[][] _sortedBGLines = new LayerPixel[4][];
    private readonly byte[]         _sortedBGBits  = new byte[4];

    private void CompositeScanline(uint y, byte mode)
    {
        int bgCount         = SortBGs(mode);
        WindowCache windows = CacheWindows(y);
        BlendCache blend    = CacheBlend();
        LayerPixel backdrop = BackdropPixel;
        uint generation     = _pixelGeneration;

        for (uint x = 0; x < ScreenWidth; x++)
        {
            byte region = windows.Resolve(x, _objWindow);
            byte layers = (byte)(region & AllLayersMask);

            LayerPixel top   = backdrop;
            LayerPixel below = backdrop;

            if ((layers & OBJLayerBit) != 0)
            {
                ref LayerPixel objPx = ref GetUnsafe(_objLine, x);

                if (objPx.IsOpaqueIn(generation))
                    InsertLayer(objPx, ref top, ref below);
            }

            for (int i = 0; i < bgCount; i++)
            {
                if ((layers & _sortedBGBits[i]) == 0)
                    continue;

                ref LayerPixel px = ref GetUnsafe(_sortedBGLines[i], x);

                if (px.IsOpaqueIn(generation))
                    InsertLayer(px, ref top, ref below);
            }

            ushort color = ApplySpecialEffect(top, below, (region & EffectsBit) != 0, blend);
            _framebuffer.SetPixel(x, y, Framebuffer.ToArgb(color));
        }
    }


    private int SortBGs(byte mode)
    {
        uint[] active = ActiveBGs[mode];
        SortActiveBGs(active);

        int count = 0;
        foreach (uint bgId in active)
        {
            if (!DisplayControl.Enable[bgId])
                continue;

            _sortedBGLines[count] = _bgLines[bgId];
            _sortedBGBits[count]  = (byte)(1 << (int)bgId);
            count++;
        }

        return count;
    }


    private static void InsertLayer(in LayerPixel pixel, ref LayerPixel top, ref LayerPixel below)
    {
        if (IsInFront(pixel, top))
        {
            below = top;
            top   = pixel;
        }
        else if (IsInFront(pixel, below))
        {
            below = pixel;
        }
    }

    private static bool IsInFront(in LayerPixel candidate, in LayerPixel current)
    {
        if (candidate.Priority != current.Priority)
            return candidate.Priority < current.Priority;

        if (candidate.Source == OBJLayer || current.Source == BackdropLayer)
            return true;

        if (current.Source == OBJLayer || candidate.Source == BackdropLayer)
            return false;

        return candidate.Source < current.Source;
    }

    private void SortActiveBGs(uint[] active)
    {
        for (int i = 1; i < active.Length; i++)
        {
            uint bgId = active[i];
            int j     = i - 1;

            while (j >= 0 && IsBGInFront(bgId, active[j]))
            {
                active[j + 1] = active[j];
                j--;
            }

            active[j + 1] = bgId;
        }
    }

    private bool IsBGInFront(uint candidate, uint current)
    {
        byte candidatePriority = Backgrounds[candidate].Priority;
        byte currentPriority   = Backgrounds[current].Priority;

        return candidatePriority != currentPriority
            ? candidatePriority < currentPriority
            : candidate < current;
    }


    private const byte EffectsBit = 0x20;

    private struct WindowCache
    {
        internal bool Window0, Window1, ObjWindow;
        internal uint Window0Start, Window0End, Window1Start, Window1End;
        internal byte Window0Region, Window1Region, ObjWindowRegion, OutsideRegion;

        internal readonly byte Resolve(uint x, bool[] objWindow)
        {
            if (Window0 && x >= Window0Start && x < Window0End) return Window0Region;
            if (Window1 && x >= Window1Start && x < Window1End) return Window1Region;
            if (ObjWindow && objWindow[x])                      return ObjWindowRegion;

            return OutsideRegion;
        }
    }

    private WindowCache CacheWindows(uint y)
    {
        bool window0   = DisplayControl.Enable[5];
        bool window1   = DisplayControl.Enable[6];
        bool objWindow = DisplayControl.Enable[7];

        if (!window0 && !window1 && !objWindow)
            return new WindowCache { OutsideRegion = AllLayersMask | EffectsBit };

        WindowCache cache = new()
        {
            Window0         = window0 && IsInsideRange(y, Windows[0].Top, Windows[0].Bottom, ScreenHeight),
            Window1         = window1 && IsInsideRange(y, Windows[1].Top, Windows[1].Bottom, ScreenHeight),
            ObjWindow       = objWindow,
            Window0Region   = WindowRegion(Windows[0]),
            Window1Region   = WindowRegion(Windows[1]),
            ObjWindowRegion = Region(WindowOutside.OBJWindowLayers, WindowOutside.OBJWindowOBJEnabled, WindowOutside.OBJWindowFXEnabled),
            OutsideRegion   = Region(WindowOutside.OutsideLayers,   WindowOutside.OutsideOBJEnabled,   WindowOutside.OutsideFXEnabled)
        };

        (cache.Window0Start, cache.Window0End) = ClampRange(Windows[0].Left, Windows[0].Right, ScreenWidth);
        (cache.Window1Start, cache.Window1End) = ClampRange(Windows[1].Left, Windows[1].Right, ScreenWidth);

        return cache;
    }

    private static byte WindowRegion(Window window) => Region(window.Layers, window.OBJEnabled, window.FXEnabled);

    private static byte Region(byte bgLayers, bool objEnabled, bool effectsEnabled) =>
        (byte)(bgLayers | (objEnabled ? OBJLayerBit : 0) | (effectsEnabled ? EffectsBit : 0));

    private static bool IsInsideRange(uint position, uint start, uint end, uint limit)
    {
        (start, end) = ClampRange(start, end, limit);
        return position >= start && position < end;
    }

    private static (uint start, uint end) ClampRange(uint start, uint end, uint limit) =>
        (end > limit || start > end) ? (start, limit) : (start, end);


    private readonly struct BlendCache(byte first, byte second, BlendMode mode, byte eva, byte evb, byte evy)
    {
        internal readonly byte First     = first, Second = second;
        internal readonly BlendMode Mode = mode;
        internal readonly byte EVA       = eva, EVB = evb, EVY = evy;
    }

    private BlendCache CacheBlend() =>
        new(TargetMask(Blend.FirstTarget), TargetMask(Blend.SecondTarget), Blend.Mode, Blend.EVA, Blend.EVB, Blend.EVY);

    private static byte TargetMask(bool[] targets)
    {
        int mask = 0;
        for (int i = 0; i < targets.Length; i++)
            if (targets[i]) mask |= 1 << i;

        return (byte)mask;
    }


    private static ushort ApplySpecialEffect(in LayerPixel top, in LayerPixel below, bool effectsEnabled, in BlendCache blend)
    {
        bool canBlend = (blend.Second & (1 << below.Source)) != 0;

        if (top.SemiTransparent && top.Source == OBJLayer && canBlend)
            return BlendColors(top.Color, below.Color, blend.EVA, blend.EVB);

        if (!effectsEnabled || (blend.First & (1 << top.Source)) == 0)
            return top.Color;

        return blend.Mode switch
        {
            BlendMode.Alpha when canBlend => BlendColors(top.Color, below.Color, blend.EVA, blend.EVB),

            BlendMode.Brighten => AdjustBrightness(top.Color, blend.EVY, true),
            BlendMode.Darken   => AdjustBrightness(top.Color, blend.EVY, false),

            _ => top.Color
        };
    }

    private static ushort BlendColors(ushort top, ushort below, byte eva, byte evb)
    {
        int Mix(int shift) => Math.Min(31, (Channel(top, shift) * eva + Channel(below, shift) * evb) >> 4);

        return PackColor(Mix(0), Mix(5), Mix(10));
    }

    private static ushort AdjustBrightness(ushort color, byte evy, bool increase)
    {
        int Adjust(int shift)
        {
            int channel = Channel(color, shift);

            return increase
                ? channel + (((31 - channel) * evy) >> 4)
                : channel - ((channel * evy) >> 4);
        }

        return PackColor(Adjust(0), Adjust(5), Adjust(10));
    }

    private static int Channel(ushort color, int shift)           => (color >> shift) & 0x1F;
    private static ushort PackColor(int red, int green, int blue) => (ushort)(red | (green << 5) | (blue << 10));


    private LayerPixel BackdropPixel => new()
    {
        Color       = _pram.Fetch<ushort>(0),
        Transparent = false,
        Priority    = 0xFF,
        Source      = BackdropLayer,
        Generation  = _pixelGeneration
    };


    private void ResetScanlineBuffers()
    {
        foreach (LayerPixel[] line in _bgLines)
            Array.Clear(line);

        Array.Clear(_objLine);
        Array.Clear(_objWindow);
    }
}