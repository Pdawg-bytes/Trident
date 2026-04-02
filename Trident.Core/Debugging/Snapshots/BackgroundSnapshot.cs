namespace Trident.Core.Debugging.Snapshots;

public readonly struct BackgroundSnapshot
(
    byte bgMode,
    bool bg0Enabled, bool bg1Enabled, bool bg2Enabled, bool bg3Enabled, bool objEnabled, bool forcedBlank,
    in BackgroundSnapshot.LayerSnapshot bg0,
    in BackgroundSnapshot.LayerSnapshot bg1,
    in BackgroundSnapshot.LayerSnapshot bg2,
    in BackgroundSnapshot.LayerSnapshot bg3
)
{
    public readonly byte BackgroundMode = bgMode;

    public readonly bool BG0Enabled  = bg0Enabled;
    public readonly bool BG1Enabled  = bg1Enabled;
    public readonly bool BG2Enabled  = bg2Enabled;
    public readonly bool BG3Enabled  = bg3Enabled;
    public readonly bool ObjEnabled  = objEnabled;
    public readonly bool ForcedBlank = forcedBlank;

    public readonly LayerSnapshot BG0 = bg0;
    public readonly LayerSnapshot BG1 = bg1;
    public readonly LayerSnapshot BG2 = bg2;
    public readonly LayerSnapshot BG3 = bg3;


    public readonly struct LayerSnapshot
    (
        bool affine, byte priority, byte charBaseBlock, bool mosaic, bool use256Colors,
        byte screenBaseBlock, bool overflowWrap, byte screenSize,
        ushort xOffset, ushort yOffset,
        short pa, short pb, short pc, short pd,
        int refX, int refY
    )
    {
        public readonly bool Affine          = affine;
        public readonly byte Priority        = priority;
        public readonly byte CharBaseBlock   = charBaseBlock;
        public readonly bool Mosaic          = mosaic;
        public readonly bool Use256Colors    = use256Colors;
        public readonly byte ScreenBaseBlock = screenBaseBlock;
        public readonly bool OverflowWrap    = overflowWrap;
        public readonly byte ScreenSize      = screenSize;

        public readonly ushort XOffset = xOffset;
        public readonly ushort YOffset = yOffset;

        public readonly short PA       = pa;
        public readonly short PB       = pb;
        public readonly short PC       = pc;
        public readonly short PD       = pd;
        public readonly int ReferenceX = refX;
        public readonly int ReferenceY = refY;
    }
}