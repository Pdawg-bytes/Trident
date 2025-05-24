using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe partial class ThumbArgumentDecoders
    {
        internal void HandleLdrPCRel(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;

            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
            args.Imm <<= 2;
        }

        internal void HandleLdrStrReg(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrReg(ref args, opcode);

        internal void HandleLdrbStrbReg(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrReg(ref args, opcode);

        internal void HandleLdrhStrhReg(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrReg(ref args, opcode);

        internal void HandleLdrsbLdrshReg(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.B = ~opcode.GetBit(11);
            args.Ro = opcode.Extract(8, 6);
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal void HandleLdrStrImm(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrImm(ref args, opcode, shift: 2);

        internal void HandleLdrbStrbImm(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrImm(ref args, opcode, shift: 0);

        internal void HandleLdrhStrhImm(ref ThumbArguments args, uint opcode) => HandleCommonLdrStrImm(ref args, opcode, shift: 1);

        private void HandleCommonLdrStrReg(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.L = opcode.GetBit(11);
            args.Ro = opcode.Extract(8, 6);
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        private void HandleCommonLdrStrImm(ref ThumbArguments args, uint opcode, int shift)
        {
            args.Opcode = opcode;
            args.L = opcode.GetBit(11);
            args.Imm = opcode.Extract(10, 6) << shift;
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal void HandleLdrStrSPRel(ref ThumbArguments args, uint opcode) => HandleSPPCOp(ref args, opcode, isAddSPPC: false);

        internal void HandleAddSPPC(ref ThumbArguments args, uint opcode) => HandleSPPCOp(ref args, opcode, isAddSPPC: true);

        private void HandleSPPCOp(ref ThumbArguments args, uint opcode, bool isAddSPPC)
        {
            args.Opcode = opcode;
            args.Imm = opcode.Extract(7, 0) << 2;
            args.Rd = opcode.Extract(10, 8);

            if (isAddSPPC)
                args.SP = opcode.GetBit(11);
            else
                args.L = opcode.GetBit(11);
        }

        internal void HandleAddSubSP(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.GetBit(11);
            args.S = opcode.GetBit(7);
            args.Imm = opcode.Extract(6, 0) << 2;
        }
    }
}