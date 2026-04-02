using Trident.Core.CPU;
using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.IO;

internal class PostHalt(Action haltCPU, Func<uint> getPC)
{
    private readonly Action _haltCPU = haltCPU;
    private readonly Func<uint> _getPC = getPC;
    private byte _postFlag;


    // HALTCNT is not readable - only POSTFLG is. Therefore, we can just return POSTFLG as it's the low byte.
    internal ushort Read() => _postFlag;

    internal void Write(ushort value, WriteMask mask)
    {
        if (_getPC() > 0x3FFF)
            return;

        if (mask.IsLower())
            _postFlag |= (byte)(value & 1);

        if (mask.IsUpper())
        {
            if ((value & 0x8000) != 0)
            {
                // TODO: replace with proper logging
                Console.WriteLine("HALTCNT bit 7 set");
            }
            else
                _haltCPU();
        }
    }


    internal void Reset() => _postFlag = 0;
}


internal class WaitControl
{
    private ushort _waitcnt;

    private ReadOnlySpan<uint> NonSequentialTimings => [ 5, 4, 3, 9 ];
    private ReadOnlySpan<uint> SequentialWS0        => [ 3, 2 ];
    private ReadOnlySpan<uint> SequentialWS1        => [ 5, 2 ];
    private ReadOnlySpan<uint> SequentialWS2        => [ 9, 2 ];

    internal readonly uint[][] AccessTimings16 = new uint[2][];
    internal readonly uint[][] AccessTimings32 = new uint[2][];

    internal int PHITerminalSpeed;
    internal bool PrefetchEnabled;
    internal const bool IsCGB = false;

    internal WaitControl()
    {
        AccessTimings16[(int)PipelineAccess.NonSequential] = new uint[4];
        AccessTimings16[(int)PipelineAccess.Sequential]    = new uint[4];
        AccessTimings32[(int)PipelineAccess.NonSequential] = new uint[4];
        AccessTimings32[(int)PipelineAccess.Sequential]    = new uint[4];

        Reset();
    }


    internal ushort Read() => (ushort)(_waitcnt & 0x7FFF);

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            int ws0First  = (value >> 2) & 0b11;
            int ws0Second = (value >> 4) & 0b01;
            int ws1First  = (value >> 5) & 0b11;
            int ws1Second = (value >> 7) & 0b01;

            SetWaitstatesROM(0, NonSequentialTimings[ws0First], SequentialWS0[ws0Second]);
            SetWaitstatesROM(1, NonSequentialTimings[ws1First], SequentialWS1[ws1Second]);

            // Backup uses the same timing for both
            int sramWait = value & 0b11;
            AccessTimings16[(int)PipelineAccess.NonSequential][3] = NonSequentialTimings[sramWait];
            AccessTimings16[(int)PipelineAccess.Sequential][3]    = NonSequentialTimings[sramWait];
            AccessTimings32[(int)PipelineAccess.NonSequential][3] = NonSequentialTimings[sramWait];
            AccessTimings32[(int)PipelineAccess.Sequential][3]    = NonSequentialTimings[sramWait];

            _waitcnt = (ushort)((_waitcnt & 0xFF00) | (value & 0x00FF));
        }

        if (mask.IsUpper())
        {
            int ws2First  = (value >> 8)  & 0b11;
            int ws2Second = (value >> 10) & 0b01;

            SetWaitstatesROM(2, NonSequentialTimings[ws2First], SequentialWS2[ws2Second]);

            PHITerminalSpeed =  (value >> 11) & 0b11;
            PrefetchEnabled  = ((value >> 14) & 0b01) != 0;

            _waitcnt = (ushort)((_waitcnt & 0x00FF) | (value & 0x7F00));
        }
    }

    private void SetWaitstatesROM(uint region, uint nonSeq, uint seq)
    {
        AccessTimings16[(int)PipelineAccess.NonSequential][region] = nonSeq;
        AccessTimings32[(int)PipelineAccess.NonSequential][region] = nonSeq + seq;

        AccessTimings16[(int)PipelineAccess.Sequential][region] = seq;
        AccessTimings32[(int)PipelineAccess.Sequential][region] = seq + seq;
    }


    internal void Reset() => Write(0x0000, WriteMask.Both);
}