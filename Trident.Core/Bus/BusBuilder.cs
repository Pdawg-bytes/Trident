using Trident.Core.Memory;

namespace Trident.Core.Bus;

internal class BusBuilder
{
    private readonly MemoryBase[] _handlers = new MemoryBase[16];

    internal void Attach(MemoryRegion region, MemoryBase handler) => _handlers[(int)region] = handler;

    internal GBABus Build(Action<uint> step)
    {
        GBABus bus = new(step);
        
        for (int i = 0; i < _handlers.Length; i++)
            if (_handlers[i] != null)
                bus.RegisterHandler(i, _handlers[i]);

        return bus;
    }
}