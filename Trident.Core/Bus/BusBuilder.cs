using Trident.Core.Memory.Region;

namespace Trident.Core.Bus;

internal class BusBuilder
{
    private readonly IMemoryRegion[] _handlers = new IMemoryRegion[16];

    internal void Attach(MemoryRegion region, IMemoryRegion handler) => _handlers[(int)region] = handler;

    internal GBABus Build(Action<uint> step)
    {
        GBABus bus = new(step);
        
        for (int i = 0; i < _handlers.Length; i++)
            bus.RegisterHandler(i, _handlers[i]);

        return bus;
    }
}