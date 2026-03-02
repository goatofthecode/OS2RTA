using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using OracleProcExecutor.Models;
using OracleProcExecutor.Services;

namespace OracleProcExecutor.Controllers
{
    [RoutePrefix("api/procedure")]
    public class ProcedureController : ApiController
    {
        private readonly OracleExecutorService _executor;

        public ProcedureController()
        {
            _executor = new OracleExecutorService();
        }

        /// <summary>
        /// Execute any Oracle stored procedure or function.
        /// Parameter types are auto-discovered from ALL_ARGUMENTS.
        /// </summary>
        [HttpPost]
        [Route("execute")]
        public HttpResponseMessage Execute([FromBody] ExecuteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ObjectName))
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { Error = "ObjectName is required." });

            try
            {
                var response = _executor.Execute(request);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (OracleException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { Error = string.Format("Oracle error [{0}]: {1}", ex.Number, ex.Message) });
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new { Error = "Unexpected error: " + ex.Message });
            }
        }
    }
}
