using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.Memory.Region;
using Trident.CodeGeneration.Shared;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Debugging.Disassembly
{
    public sealed class Disassembler
    {
        internal bool Enabled { get; set; }

        private readonly Func<uint, IDebugMemory?> _getRegion;

        private readonly Func<uint> _getPC;
        private readonly Func<CPUSnapshot> _getSnapshot;

        private readonly OpcodeDisasmCache<ushort> _thumbCache;
        private readonly OpcodeDisasmCache<uint> _armCache;

        private DisassembledInstruction[] _disasmBuffer = [];
        private int _disasmCount;

        public Disassembler(Func<uint, IDebugMemory?> getRegion, Func<uint> getPC, Func<CPUSnapshot> getSnapshot)
        {
            _getRegion   = getRegion;
            _getPC       = getPC;
            _getSnapshot = getSnapshot;

            _thumbCache = new(maxSize: 4096, usageThreshold: 3, opcode => ThumbDisassembler.Disassemble(0, 0, opcode));
            _armCache   = new(maxSize: 8192, usageThreshold: 3, opcode => ARMDisassembler.Disassemble(0, opcode));
        }


        public (uint ActualPC, bool Thumb, ReadOnlyMemory<DisassembledInstruction> Instructions) GetAroundPC(uint before, uint after)
        {
            if (!Enabled)
                return (0, false, ReadOnlyMemory<DisassembledInstruction>.Empty);

            uint lr = 0;

            uint pc = _getPC();
            bool thumb = _getSnapshot().CPSR.IsBitSet(5);

            IDebugMemory? region = _getRegion(pc >> 24);
            if (region is null)
                return (0, thumb, ReadOnlyMemory<DisassembledInstruction>.Empty);

            uint instrSize = thumb ? 2 : 4u;
            var (start, end) = GetDisasmWindow(pc, before, after, instrSize, region);

            int length = (int)((end - start) / instrSize);
            if (length > 512 || length < 0)
                throw new Exception("Disassembly window out of range.");

            if (_disasmBuffer.Length != length)
                _disasmBuffer = new DisassembledInstruction[length];

            _disasmCount = length;

            for (int i = 0; i < length; i++)
            {
                uint addr = start + (uint)(i * instrSize);

                if (thumb)
                {
                    ushort opcode = region.DebugRead<ushort>(addr);
                    var (group, cacheable) = ClassifyThumb(opcode);

                    DisassembledInstruction instr;
                    if (cacheable)
                    {
                        instr = _thumbCache.Get(opcode);
                        instr.Address = addr;
                        instr.Opcode = opcode;
                    }
                    else
                        instr = ThumbDisassembler.Disassemble(addr, lr, opcode, group);

                    _disasmBuffer[i] = instr;

                    if (group == ThumbGroup.LongBranchWithLink)
                    {
                        uint offset = (uint)((uint)opcode & 0x07FF).ExtendFrom(11) << 12;
                        lr = addr + 4 + offset;
                    }
                }
                else
                {
                    uint opcode = region.DebugRead<uint>(addr);
                    var (group, cacheable) = ClassifyARM(opcode);

                    DisassembledInstruction instr;
                    if (cacheable)
                    {
                        instr = _armCache.Get(opcode);
                        instr.Address = addr;
                        instr.Opcode = opcode;
                    }
                    else
                        instr = ARMDisassembler.Disassemble(addr, opcode, group);

                    _disasmBuffer[i] = instr;
                }
            }

            return (pc - (thumb ? 4 : 8u), thumb, _disasmBuffer.AsMemory(0, _disasmCount));
        }

        private static (uint start, uint end) GetDisasmWindow(uint pc, uint before, uint after, uint instrSize, IDebugMemory region)
        {
            before *= instrSize;
            after *= instrSize;

            uint min = region.BaseAddress;
            uint max = region.EndAddress;

            uint start = pc > before ? pc - before : min;
            if (start < min) start = min;

            uint end = pc + after;
            if (end > max) end = max;

            return (start, end);
        }


        private static (ThumbGroup group, bool cacheable) ClassifyThumb(ushort opcode)
        {
            ThumbGroup group = ThumbDecoder.DetermineThumbGroup(opcode);
            bool cacheable = group switch
            {
                ThumbGroup.LoadPCRelative      => false,
                ThumbGroup.LoadAddress         => false,
                ThumbGroup.ConditionalBranch   => false,
                ThumbGroup.UnconditionalBranch => false,
                ThumbGroup.LongBranchWithLink  => false,
                _ => true
            };
            return (group, cacheable);
        }

        private static (ARMGroup group, bool cacheable) ClassifyARM(uint opcode)
        {
            ARMGroup group = ARMDecoder.DetermineARMGroup(opcode);
            bool cacheable = group switch
            {
                ARMGroup.BranchWithLink => false,
                ARMGroup.DataProcessing => CachableDataProc(opcode),
                _ => true
            };
            return (group, cacheable);
        }

        private static bool CachableDataProc(uint opcode)
        {
            ALUOpARM op    = (ALUOpARM)((opcode >> 21) & 0x0F);
            uint rn        = (opcode >> 16) & 0xF;
            bool immediate = (opcode & (1 << 25)) != 0;

            return !(immediate && rn == 15 && (op == ALUOpARM.ADD || op == ALUOpARM.SUB));
        }
    }


    internal sealed class OpcodeDisasmCache<TOpcode> where TOpcode : unmanaged
    {
        private readonly int _maxSize;
        private readonly int _usageThreshold;

        private readonly Dictionary<TOpcode, int> _usageCount = [];
        private readonly LinkedList<TOpcode> _lruList = [];

        private readonly Dictionary<TOpcode, DisassembledInstruction> _cache = [];

        private readonly Func<TOpcode, DisassembledInstruction> _disasmFunc;

        internal OpcodeDisasmCache(int maxSize, int usageThreshold, Func<TOpcode, DisassembledInstruction> disasmFunc)
        {
            _maxSize = maxSize;
            _usageThreshold = usageThreshold;
            _disasmFunc = disasmFunc;
        }


        internal DisassembledInstruction Get(TOpcode opcode)
        {
            if (_cache.TryGetValue(opcode, out var cached))
            {
                _lruList.Remove(opcode);
                _lruList.AddLast(opcode);
                return cached;
            }

            int count = _usageCount.TryGetValue(opcode, out var c) ? c + 1 : 1;
            _usageCount[opcode] = count;

            if (count >= _usageThreshold)
            {
                var instr = _disasmFunc(opcode);

                if (_cache.Count >= _maxSize)
                {
                    var evictKey = _lruList.First!.Value;
                    _lruList.RemoveFirst();
                    _cache.Remove(evictKey);
                }

                _cache[opcode] = instr;
                _lruList.AddLast(opcode);
                return instr;
            }

            return _disasmFunc(opcode);
        }
    }


    public struct DisassembledInstruction(uint address, uint opcode, string mnemonicBase, string conditionCode, List<string> operands)
    {
        public uint Address = address;
        public uint Opcode = opcode;
        public string MnemonicBase = mnemonicBase;
        public string ConditionCode = conditionCode;
        public List<string> Operands = operands;
    }
}