using Trident.Core.CPU.Pipeline;
using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Hardware.IO
{
    internal class HaltControl(Action haltCPU, Func<uint> getPC)
    {
        private readonly Action _haltCPU = haltCPU;
        private readonly Func<uint> _getPC = getPC;

        internal void Write(byte value)
        {
            if (_getPC() > 0x3FFF) return;

            if ((value & 0x80) != 0)
            {
                // TODO: replace with log
                Console.WriteLine("HALTCNT bit 7 set");
            }
            else
                _haltCPU();
        }
    }


    internal class PostFlag(Func<uint> getPC)
    {
        private readonly Func<uint> _getPC = getPC;
        private byte _postFlag;

        internal byte Read() => _postFlag;
        internal void Write(byte value)
        {
            if (_getPC() > 0x3FFF) return;
            _postFlag |= (byte)(value & 1);
        }
    }


    internal class WaitControl
    {
        private ushort _waitcnt;

        private readonly uint[] _nonSequentialTimings = [ 4, 3, 2, 8 ];
        private readonly uint[] _sequentialWS0        = [ 2, 1 ];
        private readonly uint[] _sequentialWS1        = [ 4, 1 ];
        private readonly uint[] _sequentialWS2        = [ 8, 1 ];

        internal readonly uint[][] AccessTimings = new uint[2][];

        internal int PHITerminalSpeed;
        internal bool PrefetchEnabled;
        internal const bool IsCGB = false;

        internal WaitControl()
        {
            AccessTimings[(int)PipelineAccess.NonSequential] = [ 2, 4, 8, 4 ];
            AccessTimings[(int)PipelineAccess.Sequential]    = [ 4, 4, 4, 4 ];
        }


        internal byte ReadLower() => (byte)_waitcnt;
        internal byte ReadUpper() => (byte)((_waitcnt >> 8) & 0x7F); // Force GamePak type to 0

        internal void WriteLower(byte value)
        {
            int ws0First =  (value >> 2) & 0b11;
            int ws0Second = (value >> 4) & 0b01;
            int ws1First =  (value >> 5) & 0b11;
            int ws1Second = (value >> 7) & 0b01;

            AccessTimings[(int)PipelineAccess.NonSequential][0] = _nonSequentialTimings[ws0First];
            AccessTimings[(int)PipelineAccess.NonSequential][1] = _nonSequentialTimings[ws1First];

            AccessTimings[(int)PipelineAccess.Sequential][0] = _sequentialWS0[ws0Second];
            AccessTimings[(int)PipelineAccess.Sequential][1] = _sequentialWS1[ws1Second];

            // Backup uses the same timing for both
            int sramWait = value & 0b11;
            AccessTimings[(int)PipelineAccess.NonSequential][3] = _nonSequentialTimings[sramWait];
            AccessTimings[(int)PipelineAccess.Sequential][3]    = _nonSequentialTimings[sramWait];

            _waitcnt &= 0xFF00;
            _waitcnt |= value;
        }

        internal void WriteUpper(byte value)
        {
            int ws2First =  (value >> 0) & 0b11;
            int ws2Second = (value >> 2) & 0b01;

            AccessTimings[(int)PipelineAccess.NonSequential][2] = _nonSequentialTimings[ws2First];
            AccessTimings[(int)PipelineAccess.Sequential][2]    = _sequentialWS2[ws2Second];

            PHITerminalSpeed = (value >> 3) & 0b11;
            PrefetchEnabled = ((value >> 6) & 1) != 0;

            _waitcnt &= 0x00FF;
            _waitcnt |= (ushort)((value & 0x7F) << 8); // Force GamePak type to 0
        }
    }
}