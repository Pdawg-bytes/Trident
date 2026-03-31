using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Memory.Region;

namespace Trident.Core.Bus;

public readonly struct GBABus : IDataBus
{
    private readonly Action<uint> _step;

    private readonly IMemoryRegion[] _accessHandlers;
    private readonly IDebugMemory?[] _debugHandlers = new IDebugMemory?[16];

    private readonly IMemoryRegion _unusedSection;
    private readonly UnusedSection _unused;

    public GBABus(Action<uint> step)
    {
        _step = step;

        _unused = new(step);
        _unusedSection = _unused;

        _accessHandlers = 
        [
            _unusedSection, // BIOS
            _unusedSection, // Unused
            _unusedSection, // EWRAM
            _unusedSection, // IWRAM
            _unusedSection, // MMIO
            _unusedSection, // PRAM
            _unusedSection, // VRAM
            _unusedSection, // OAM
            _unusedSection, // GamePak L WS0
            _unusedSection, // GamePak H WS0
            _unusedSection, // GamePak L WS1
            _unusedSection, // GamePak H WS1
            _unusedSection, // GamePak L WS2
            _unusedSection, // GamePak H WS2
            _unusedSection, // GamePak Backup
            _unusedSection, // GamePak Backup
        ];
    }

    /// <summary>
    /// Registers the <paramref name="handler"/> for the given <paramref name="page"/>.
    /// </summary>
    /// <param name="page">The page that the handler should be registered to.</param>
    internal void RegisterHandler(int page, IMemoryRegion handler)
    {
        if (page < 0 || page >= 16)
            throw new ArgumentOutOfRangeException(nameof(page), $"Invalid page index {page}. Must be in 0..15.");

        if (handler is null) return;

        _accessHandlers[page] = handler;

        if (page < 8)
            _debugHandlers[page] = handler as IDebugMemory;
    }

    /// <summary>
    /// Registers multiple <see cref="MemoryAccessHandler"/>s at the respective pages.
    /// </summary>
    /// <param name="handlers">The list of handlers to register.</param>
    internal void RegisterHandlers((int page, IMemoryRegion handler)[] handlers)
    {
        if (handlers.Any(handler => handler.page < 0 || handler.page >= 16))
            throw new ArgumentOutOfRangeException(nameof(handlers), $"Invalid page index for one or more handlers.");

        foreach (var handler in handlers.Where(mapping => mapping.handler is not null))
        {
            _accessHandlers[handler.page] = handler.handler;

            if (handler.page < 8)
                _debugHandlers[handler.page] = handler.handler as IDebugMemory;
        }
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
        _debugHandlers[page] = null;
    }


    internal void LoadDebugGamePak(IDebugMemory handler)
    {
        for (int i = 8; i < 15; i++)
            _debugHandlers[i] = handler;
    }

    internal IDebugMemory? GetRegionAsDebug(uint region) => (region >= _debugHandlers.Length) ? null : _debugHandlers[region];


    #region Read
    public byte Read8(uint address, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region > 0x0F) return (byte)ReadOpenBus(address);

        return _accessHandlers[region].Read8(address, access);
    }

    public ushort Read16(uint address, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region > 0x0F) return (ushort)ReadOpenBus(address);

        return _accessHandlers[region].Read16(address, access);
    }

    public uint Read32(uint address, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region > 0x0F) return ReadOpenBus(address);

        return _accessHandlers[region].Read32(address, access);
    }
    #endregion

    #region Write
    public void Write8(uint address, byte value, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region < 2 || region > 0x0F)
        {
            _step(1);
            return;
        }

        _accessHandlers[region].Write8(address, access, value);
    }

    public void Write16(uint address, ushort value, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region < 2 || region > 0x0F)
        {
            _step(1);
            return;
        }

        _accessHandlers[region].Write16(address, access, value);
    }

    public void Write32(uint address, uint value, PipelineAccess access)
    {
        uint region = address >> 24;
        if (region < 2 || region > 0x0F)
        {
            _step(1);
            return;
        }

        _accessHandlers[region].Write32(address, access, value);
    }
    #endregion

    private uint ReadOpenBus(uint address)
    {
        _step(1);
        // TODO: Open bus behavior
        return 0;
    }


    internal void DisposeMemory()
    {
        foreach (var handler in _accessHandlers.Where(h => h is not null))
            handler.Dispose();
    }
}