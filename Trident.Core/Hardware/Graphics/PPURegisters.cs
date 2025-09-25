using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics
{
    internal class PPURegisters
    {
        internal DisplayControl DisplayControl = new();
        internal DisplayStatus DisplayStatus = new();

        internal uint Greenswap;

        internal uint VCount;

        internal BackgroundControl[] BackgroundControls =
        [
            new(0),
            new(1),
            new(2),
            new(3)
        ];


        internal void Reset()
        {
            DisplayControl.Write(0, WriteMask.Both);
            DisplayStatus.Write(0, WriteMask.Both);

            Greenswap = 0;
            VCount = 0;

            for (int i = 0; i < 4; i++)
                BackgroundControls[i].Write(0, WriteMask.Both);
        }
    }
}