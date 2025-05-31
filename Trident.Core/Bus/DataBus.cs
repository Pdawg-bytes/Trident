using Trident.Core.CPU;
using Trident.Core.Enums;
using Trident.Core.Memory;

namespace Trident.Core.Bus
{
    public class DataBus
    {
        private MemoryAccessHandler[] _accessHandlers = new MemoryAccessHandler[16];


        #region Read
        internal byte Read8(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return (byte)ReadOpenBus(address);

            return _accessHandlers[section].Read8(address, access);
        }

        internal ushort Read16(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return (ushort)ReadOpenBus(address);

            return _accessHandlers[section].Read16(address, access);
        }

        internal uint Read32(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return ReadOpenBus(address);

            return _accessHandlers[section].Read32(address, access);
        }
        #endregion

        #region Write
        internal void Write8(uint address, PipelineAccess access, byte value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write8(address, access, value);
        }

        internal void Write16(uint address, PipelineAccess access, ushort value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write16(address, access, value);
        }

        internal void Write32(uint address, PipelineAccess access, uint value)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write32(address, access, value);
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