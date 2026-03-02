using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OracleProcExecutor.Services
{
    /// <summary>Internal metadata for one Oracle parameter fetched from ALL_ARGUMENTS.</summary>
    public class OracleParamMeta
    {
        public string ArgumentName { get; set; }   // null/empty = function return value
        public string InOut        { get; set; }   // "IN", "OUT", "IN/OUT"
        public string DataType     { get; set; }   // "NUMBER", "VARCHAR2", "REF CURSOR" …
        public int    Position     { get; set; }   // 0 = function return value
        public int    Sequence     { get; set; }

        public bool IsReturnValue => Position == 0 && string.IsNullOrEmpty(ArgumentName);
        public bool IsRefCursor   => string.Equals(DataType, "REF CURSOR",
                                         System.StringComparison.OrdinalIgnoreCase);

        public ParameterDirection GetDirection()
        {
            if (IsReturnValue) return ParameterDirection.ReturnValue;
            switch ((InOut ?? "IN").ToUpperInvariant())
            {
                case "OUT":    return ParameterDirection.Output;
                case "IN/OUT": return ParameterDirection.InputOutput;
                default:       return ParameterDirection.Input;
            }
        }

        public OracleDbType GetOracleDbType()
        {
            switch ((DataType ?? "VARCHAR2").ToUpperInvariant())
            {
                case "VARCHAR2":        return OracleDbType.Varchar2;
                case "NVARCHAR2":       return OracleDbType.NVarchar2;
                case "CHAR":            return OracleDbType.Char;
                case "NCHAR":           return OracleDbType.NChar;
                case "CLOB":            return OracleDbType.Clob;
                case "NCLOB":           return OracleDbType.NClob;
                case "NUMBER":          return OracleDbType.Decimal;
                case "FLOAT":           return OracleDbType.Double;
                case "BINARY_FLOAT":    return OracleDbType.BinaryFloat;
                case "BINARY_DOUBLE":   return OracleDbType.BinaryDouble;
                case "INTEGER":
                case "BINARY_INTEGER":
                case "PLS_INTEGER":     return OracleDbType.Int32;
                case "DATE":            return OracleDbType.Date;
                case "TIMESTAMP":       return OracleDbType.TimeStamp;
                case "TIMESTAMP WITH TIME ZONE":       return OracleDbType.TimeStampTZ;
                case "TIMESTAMP WITH LOCAL TIME ZONE": return OracleDbType.TimeStampLTZ;
                case "RAW":             return OracleDbType.Raw;
                case "BLOB":            return OracleDbType.Blob;
                case "XMLTYPE":         return OracleDbType.XmlType;
                case "REF CURSOR":      return OracleDbType.RefCursor;
                default:                return OracleDbType.Varchar2;
            }
        }
    }
}
