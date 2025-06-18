using Trident.Core.Bus;
using Trident.Core.Global;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void ARM_BX(uint opcode)
        {
            uint address = Registers[opcode & 0b1111];

            Registers.PC = address & 0xFFFFFFFE;

            if ((address & 1) != 0)
            {
                Registers.SetFlag(Enums.Flags.T);
                ReloadPipelineThumb();
            }
            else
                ReloadPipelineARM();
        }

        internal void ARM_B_BL(uint opcode)
        {
            bool link = opcode.IsBitSet(24);
            int offset = ((opcode & 0xFFFFFF).Extend(24)) << 2;

            if (link) Registers.LR = Registers.PC - 4;
            Registers.PC += (uint)offset;

            ReloadPipelineARM();
        }
    }
}