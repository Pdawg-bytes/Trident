using System.Runtime.CompilerServices;
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
        private int _sramWait;

        private int[] _ws0 = new int[2];
        private int[] _ws1 = new int[2];
        private int[] _ws2 = new int[2];

        private int _phiSpeed;

        private bool _prefetchEnabled;
        private const bool _isCGB = false;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte Read(bool upper)
        {
            // WAITCNT
            if (!upper) return (byte)
            (
                _sramWait |
                (_ws0[0] << 2) | (_ws0[1] << 4) |
                (_ws1[0] << 5) | (_ws1[1] << 7)
            );

            // WAITCNT + 1
            else return (byte)
            (
                _ws2[0] | (_ws2[1] << 2) |
                (_phiSpeed << 3) |
                (_prefetchEnabled ? 64 : 0) |
                (_isCGB ? 128 : 0)
            );
        }
        internal byte ReadLower() => Read(false);
        internal byte ReadUpper() => Read(true);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(bool upper, byte value)
        {
            // WAITCNT
            if (!upper)
            {
                _sramWait = value & 0b11;

                _ws0[0] = (value >> 2) & 0b11;
                _ws0[1] = (value >> 4) & 0b01;

                _ws1[0] = (value >> 5) & 0b11;
                _ws1[0] = (value >> 7) & 0b01;
            }

            // WAITCNT + 1
            else
            {
                _ws2[0] = value        & 0b11;
                _ws2[1] = (value >> 2) & 0b01;

                _phiSpeed = (value >> 3) & 0b01;

                _prefetchEnabled = ((value >> 6) & 1) != 0;
            }
        }
        internal void WriteLower(byte value) => Write(false, value);
        internal void WriteUpper(byte value) => Write(true, value);
    }
}