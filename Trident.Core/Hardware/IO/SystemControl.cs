using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Hardware.IO
{
    internal class PostHalt(Action haltCPU, Func<uint> getPC)
    {
        private readonly Action _haltCPU = haltCPU;
        private readonly Func<uint> _getPC = getPC;
        private byte _postFlag;


        // HALTCNT is not readable - only POSTFLG is. Therefore, we can just return POSTFLG as it's the low byte.
        internal ushort Read() => _postFlag;

        internal void Write(ushort value, bool upper, bool lower)
        {
            if (_getPC() > 0x3FFF)
                return;

            if (lower)
                _postFlag |= (byte)(value & 1);

            if (upper)
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
    }


    internal class WaitControl
    {
        private ushort _waitcnt;

        private ReadOnlySpan<uint> NonSequentialTimings => [ 4, 3, 2, 8 ];
        private ReadOnlySpan<uint> SequentialWS0        => [ 2, 1 ];
        private ReadOnlySpan<uint> SequentialWS1        => [ 4, 1 ];
        private ReadOnlySpan<uint> SequentialWS2        => [ 8, 1 ];

        internal readonly uint[][] AccessTimings = new uint[2][];

        internal int PHITerminalSpeed;
        internal bool PrefetchEnabled;
        internal const bool IsCGB = false;

        internal WaitControl()
        {
            AccessTimings[(int)PipelineAccess.NonSequential] = [ 2, 4, 8, 4 ];
            AccessTimings[(int)PipelineAccess.Sequential]    = [ 4, 4, 4, 4 ];
        }


        internal ushort Read() => (ushort)(_waitcnt & 0x7FFF);

        internal void Write(ushort value, bool upper, bool lower)
        {
            if (lower)
            {
                int ws0First  = (value >> 2) & 0b11;
                int ws0Second = (value >> 4) & 0b01;
                int ws1First  = (value >> 5) & 0b11;
                int ws1Second = (value >> 7) & 0b01;

                AccessTimings[(int)PipelineAccess.NonSequential][0] = NonSequentialTimings[ws0First];
                AccessTimings[(int)PipelineAccess.NonSequential][1] = NonSequentialTimings[ws1First];

                AccessTimings[(int)PipelineAccess.Sequential][0] = SequentialWS0[ws0Second];
                AccessTimings[(int)PipelineAccess.Sequential][1] = SequentialWS1[ws1Second];

                // Backup uses the same timing for both
                int sramWait = value & 0b11;
                AccessTimings[(int)PipelineAccess.NonSequential][3] = NonSequentialTimings[sramWait];
                AccessTimings[(int)PipelineAccess.Sequential][3]    = NonSequentialTimings[sramWait];

                _waitcnt = (ushort)((_waitcnt & 0xFF00) | (value & 0x00FF));
            }

            if (upper)
            {
                int ws2First  = (value >> 0) & 0b11;
                int ws2Second = (value >> 2) & 0b01;

                AccessTimings[(int)PipelineAccess.NonSequential][2] = NonSequentialTimings[ws2First];
                AccessTimings[(int)PipelineAccess.Sequential][2]    = SequentialWS2[ws2Second];

                PHITerminalSpeed = (value >> 3) & 0b11;
                PrefetchEnabled = ((value >> 6) & 1) != 0;

                _waitcnt = (ushort)((_waitcnt & 0x00FF) | (value & 0x7F00));
            }
        }
    }
}