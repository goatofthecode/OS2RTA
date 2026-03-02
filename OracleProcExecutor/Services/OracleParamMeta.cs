using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OracleProcExecutor.Services;

/// <summary>
/// Internal metadata for one Oracle parameter fetched from ALL_ARGUMENTS.
/// </summary>
public class OracleParamMeta
{
    /// <summary>Parameter name. Null/empty for a function RETURN value.</summary>
    public string? ArgumentName { get; set; }

    /// <summary>Oracle direction string: "IN", "OUT", "IN/OUT".</summary>
    public string InOut { get; set; } = "IN";

    /// <summary>Oracle DATA_TYPE string, e.g. "NUMBER", "VARCHAR2", "REF CURSOR".</summary>
    public string DataType { get; set; } = "VARCHAR2";

    /// <summary>Position in the signature. 0 = function return value.</summary>
    public int Position { get; set; }

    /// <summary>Sequence order from ALL_ARGUMENTS.</summary>
    public int Sequence { get; set; }

    // ─── Derived helpers ────────────────────────────────────────────────────

    public bool IsReturnValue => Position == 0 && string.IsNullOrEmpty(ArgumentName);

    public bool IsRefCursor => DataType.Equals("REF CURSOR", StringComparison.OrdinalIgnoreCase);

    public ParameterDirection GetDirection()
    {
        if (IsReturnValue) return ParameterDirection.ReturnValue;
        return InOut.ToUpperInvariant() switch
        {
            "IN"     => ParameterDirection.Input,
            "OUT"    => ParameterDirection.Output,
            "IN/OUT" => ParameterDirection.InputOutput,
            _        => ParameterDirection.Input
        };
    }

    /// <summary>
    /// Map Oracle DATA_TYPE string to OracleDbType.
    /// </summary>
    public OracleDbType GetOracleDbType()
    {
        return DataType.ToUpperInvariant() switch
        {
            "VARCHAR2"        => OracleDbType.Varchar2,
            "NVARCHAR2"       => OracleDbType.NVarchar2,
            "CHAR"            => OracleDbType.Char,
            "NCHAR"           => OracleDbType.NChar,
            "CLOB"            => OracleDbType.Clob,
            "NCLOB"           => OracleDbType.NClob,
            "NUMBER"          => OracleDbType.Decimal,
            "FLOAT"           => OracleDbType.Double,
            "BINARY_FLOAT"    => OracleDbType.BinaryFloat,
            "BINARY_DOUBLE"   => OracleDbType.BinaryDouble,
            "INTEGER"         => OracleDbType.Int32,
            "BINARY_INTEGER"  => OracleDbType.Int32,
            "PLS_INTEGER"     => OracleDbType.Int32,
            "DATE"            => OracleDbType.Date,
            "TIMESTAMP"       => OracleDbType.TimeStamp,
            "TIMESTAMP WITH TIME ZONE"          => OracleDbType.TimeStampTZ,
            "TIMESTAMP WITH LOCAL TIME ZONE"    => OracleDbType.TimeStampLTZ,
            "INTERVAL YEAR TO MONTH"            => OracleDbType.IntervalYM,
            "INTERVAL DAY TO SECOND"            => OracleDbType.IntervalDS,
            "RAW"             => OracleDbType.Raw,
            "LONG RAW"        => OracleDbType.LongRaw,
            "BLOB"            => OracleDbType.Blob,
            "BFILE"           => OracleDbType.BFile,
            "XMLTYPE"         => OracleDbType.XmlType,
            "REF CURSOR"      => OracleDbType.RefCursor,
            _                 => OracleDbType.Varchar2   // safe fallback
        };
    }
}
