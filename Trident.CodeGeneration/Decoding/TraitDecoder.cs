using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Trident.CodeGeneration.CodeGen;
using Trident.CodeGeneration.Attributes;

namespace Trident.CodeGeneration.Decoding
{
    internal static class TraitDecoder
    {
        internal static Dictionary<string, object> DecodeTraitValues(
            uint opcode,
            IMethodSymbol methodSymbol)
        {
            var traits = ARMAttributeParser.ParseFullTraits(methodSymbol);
            var results = new Dictionary<string, object>();

            foreach (var (name, type, size, bit, hi, lo) in traits)
            {
                object value = DecodeTrait(type, opcode, bit, hi, lo, size);
                results[name] = value;
            }

            return results;
        }

        private static object DecodeTrait(
            string type,
            uint opcode,
            int bit,
            int hi,
            int lo,
            int size)
        {
            return type switch
            {
                "bool" => DecodeBool(opcode, bit),
                "int" => (int)DecodeNumeric(opcode, hi, lo, size),
                "uint" => (uint)DecodeNumeric(opcode, hi, lo, size),
                "byte" => (byte)DecodeNumeric(opcode, hi, lo, size),
                "ushort" => (ushort)DecodeNumeric(opcode, hi, lo, size),
                _ => throw new NotSupportedException($"Unsupported trait type: {type}")
            };
        }

        private static bool DecodeBool(uint opcode, int bit)
        {
            if (bit == -1)
                throw new ArgumentException("Missing 'bit' for bool trait.");

            return opcode.IsBitSet(bit);
        }

        private static uint DecodeNumeric(uint opcode, int hi, int lo, int size)
        {
            uint value = opcode.Extract(hi, lo);

            if (size > 0 && size is int bits && bits < 32)
                value &= (uint)((1 << bits) - 1);

            return value;
        }
    }
}