using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void Thumb_SWI(ref ThumbArguments args)
        {
            Registers.SetSPSR(PrivilegeMode.Supervisor, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            Registers.LR = Registers.PC - 2; // LR is now R14_svc because of mode switch.

            Registers.ClearFlag(Flags.T);
            Registers.SetFlag(Flags.I);

            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;
            Registers.PC = 0x00000008;
            ReloadPipelineARM();
        }

        internal void Thumb_AddSpecialOffset(ref ThumbArguments args)
        {
            if (args.SP != 0)
                Registers[args.Rd] = Registers.SP + args.Imm;
            else
                Registers[args.Rd] = (Registers.PC & 0xFFFFFFFD) + args.Imm;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}