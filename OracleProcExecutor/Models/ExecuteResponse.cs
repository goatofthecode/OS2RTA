namespace OracleProcExecutor.Models;

/// <summary>
/// Response envelope returned by POST /api/execute.
/// </summary>
public class ExecuteResponse
{
    /// <summary>
    /// OUT / INOUT / RETURN values keyed by parameter name.
    /// REF CURSOR values are serialised as List&lt;Dictionary&lt;string, object?&gt;&gt;.
    /// Functions expose their return value under the key "RETURN_VALUE".
    /// </summary>
    public Dictionary<string, object?> ResultSet { get; set; } = [];
}
