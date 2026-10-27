namespace Trident.Core.Debugging.Snapshots;

public enum BlendModeKind : byte
{
    None     = 0,
    Alpha    = 1,
    Brighten = 2,
    Darken   = 3
}

public readonly struct EffectsSnapshot
(
    bool window0Enabled, bool window1Enabled, bool objWindowEnabled,
    in EffectsSnapshot.WindowSnapshot window0,
    in EffectsSnapshot.WindowSnapshot window1,
    in EffectsSnapshot.RegionSnapshot outside,
    in EffectsSnapshot.RegionSnapshot objWindow,
    in EffectsSnapshot.MosaicSnapshot mosaic,
    in EffectsSnapshot.BlendSnapshot blend
)
{
    public readonly bool Window0Enabled   = window0Enabled;
    public readonly bool Window1Enabled   = window1Enabled;
    public readonly bool ObjWindowEnabled = objWindowEnabled;

    public readonly WindowSnapshot Window0 = window0;
    public readonly WindowSnapshot Window1 = window1;

    public readonly RegionSnapshot Outside   = outside;
    public readonly RegionSnapshot ObjWindow = objWindow;

    public readonly MosaicSnapshot Mosaic = mosaic;
    public readonly BlendSnapshot Blend   = blend;


    public readonly struct RegionSnapshot(byte layers, bool objEnabled, bool effectsEnabled)
    {
        public readonly byte Layers         = layers;
        public readonly bool ObjEnabled     = objEnabled;
        public readonly bool EffectsEnabled = effectsEnabled;
    }

    public readonly struct WindowSnapshot(byte left, byte right, byte top, byte bottom, in RegionSnapshot region)
    {
        public readonly byte Left   = left;
        public readonly byte Right  = right;
        public readonly byte Top    = top;
        public readonly byte Bottom = bottom;

        public readonly RegionSnapshot Region = region;
    }

    public readonly struct MosaicSnapshot(byte bgH, byte bgV, byte objH, byte objV)
    {
        public readonly byte BGHSize  = bgH;
        public readonly byte BGVSize  = bgV;
        public readonly byte OBJHSize = objH;
        public readonly byte OBJVSize = objV;
    }

    public readonly struct BlendSnapshot(BlendModeKind mode, byte firstTargets, byte secondTargets, byte eva, byte evb, byte evy)
    {
        public readonly BlendModeKind Mode = mode;

        public readonly byte FirstTargets  = firstTargets;
        public readonly byte SecondTargets = secondTargets;

        public readonly byte EVA = eva;
        public readonly byte EVB = evb;
        public readonly byte EVY = evy;
    }
}
