using System.Collections.Generic;

namespace OracleProcExecutor.Models
{
    /// <summary>
    /// Request body for POST /api/procedure/execute.
    /// </summary>
    public class ExecuteRequest
    {
        /// <summary>Oracle schema owner (optional — omit to use connected user's schema).</summary>
        public string SchemaName { get; set; }

        /// <summary>Procedure or function name.</summary>
        public string ObjectName { get; set; }

        /// <summary>IN / OUT / INOUT parameter list. OUT-only params need only Name + Direction.</summary>
        public List<ProcParameter> Parameters { get; set; } = new List<ProcParameter>();
    }
}
