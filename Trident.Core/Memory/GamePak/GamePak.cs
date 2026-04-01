using Trident.Core.CPU;
using Trident.Core.Hardware.IO;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Memory.GamePak.Backup;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak;

internal sealed partial class GamePak : MemoryBase
{
    internal const int MaxSize = 32 * 1024 * 1024;
    internal const int HalfSize = MaxSize / 2;

    internal readonly int ActualSize;
    internal readonly GamePakInfo PakInfo;

    private readonly uint _romAddressMask;
    private uint _romAddress;
    private UnsafeMemoryBlock _romMemory;

    private readonly IBackupDevice? _backupDevice;
    private readonly bool _isEEPROM;
    private readonly uint _eepromMask;

    private readonly GPIOBus? _gpio;
    private readonly bool _isGPIO;

    private readonly WaitControl _waitControl;

    private readonly GamePakRegion _upperRegion;
    private readonly GamePakRegion _lowerRegion;
    private readonly BackupRegion? _backupRegion;

    public override uint BaseAddress => 0x08000000;
    public override uint Length      => MaxSize * 3;

    internal GamePak(byte[] romData, GamePakInfo info, Action<uint> step, WaitControl waitControl, IBackupDevice? backupDevice, GPIOBus? gpio)
        : base(0, step)
    {
        _romAddressMask = info.AddressMask;
        PakInfo         = info;
        ActualSize      = romData.Length;

        _romMemory = new((nuint)romData.Length);
        _romMemory.WriteBytes(0, romData);

        if (backupDevice != null)
        {
            _backupDevice = backupDevice;
            _isEEPROM     = backupDevice.Type.IsEEPROM();
            _eepromMask   = ActualSize > HalfSize ? 0x01FFFF00u : 0x01000000u;
            _backupRegion = new BackupRegion(this);
        }

        if (gpio != null)
        {
            _gpio   = gpio;
            _isGPIO = true;
            _gpio.Reset();
        }

        _waitControl = waitControl;
        _upperRegion = new GamePakRegion(this, isLower: false);
        _lowerRegion = new GamePakRegion(this, isLower: true);
    }

    internal MemoryBase GetUpperRegion()   => _upperRegion;
    internal MemoryBase GetLowerRegion()   => _lowerRegion;
    internal MemoryBase? GetBackupRegion() => _backupRegion;

    internal T? GetGPIODevice<T>() where T : GPIODevice => _gpio?.GetDevice<T>();
    internal IBackupDevice? GetBackupDevice()           => _backupDevice;


    public override T DebugRead<T>(uint address)
    {
        const uint ROMMirrorMask = 0x0E000000;
        uint normalized = (address & ~ROMMirrorMask) | BaseAddress;
        uint offset = normalized - BaseAddress;

        if (offset + (uint)Unsafe.SizeOf<T>() <= _romMemory.Size)
            return _romMemory.Read<T>(offset);

        return default!;
    }


    public override void Dispose()
    {
        _romMemory.Dispose();
        _backupDevice?.Dispose();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSequential(uint address, PipelineAccess access)
    {
        bool markedSequential = (access & PipelineAccess.Sequential) != 0;
        bool boundaryAligned  = (address & 0x1FFFF) == 0;
        bool dmaTransition    = ((int)access & (int)PipelineAccess.DMA) != 0;

        return markedSequential && !boundaryAligned && !dmaTransition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEEPROMAddress(uint address) => _isEEPROM && ((address & _eepromMask) == _eepromMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsGPIOAddress(uint address) => _isGPIO && address >= 0xC4 && address <= 0xC8;
}