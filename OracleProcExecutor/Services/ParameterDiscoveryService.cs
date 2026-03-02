using Oracle.ManagedDataAccess.Client;

namespace OracleProcExecutor.Services;

public interface IParameterDiscoveryService
{
    /// <summary>
    /// Queries ALL_ARGUMENTS to retrieve full parameter metadata for the given object.
    /// </summary>
    Task<IReadOnlyList<OracleParamMeta>> GetParameterMetaAsync(
        string? schema, string objectName);
}

public class ParameterDiscoveryService(IConfiguration config) : IParameterDiscoveryService
{
    private readonly string _connStr =
        config.GetConnectionString("OracleDb")
        ?? throw new InvalidOperationException("Connection string 'OracleDb' is not configured.");

    public async Task<IReadOnlyList<OracleParamMeta>> GetParameterMetaAsync(
        string? schema, string objectName)
    {
        // We query ALL_ARGUMENTS.
        // When schema is supplied we filter by OWNER; otherwise we use USER_ARGUMENTS
        // (which only shows the current user's objects) as a fallback via a UNION.
        const string sql = """
            SELECT ARGUMENT_NAME,
                   IN_OUT,
                   DATA_TYPE,
                   POSITION,
                   SEQUENCE
            FROM   ALL_ARGUMENTS
            WHERE  UPPER(OBJECT_NAME) = UPPER(:objName)
              AND  (:schema IS NULL OR UPPER(OWNER) = UPPER(:schema))
              AND  DATA_LEVEL   = 0
            ORDER  BY SEQUENCE
            """;

        var result = new List<OracleParamMeta>();

        await using var conn = new OracleConnection(_connStr);
        await conn.OpenAsync();

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("objName", objectName));
        cmd.Parameters.Add(new OracleParameter("schema",  (object?)schema ?? DBNull.Value));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new OracleParamMeta
            {
                ArgumentName = reader.IsDBNull(0) ? null : reader.GetString(0),
                InOut        = reader.IsDBNull(1) ? "IN"  : reader.GetString(1),
                DataType     = reader.IsDBNull(2) ? "VARCHAR2" : reader.GetString(2),
                Position     = reader.GetInt32(3),
                Sequence     = reader.GetInt32(4)
            });
        }

        if (result.Count == 0)
            throw new InvalidOperationException(
                $"No parameter metadata found for '{(schema is null ? "" : schema + ".")}{objectName}'. " +
                "Check that the object exists and the connected user has SELECT on ALL_ARGUMENTS.");

        return result;
    }
}
