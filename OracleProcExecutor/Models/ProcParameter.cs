namespace OracleProcExecutor.Models;

/// <summary>
/// Represents one parameter in the Execute request.
/// </summary>
public class ProcParameter
{
    /// <summary>Parameter name as declared in Oracle (e.g. "P_MSISDN").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>"IN", "OUT", or "INOUT".</summary>
    public string Direction { get; set; } = "IN";

    /// <summary>Value for IN / INOUT parameters. Null for pure OUT parameters.</summary>
    public object? Value { get; set; }
}
