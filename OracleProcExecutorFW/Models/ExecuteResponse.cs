using System.Collections.Generic;

namespace OracleProcExecutor.Models
{
    /// <summary>
    /// Response envelope — OUT values keyed by parameter name.
    /// REF_CURSOR params become List&lt;Dictionary&lt;string,object&gt;&gt;.
    /// Function return values appear as "RETURN_VALUE".
    /// </summary>
    public class ExecuteResponse
    {
        public Dictionary<string, object> ResultSet { get; set; }
            = new Dictionary<string, object>();
    }
}
