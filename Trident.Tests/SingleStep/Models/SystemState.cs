using System.Text.Json.Serialization;

namespace Trident.Tests.SingleStep.Models;

public record IndexedTestCase(int Index, SystemState TestCase);

public class SystemState
{
    [JsonPropertyName("initial")]
    public RegisterState Initial { get; set; }

    [JsonPropertyName("final")]
    public RegisterState Final { get; set; }

    [JsonPropertyName("transactions")]
    public List<Transaction> Transactions { get; set; }

    [JsonPropertyName("opcode")]
    public uint Opcode { get; set; }

    [JsonPropertyName("base_addr")]
    public uint BaseAddr { get; set; }

    [JsonIgnore]
    public bool IgnoreAccessMismatch { get; set; }
}

public class RegisterState
{
    [JsonPropertyName("R")]
    public List<uint> R { get; set; }

    [JsonPropertyName("R_fiq")]
    public List<uint> RFiq { get; set; }

    [JsonPropertyName("R_svc")]
    public List<uint> RSvc { get; set; }

    [JsonPropertyName("R_abt")]
    public List<uint> RAbt { get; set; }

    [JsonPropertyName("R_irq")]
    public List<uint> RIrq { get; set; }

    [JsonPropertyName("R_und")]
    public List<uint> RUnd { get; set; }

    [JsonPropertyName("CPSR")]
    public uint Cpsr { get; set; }

    [JsonPropertyName("SPSR")]
    public List<uint> Spsr { get; set; }

    [JsonPropertyName("pipeline")]
    public List<uint> Pipeline { get; set; }

    [JsonPropertyName("access")]
    public int Access { get; set; }
}

public class Transaction
{
    [JsonPropertyName("kind")]
    public int Kind { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("addr")]
    public uint Addr { get; set; }

    [JsonPropertyName("data")]
    public uint Data { get; set; }

    [JsonPropertyName("cycle")]
    public int Cycle { get; set; }

    [JsonPropertyName("access")]
    public int Access { get; set; }
}