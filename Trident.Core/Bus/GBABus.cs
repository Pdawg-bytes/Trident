using Trident.Core.Memory;
using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Bus
{
    internal struct GBABus : IDataBus
    {
        private MemoryAccessHandler[] _accessHandlers;
        private readonly MemoryAccessHandler _unusedSection;

        public GBABus()
        {
            UnusedSection unused = new();
            _unusedSection = unused.GetAccessHandler();

            _accessHandlers = 
            [
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
                _unusedSection,
            ];
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> for the given <paramref name="page"/>.
        /// </summary>
        /// <param name="page">The page that the handler should be registered to.</param>
        internal void RegisterHandler(int page, MemoryAccessHandler handler)
        {
            if (page < 0 || page >= 16)
                throw new ArgumentOutOfRangeException(nameof(page), $"Invalid page index {page}. Must be in 0..15.");

            _accessHandlers[page] = handler;
        }

        /// <summary>
        /// Registers multiple <see cref="MemoryAccessHandler"/>s at the respective pages.
        /// </summary>
        /// <param name="handlers">The list of handlers to register.</param>
        internal void RegisterHandlers((int page, MemoryAccessHandler handler)[] handlers)
        {
            if (handlers.Any(handler => handler.page < 0 || handler.page >= 16))
                throw new ArgumentOutOfRangeException(nameof(handlers), $"Invalid page index for one or more handlers.");

            foreach (var handler in handlers)
                _accessHandlers[handler.page] = handler.handler;
        }

        /// <summary>
        /// Deregisters the <see cref="MemoryAccessHandler"/> for the given <paramref name="page"/>, and disposes of the backing memory for the registered handler.
        /// </summary>
        /// <param name="page">The page at which the handler to deregister is located in.</param>
        internal void DeregisterHandler(int page)
        {
            if (page < 0 || page >= 16)
                throw new ArgumentOutOfRangeException(nameof(page), $"Invalid page index {page}. Must be in 0..15.");

            _accessHandlers[page].Dispose();
            _accessHandlers[page] = _unusedSection;
        }


        #region Read
        public byte Read8(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return (byte)ReadOpenBus(address);

            return _accessHandlers[section].Read8(address, access);
        }

        public ushort Read16(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return (ushort)ReadOpenBus(address);

            return _accessHandlers[section].Read16(address, access);
        }

        public uint Read32(uint address, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section > 15) return ReadOpenBus(address);

            return _accessHandlers[section].Read32(address, access);
        }
        #endregion

        #region Write
        public void Write8(uint address, byte value, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write8(address, access, value);
        }

        public void Write16(uint address, ushort value, PipelineAccess access)
        {
            uint section = address >> 24;
            if (section < 2 || section > 15)
            {
                // Step scheduler 1 cycle for invalid access
                return;
            }

            _accessHandlers[section].Write16(address, access, value);
        }

        public void Write32(uint address, uint value, PipelineAccess access)
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

        internal static void InvalidAccess(string section, uint address, bool write, uint value = 0)
        {
            Console.WriteLine($"Invalid {(write ? "write" : "read")} in {section} at 0x{address:X2} {(write ? $"with value 0x{value:X2}" : "")}");
        }


        internal void DisposeMemory()
        {
            foreach (var handler in _accessHandlers)
                handler.Dispose();
        }
    }
}