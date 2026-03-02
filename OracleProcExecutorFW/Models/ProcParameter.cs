namespace OracleProcExecutor.Models
{
    /// <summary>
    /// One parameter slot in the Execute request.
    /// </summary>
    public class ProcParameter
    {
        /// <summary>Parameter name as declared in Oracle (e.g. "P_MSISDN").</summary>
        public string Name { get; set; }

        /// <summary>"IN", "OUT", or "INOUT".</summary>
        public string Direction { get; set; }

        /// <summary>Value for IN / INOUT parameters. Omit for pure OUT.</summary>
        public object Value { get; set; }
    }
}
