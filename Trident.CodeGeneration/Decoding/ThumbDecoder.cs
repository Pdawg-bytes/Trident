using System.Collections.Generic;
using Trident.CodeGeneration.Shared;

namespace Trident.CodeGeneration.Decoding
{
    internal static class ThumbDecoder
    {
        internal static ThumbGroup DetermineThumbGroup(uint instruction)
        {
            return ThumbGroup.Test;
        }

        internal static Dictionary<ThumbGroup, List<uint>> BuildGroupOpcodeMap()
        {
            var map = new Dictionary<ThumbGroup, List<uint>>();
            for (uint opcode = 0; opcode < 1024; opcode++)
            {
                uint expanded = opcode << 6;
                ThumbGroup group = DetermineThumbGroup(expanded);
                if (!map.TryGetValue(group, out var list))
                    map[group] = list = new List<uint>();
                list.Add(expanded);
            }
            return map;
        }
    }
}