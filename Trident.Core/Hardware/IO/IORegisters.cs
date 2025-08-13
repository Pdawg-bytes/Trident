namespace Trident.Core.Hardware.IO
{
    internal static class IORegisters
    {
        internal const uint DISPCNT = 0x04000000;
        internal const uint GREENSWAP = 0x04000002;
        internal const uint DISPSTAT = 0x04000004;
        internal const uint VCOUNT = 0x04000006;
        internal const uint BG0CNT = 0x04000008;
        internal const uint BG1CNT = 0x0400000A;
        internal const uint BG2CNT = 0x0400000C;
        internal const uint BG3CNT = 0x0400000E;
        internal const uint BG0HOFS = 0x04000010;
        internal const uint BG0VOFS = 0x04000012;
        internal const uint BG1HOFS = 0x04000014;
        internal const uint BG1VOFS = 0x04000016;
        internal const uint BG2HOFS = 0x04000018;
        internal const uint BG2VOFS = 0x0400001A;
        internal const uint BG3HOFS = 0x0400001C;
        internal const uint BG3VOFS = 0x0400001E;
        internal const uint BG2PA = 0x04000020;
        internal const uint BG2PB = 0x04000022;
        internal const uint BG2PC = 0x04000024;
        internal const uint BG2PD = 0x04000026;
        internal const uint BG2X = 0x04000028;
        internal const uint BG2Y = 0x0400002C;
        internal const uint BG3PA = 0x04000030;
        internal const uint BG3PB = 0x04000032;
        internal const uint BG3PC = 0x04000034;
        internal const uint BG3PD = 0x04000036;
        internal const uint BG3X = 0x04000038;
        internal const uint BG3Y = 0x0400003C;
        internal const uint WIN0H = 0x04000040;
        internal const uint WIN1H = 0x04000042;
        internal const uint WIN0V = 0x04000044;
        internal const uint WIN1V = 0x04000046;
        internal const uint WININ = 0x04000048;
        internal const uint WINOUT = 0x0400004A;
        internal const uint MOSAIC = 0x0400004C;
        internal const uint BLDCNT = 0x04000050;
        internal const uint BLDALPHA = 0x04000052;
        internal const uint BLDY = 0x04000054;

        internal const uint SOUND1CNT_L = 0x04000060;
        internal const uint SOUND1CNT_H = 0x04000062;
        internal const uint SOUND1CNT_X = 0x04000064;
        internal const uint SOUND2CNT_L = 0x04000068;
        internal const uint SOUND2CNT_H = 0x0400006C;
        internal const uint SOUND3CNT_L = 0x04000070;
        internal const uint SOUND3CNT_H = 0x04000072;
        internal const uint SOUND3CNT_X = 0x04000074;
        internal const uint SOUND4CNT_L = 0x04000078;
        internal const uint SOUND4CNT_H = 0x0400007C;
        internal const uint SOUNDCNT_L = 0x04000080;
        internal const uint SOUNDCNT_H = 0x04000082;
        internal const uint SOUNDCNT_X = 0x04000084;
        internal const uint SOUNDBIAS = 0x04000088;
        internal const uint WAVE_RAM = 0x04000090;
        internal const uint FIFO_A = 0x040000A0;
        internal const uint FIFO_B = 0x040000A4;

        internal const uint DMA0SAD = 0x040000B0;
        internal const uint DMA0DAD = 0x040000B4;
        internal const uint DMA0CNT_L = 0x040000B8;
        internal const uint DMA0CNT_H = 0x040000BA;
        internal const uint DMA1SAD = 0x040000BC;
        internal const uint DMA1DAD = 0x040000C0;
        internal const uint DMA1CNT_L = 0x040000C4;
        internal const uint DMA1CNT_H = 0x040000C6;
        internal const uint DMA2SAD = 0x040000C8;
        internal const uint DMA2DAD = 0x040000CC;
        internal const uint DMA2CNT_L = 0x040000D0;
        internal const uint DMA2CNT_H = 0x040000D2;
        internal const uint DMA3SAD = 0x040000D4;
        internal const uint DMA3DAD = 0x040000D8;
        internal const uint DMA3CNT_L = 0x040000DC;
        internal const uint DMA3CNT_H = 0x040000DE;

        internal const uint TM0CNT_L = 0x04000100;
        internal const uint TM0CNT_H = 0x04000102;
        internal const uint TM1CNT_L = 0x04000104;
        internal const uint TM1CNT_H = 0x04000106;
        internal const uint TM2CNT_L = 0x04000108;
        internal const uint TM2CNT_H = 0x0400010A;
        internal const uint TM3CNT_L = 0x0400010C;
        internal const uint TM3CNT_H = 0x0400010E;

        internal const uint SIODATA32_L = 0x04000120;
        internal const uint SIODATA32_H = 0x04000122;
        internal const uint SIOMULTI0 = 0x04000120;
        internal const uint SIOMULTI1 = 0x04000122;
        internal const uint SIOMULTI2 = 0x04000124;
        internal const uint SIOMULTI3 = 0x04000126;
        internal const uint SIOCNT = 0x04000128;
        internal const uint SIOMLT_SEND = 0x0400012A;
        internal const uint SIODATA8 = 0x0400012A;

        internal const uint KEYINPUT = 0x04000130;
        internal const uint KEYCNT = 0x04000132;

        internal const uint RCNT = 0x04000134;
        internal const uint JOYCNT = 0x04000140;
        internal const uint JOY_RECV = 0x04000150;
        internal const uint JOY_TRANS = 0x04000154;
        internal const uint JOYSTAT = 0x04000158;

        internal const uint IE = 0x04000200;
        internal const uint IF = 0x04000202;
        internal const uint WAITCNT = 0x04000204;
        internal const uint IME = 0x04000208;
        internal const uint POSTFLG = 0x04000300;
        internal const uint HALTCNT = 0x04000301;
    }
}