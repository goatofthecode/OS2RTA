using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using OracleProcExecutor.Models;

namespace OracleProcExecutor.Services;

public interface IOracleExecutorService
{
    Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request);
}

public class OracleExecutorService(
    IConfiguration config,
    IParameterDiscoveryService discoveryService) : IOracleExecutorService
{
    private readonly string _connStr =
        config.GetConnectionString("OracleDb")
        ?? throw new InvalidOperationException("Connection string 'OracleDb' is not configured.");

    public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request)
    {
        // 1. Discover parameter metadata from ALL_ARGUMENTS
        var metaList = await discoveryService.GetParameterMetaAsync(
            request.SchemaName, request.ObjectName);

        // Build a lookup from param name (upper) → caller-supplied value
        var callerValues = request.Parameters
            .ToDictionary(
                p => p.Name.ToUpperInvariant(),
                p => p.Value);

        // 2. Open connection and build command
        await using var conn = new OracleConnection(_connStr);
        await conn.OpenAsync();

        var objectFullName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? request.ObjectName
            : $"{request.SchemaName}.{request.ObjectName}";

        await using var cmd = new OracleCommand(objectFullName, conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            // Bind parameters by name so order in the payload doesn't matter
            BindByName = true
        };

        // 3. Add OracleParameters per discovered metadata
        var oracleParams = new List<(OracleParamMeta Meta, OracleParameter Param)>();

        foreach (var meta in metaList)
        {
            var direction = meta.GetDirection();
            var dbType    = meta.GetOracleDbType();

            // The command parameter name: use ArgumentName for normal params,
            // or a synthetic name for the function return value
            var paramName = string.IsNullOrEmpty(meta.ArgumentName)
                ? "RETURN_VALUE"
                : meta.ArgumentName;

            var oraParam = new OracleParameter(paramName, dbType)
            {
                Direction = direction
            };

            // Set value for IN / INOUT from caller payload
            if (direction is System.Data.ParameterDirection.Input
                          or System.Data.ParameterDirection.InputOutput)
            {
                if (callerValues.TryGetValue(meta.ArgumentName!.ToUpperInvariant(), out var callerVal))
                    oraParam.Value = callerVal ?? DBNull.Value;
                else
                    oraParam.Value = DBNull.Value;
            }

            // For RefCursors the size must not be set
            if (dbType != OracleDbType.RefCursor && dbType != OracleDbType.Clob
                && dbType != OracleDbType.NClob && dbType != OracleDbType.Blob)
            {
                // Varchar2 needs a reasonable size when direction is OUTPUT
                if (dbType == OracleDbType.Varchar2 || dbType == OracleDbType.NVarchar2
                    || dbType == OracleDbType.Char   || dbType == OracleDbType.NChar)
                {
                    oraParam.Size = 4000;
                }
            }

            cmd.Parameters.Add(oraParam);
            oracleParams.Add((meta, oraParam));
        }

        // 4. Execute
        await cmd.ExecuteNonQueryAsync();

        // 5. Collect result set
        var resultSet = new Dictionary<string, object?>();

        foreach (var (meta, oraParam) in oracleParams)
        {
            var dir = meta.GetDirection();
            if (dir is System.Data.ParameterDirection.Input) continue; // skip pure IN params

            var resultKey = meta.IsReturnValue
                ? "RETURN_VALUE"
                : meta.ArgumentName!;

            if (meta.IsRefCursor)
            {
                // Read ref cursor into list of dictionaries
                var rows = new List<Dictionary<string, object?>>();

                if (oraParam.Value is OracleRefCursor cursor)
                {
                    await using var cursorReader = cursor.GetDataReader();
                    var fieldCount = cursorReader.FieldCount;
                    var fieldNames = Enumerable.Range(0, fieldCount)
                        .Select(i => cursorReader.GetName(i))
                        .ToArray();

                    while (await cursorReader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < fieldCount; i++)
                        {
                            row[fieldNames[i]] = cursorReader.IsDBNull(i)
                                ? null
                                : ConvertOracleValue(cursorReader.GetValue(i));
                        }
                        rows.Add(row);
                    }
                }

                resultSet[resultKey] = rows;
            }
            else
            {
                // Scalar OUT
                resultSet[resultKey] = oraParam.Value is DBNull or null
                    ? null
                    : ConvertOracleValue(oraParam.Value);
            }
        }

        return new ExecuteResponse { ResultSet = resultSet };
    }

    /// <summary>
    /// Converts Oracle-specific value types (OracleDecimal, OracleString, etc.)
    /// to plain CLR types for safe JSON serialization.
    /// </summary>
    private static object? ConvertOracleValue(object? value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            // Oracle numeric types
            OracleDecimal d when d.IsNull  => null,
            OracleDecimal d                => d.IsInt ? (object)d.ToInt64() : (double)d,
            // Oracle string types
            OracleString s when s.IsNull   => null,
            OracleString s                 => s.Value,
            // Oracle date
            OracleDate dt when dt.IsNull   => null,
            OracleDate dt                  => dt.Value,
            // Oracle timestamp
            OracleTimeStamp ts when ts.IsNull => null,
            OracleTimeStamp ts               => ts.Value,
            // Standard CLR types (already fine)
            string or int or long or double or decimal or bool or DateTime => value,
            // Fallback: ToString
            _ => value.ToString()
        };
    }
}
