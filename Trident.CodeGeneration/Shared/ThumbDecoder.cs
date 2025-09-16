using System.Collections.Generic;

namespace Trident.CodeGeneration.Shared
{
    internal static class ThumbDecoder
    {
        internal static ThumbGroup DetermineThumbGroup(ushort instruction) => instruction switch
        {
            ushort i when (i & 0xF800) < 0x1800 =>  ThumbGroup.ShiftImmediate,
            ushort i when (i & 0xF800) == 0x1800 => ThumbGroup.AddSubtract,
            ushort i when (i & 0xE000) == 0x2000 => ThumbGroup.ImmediateOperations,
            ushort i when (i & 0xFC00) == 0x4000 => ThumbGroup.ThumbALU,
            ushort i when (i & 0xFC00) == 0x4400 && (i >> 8 & 0b11) == 0b11 => ThumbGroup.BranchExchange,
            ushort i when (i & 0xFC00) == 0x4400 => ThumbGroup.HiRegisterOps,
            ushort i when (i & 0xF800) == 0x4800 => ThumbGroup.LoadPCRelative,
            ushort i when (i & 0xF200) == 0x5000 => ThumbGroup.LoadStoreRegOffset,
            ushort i when (i & 0xF200) == 0x5200 => ThumbGroup.LoadStoreSigned,
            ushort i when (i & 0xE000) == 0x6000 => ThumbGroup.LoadStoreImmOffset,
            ushort i when (i & 0xF000) == 0x8000 => ThumbGroup.LoadStore16,
            ushort i when (i & 0xF000) == 0x9000 => ThumbGroup.LoadStoreSPRelative,
            ushort i when (i & 0xF000) == 0xA000 => ThumbGroup.LoadAddress,
            ushort i when (i & 0xFF00) == 0xB000 => ThumbGroup.AddOffsetSP,
            ushort i when (i & 0xF600) == 0xB400 => ThumbGroup.PushPop,
            ushort i when (i & 0xF000) == 0xC000 => ThumbGroup.LoadStoreMultiple,
            ushort i when (i & 0xFF00) < 0xDF00 =>  ThumbGroup.ConditionalBranch,
            ushort i when (i & 0xFF00) == 0xDF00 => ThumbGroup.SoftwareInterrupt,
            ushort i when (i & 0xF800) == 0xE000 => ThumbGroup.UnconditionalBranch,
            ushort i when (i & 0xF000) == 0xF000 => ThumbGroup.LongBranchWithLink,
            _ => ThumbGroup.Undefined
        };


        internal static Dictionary<ThumbGroup, List<uint>> BuildGroupOpcodeMap()
        {
            var map = new Dictionary<ThumbGroup, List<uint>>();
            for (uint opcode = 0; opcode < 1024; opcode++)
            {
                ushort expanded = (ushort)(opcode << 6);
                ThumbGroup group = DetermineThumbGroup(expanded);
                if (!map.TryGetValue(group, out var list))
                    map[group] = list = new List<uint>();
                list.Add(expanded);
            }
            return map;
        }
    }
}