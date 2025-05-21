using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static class ThumbArgumentHandlers
    {
        internal static void NonImplementedArgHandler(ref ThumbArguments args, uint opcode) { }

        internal static void HandleAddSub(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.GetBit(9);

            args.I = opcode.GetBit(10);
            if (args.I == 0)
                args.Rn = opcode.Extract(8, 6);
            else
                args.Imm = opcode.Extract(8, 6);

            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleShiftImm(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.Extract(12, 11);

            args.Imm = opcode.Extract(10, 6);
            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleMovCmpAddSub(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.Extract(12, 11);

            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
        }

        internal static void HandleDataProcReg(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.Extract(9, 6);

            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleBX(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.Rd = opcode.GetBit(7) << 3;
            args.Rd |= opcode.Extract(2, 0);
            args.Rs = opcode.GetBit(6) << 3;
            args.Rs |= opcode.Extract(5, 3);
        }

        internal static void HandleHighRegOps(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.Extract(9, 8);

            args.Rd = opcode.GetBit(7) << 3;
            args.Rd |= opcode.Extract(2, 0);
            args.Rs = opcode.GetBit(6) << 3;
            args.Rs |= opcode.Extract(5, 3);
        }

        internal static void HandleLdrPCRel(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
            args.Imm <<= 2;
        }

        internal static void HandleGenericLdrStrReg(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;

            args.L = opcode.GetBit(11);
            args.Ro = opcode.Extract(8, 6);
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleLdrsbLdrshReg(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.B = ~opcode.GetBit(11);
            args.Ro = opcode.Extract(8, 6);
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleLdrStrImm(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.L = opcode.GetBit(11);
            args.Imm = opcode.Extract(10, 6);
            args.Imm <<= 2;
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleLdrbStrbImm(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.L = opcode.GetBit(11);
            args.Imm = opcode.Extract(10, 6);
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleLdrhStrhImm(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.L = opcode.GetBit(11);
            args.Imm = opcode.Extract(10, 6);
            args.Imm <<= 1;
            args.Rb = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleLdrStrSPRel(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.L = opcode.GetBit(11);
            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
            args.Imm <<= 2;
        }

        internal static void HandleAddSPPC(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.SP = opcode.GetBit(11);
            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
            args.Imm <<= 2;
        }

        internal static void HandleAddSubSP(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.GetBit(11);

            args.S = opcode.GetBit(7);
            args.Imm = opcode.Extract(6, 0);
            args.Imm <<= 2; 
        }

        internal static void HandlePushPop(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.GetBit(11);

            args.PC_LR = opcode.GetBit(8);
            args.Rlist = opcode.Extract(7, 0);
        }

        internal static void HandleLdmStm(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.GetBit(11);

            args.Rb = opcode.Extract(10, 8);
            args.Rlist = opcode.Extract(7, 0);
        }

        internal static void HandleSWI(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.Comment = opcode.Extract(7, 0);
        }

        internal static void HandleBCC(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.SubOpcode = opcode.Extract(11, 8);

            args.Imm = opcode.Extract(7, 0);
            args.Imm = args.Imm.Extend(8);
            args.Imm <<= 1;
        }

        internal static void HandleBUncond(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.Imm = opcode.Extract(10, 0);
            args.Imm = args.Imm.Extend(11);
            args.Imm <<= 1;
        }

        internal static void HandleBlBlxPrefix(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;

            args.Imm = opcode.Extract(10, 0);
            args.Imm = args.Imm.Extend(11);
            args.Imm <<= 12;
        }

        internal static void HandleBlSuffix(ref ThumbArguments args, uint opcode) 
        {
            args.Opcode = opcode;
            args.Imm = opcode.Extract(10, 0) << 1;
        }
    }
}