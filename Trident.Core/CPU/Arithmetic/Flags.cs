using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Registers;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetZeroSign(uint value)
        {
            Registers.ModifyFlag(Flags.Z, value == 0);
            Registers.ModifyFlag(Flags.N, value.IsBitSet(31));
        }
    }
}