using Trident.Core.CPU;
using Trident.Core.Enums;
using Trident.Core.Memory;

namespace Trident.Core.Bus
{
    public class DataBus
    {
        private ARM7TDMI _cpu;

        private readonly BIOS _bios = new();
        private readonly UnusedSection _unused = new();
        private readonly EWRAM _eWRAM = new();
        private readonly IWRAM _iWRAM = new();

        private readonly MemoryAccessHandler[] _accessHandlers;

        public DataBus()
        {
            MemoryAccessHandler unusedHandler = _unused.GetAccessHandler();
            _accessHandlers = new MemoryAccessHandler[]
            {
                _bios.GetAccessHandler(),
                unusedHandler,
                _eWRAM.GetAccessHandler(),
                _iWRAM.GetAccessHandler(),
                unusedHandler, // MMIO
                unusedHandler, // PRAM
                unusedHandler, // VRAM
                unusedHandler, // OAM

                unusedHandler, // GamePak
                unusedHandler, // GamePak
                unusedHandler, // GamePak
                unusedHandler, // GamePak
                unusedHandler, // GamePak
                unusedHandler, // GamePak
                unusedHandler, // Backup
                unusedHandler, // Backup
            };
        }

        public void AttachComponents(ARM7TDMI cpu)
        {
            _cpu = cpu;
            _bios.AttachComponents(cpu);
        }

        #region Read
        internal unsafe byte Read8(uint address)
        {
            uint section = address >> 24;
            if (section > 15) return (byte)ReadOpenBus(address);

            return _accessHandlers[section].Read8(address);
        }

        internal unsafe ushort Read16(uint address)
        {
            uint section = address >> 24;
            if (section > 15) return (ushort)ReadOpenBus(address);

            return _accessHandlers[section].Read16(address);
        }

        internal unsafe uint Read32(uint address)
        {
            uint section = address >> 24;
            if (section > 15) return ReadOpenBus(address);

            return _accessHandlers[section].Read32(address);
        }
        #endregion

        #region Write
        internal unsafe void Write8(uint address, byte value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write8(address, value);
        }

        internal unsafe void Write16(uint address, ushort value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write16(address, value);
        }

        internal unsafe void Write32(uint address, uint value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write32(address, value);
        }
        #endregion

        private uint ReadOpenBus(uint address)
        {
            // TODO: Open bus behavior
            return 0;
        }

        internal static void InvalidAccess(MemorySection section, uint address, bool write, uint value = 0)
        {
            Console.WriteLine($"Invalid {(write ? "write" : "read")} in {section} at 0x{address:X2} {(write ? $"with value 0x{value:X2}" : "")}");
        }


        internal void DisposeMemory()
        {
            foreach (var handler in _accessHandlers)
                handler.Dispose();
        }

        ~DataBus() => DisposeMemory();
    }
}