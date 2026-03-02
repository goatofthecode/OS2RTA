using System;
using System.Collections.Generic;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace OracleProcExecutor.Services
{
    public class ParameterDiscoveryService
    {
        private readonly string _connStr;

        public ParameterDiscoveryService()
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDb"];
            if (cs == null)
                throw new InvalidOperationException(
                    "Connection string 'OracleDb' is not configured in web.config.");
            _connStr = cs.ConnectionString;
        }

        /// <summary>
        /// Queries ALL_ARGUMENTS to return full parameter metadata for the given object.
        /// </summary>
        public IList<OracleParamMeta> GetParameterMeta(string schema, string objectName)
        {
            const string Sql = @"
                SELECT ARGUMENT_NAME,
                       IN_OUT,
                       DATA_TYPE,
                       POSITION,
                       SEQUENCE
                FROM   ALL_ARGUMENTS
                WHERE  UPPER(OBJECT_NAME) = UPPER(:objName)
                  AND  (:schema IS NULL OR UPPER(OWNER) = UPPER(:schema))
                  AND  DATA_LEVEL = 0
                ORDER  BY SEQUENCE";

            var result = new List<OracleParamMeta>();

            using (var conn = new OracleConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new OracleCommand(Sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("objName", objectName));
                    cmd.Parameters.Add(new OracleParameter("schema",
                        (object)schema ?? DBNull.Value));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new OracleParamMeta
                            {
                                ArgumentName = reader.IsDBNull(0) ? null : reader.GetString(0),
                                InOut        = reader.IsDBNull(1) ? "IN"       : reader.GetString(1),
                                DataType     = reader.IsDBNull(2) ? "VARCHAR2" : reader.GetString(2),
                                Position     = reader.GetInt32(3),
                                Sequence     = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            if (result.Count == 0)
                throw new InvalidOperationException(
                    string.Format(
                        "No parameter metadata found for '{0}{1}'. " +
                        "Check the object exists and the user has SELECT on ALL_ARGUMENTS.",
                        schema != null ? schema + "." : "", objectName));

            return result;
        }
    }
}
