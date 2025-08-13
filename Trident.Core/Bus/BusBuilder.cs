using Trident.Core.Memory;

namespace Trident.Core.Bus
{
    internal class BusBuilder
    {
        private readonly MemoryAccessHandler[] _handlers = new MemoryAccessHandler[16];

        internal void Attach(MemoryRegion region, MemoryAccessHandler handler) => _handlers[(int)region] = handler;

        internal GBABus Build()
        {
            GBABus bus = new();
            
            for (int i = 0; i < _handlers.Length; i++)
                bus.RegisterHandler(i, _handlers[i]);

            return bus;
        }
    }
}