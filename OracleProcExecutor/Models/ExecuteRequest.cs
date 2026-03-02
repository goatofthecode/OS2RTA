namespace OracleProcExecutor.Models;

/// <summary>
/// Top-level request body for POST /api/execute.
/// </summary>
public class ExecuteRequest
{
    /// <summary>Oracle schema owner. Optional – if omitted, searches the connected user's schema first.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Procedure or function name (without schema prefix).</summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>IN / INOUT / OUT parameters to pass. OUT-only params need only Name + Direction.</summary>
    public List<ProcParameter> Parameters { get; set; } = [];
}
