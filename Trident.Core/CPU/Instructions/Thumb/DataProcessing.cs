using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void Thumb_AddSub(ref ThumbArguments args)
        {
            uint op = args.I != 0 ? args.Imm : Registers[args.Rn];

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;

            uint rd = args.Rd;
            uint src = Registers[args.Rs];

            if (args.SubOp != 0) 
                Registers[rd] = Subtract(src, op, modifyFlags: true);
            else 
                Registers[rd] = Add(src, op, modifyFlags: true);
        }

        internal void Thumb_MovCmpAddSubImm(ref ThumbArguments args)
        {
            uint rd = args.Rd;
            uint imm = args.Imm;

            switch (args.SubOp)
            {
                case 0: // MOV
                    Registers[rd] = imm;
                    Registers.ClearFlag(Flags.N);
                    Registers.ModifyFlag(Flags.Z, imm == 0);
                    break;
                case 1: // CMP
                    Subtract(Registers[rd], imm, modifyFlags: true);
                    break;
                case 2: // ADD
                    Registers[rd] = Add(Registers[rd], imm, modifyFlags: true);
                    break;
                case 3: // SUB
                    Registers[rd] = Subtract(Registers[rd], imm, modifyFlags: true);
                    break;
            }

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }

        internal void Thumb_HighRegister(ref ThumbArguments args)
        {
            uint rd = args.Rd;
            uint dst = Registers[rd];
            uint op = Registers[args.Rs];

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;

            switch (args.SubOp)
            {
                case 0: // ADD
                    Registers[rd] = dst + op;
                    break;
                case 1: // CMP
                    Subtract(dst, op, modifyFlags: true);
                    return;
                case 2: // MOV
                    Registers[rd] = op;
                    break;
                case 3:
                    throw new InvalidInstructionException<TBus>("BX encoded in high-register operation.", this);
            }

            if (rd == 15)
            {
                Registers.PC &= 0xFFFFFFFE;
                ReloadPipelineThumb();
            }
        }
    }
}