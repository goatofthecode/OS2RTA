using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using OracleProcExecutor.Models;

namespace OracleProcExecutor.Services
{
    public class OracleExecutorService
    {
        private readonly string _connStr;
        private readonly ParameterDiscoveryService _discovery;

        public OracleExecutorService()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDb"];
            if (cs == null)
                throw new InvalidOperationException(
                    "Connection string 'OracleDb' is not configured in web.config.");
            _connStr   = cs.ConnectionString;
            _discovery = new ParameterDiscoveryService();
        }

        public ExecuteResponse Execute(ExecuteRequest request)
        {
            // 1. Discover parameter metadata from Oracle data dictionary
            var metaList = _discovery.GetParameterMeta(request.SchemaName, request.ObjectName);

            // Build lookup: upper param name → caller-supplied value
            var callerValues = new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var p in request.Parameters)
                callerValues[p.Name.ToUpperInvariant()] = p.Value;

            // 2. Build fully-qualified object name
            var objectName = string.IsNullOrWhiteSpace(request.SchemaName)
                ? request.ObjectName
                : request.SchemaName + "." + request.ObjectName;

            var boundParams = new List<KeyValuePair<OracleParamMeta, OracleParameter>>();

            using (var conn = new OracleConnection(_connStr))
            {
                conn.Open();

                using (var cmd = new OracleCommand(objectName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.BindByName  = true;

                    // 3. Add parameters
                    foreach (var meta in metaList)
                    {
                        var direction = meta.GetDirection();
                        var dbType    = meta.GetOracleDbType();
                        var paramName = meta.IsReturnValue ? "RETURN_VALUE" : meta.ArgumentName;

                        var oraParam = new OracleParameter(paramName, dbType)
                        {
                            Direction = direction
                        };

                        // Set value for IN / INOUT
                        if (direction == ParameterDirection.Input ||
                            direction == ParameterDirection.InputOutput)
                        {
                            object val;
                            if (!string.IsNullOrEmpty(meta.ArgumentName) &&
                                callerValues.TryGetValue(
                                    meta.ArgumentName.ToUpperInvariant(), out val))
                                oraParam.Value = val ?? DBNull.Value;
                            else
                                oraParam.Value = DBNull.Value;
                        }

                        // String OUT parameters need a size hint
                        if (direction != ParameterDirection.Input &&
                            (dbType == OracleDbType.Varchar2 ||
                             dbType == OracleDbType.NVarchar2 ||
                             dbType == OracleDbType.Char      ||
                             dbType == OracleDbType.NChar))
                        {
                            oraParam.Size = 4000;
                        }

                        cmd.Parameters.Add(oraParam);
                        boundParams.Add(
                            new KeyValuePair<OracleParamMeta, OracleParameter>(meta, oraParam));
                    }

                    // 4. Execute
                    cmd.ExecuteNonQuery();

                    // 5. Collect OUT / ReturnValue results
                    var resultSet = new Dictionary<string, object>();

                    foreach (var pair in boundParams)
                    {
                        var meta     = pair.Key;
                        var oraParam = pair.Value;
                        var dir      = meta.GetDirection();

                        if (dir == ParameterDirection.Input) continue;

                        var key = meta.IsReturnValue ? "RETURN_VALUE" : meta.ArgumentName;

                        if (meta.IsRefCursor)
                        {
                            var rows = new List<Dictionary<string, object>>();
                            var cursor = oraParam.Value as OracleRefCursor;
                            if (cursor != null)
                            {
                                using (var cursorReader = cursor.GetDataReader())
                                {
                                    var fieldCount = cursorReader.FieldCount;
                                    var fieldNames = new string[fieldCount];
                                    for (int i = 0; i < fieldCount; i++)
                                        fieldNames[i] = cursorReader.GetName(i);

                                    while (cursorReader.Read())
                                    {
                                        var row = new Dictionary<string, object>(
                                            StringComparer.OrdinalIgnoreCase);
                                        for (int i = 0; i < fieldCount; i++)
                                            row[fieldNames[i]] = cursorReader.IsDBNull(i)
                                                ? null
                                                : ConvertOracleValue(cursorReader.GetValue(i));
                                        rows.Add(row);
                                    }
                                }
                            }
                            resultSet[key] = rows;
                        }
                        else
                        {
                            resultSet[key] = (oraParam.Value == null ||
                                              oraParam.Value is DBNull)
                                ? null
                                : ConvertOracleValue(oraParam.Value);
                        }
                    }

                    return new ExecuteResponse { ResultSet = resultSet };
                }
            }
        }

        /// <summary>Converts Oracle-specific value types to CLR primitives for JSON.</summary>
        private static object ConvertOracleValue(object value)
        {
            if (value == null || value is DBNull) return null;

            if (value is OracleDecimal)
            {
                var d = (OracleDecimal)value;
                if (d.IsNull) return null;
                return d.IsInt ? (object)d.ToInt64() : (double)d;
            }
            if (value is OracleString)
            {
                var s = (OracleString)value;
                return s.IsNull ? null : s.Value;
            }
            if (value is OracleDate)
            {
                var dt = (OracleDate)value;
                return dt.IsNull ? null : (object)dt.Value;
            }
            if (value is OracleTimeStamp)
            {
                var ts = (OracleTimeStamp)value;
                return ts.IsNull ? null : (object)ts.Value;
            }
            // Already a CLR primitive
            return value;
        }
    }
}
