using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Memory;

namespace Trident.Core.Bus
{
    internal class DataBus
    {
        private readonly MemoryAccessHandler[] _accessHandlers;

        internal DataBus()
        {
            _accessHandlers = new MemoryAccessHandler[16];
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
    }
}