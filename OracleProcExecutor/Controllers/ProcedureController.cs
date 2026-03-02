using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using OracleProcExecutor.Models;
using OracleProcExecutor.Services;

namespace OracleProcExecutor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcedureController(IOracleExecutorService executorService, ILogger<ProcedureController> logger)
    : ControllerBase
{
    /// <summary>
    /// Execute any Oracle stored procedure or function.
    /// The service auto-discovers parameter types from ALL_ARGUMENTS.
    /// IN parameter values are provided in the request; OUT values are returned in resultSet.
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute([FromBody] ExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ObjectName))
            return BadRequest(new ErrorResponse("ObjectName is required."));

        try
        {
            var response = await executorService.ExecuteAsync(request);
            return Ok(response);
        }
        catch (OracleException ex)
        {
            logger.LogError(ex, "Oracle error executing {Object}", request.ObjectName);
            return BadRequest(new ErrorResponse($"Oracle error [{ex.Number}]: {ex.Message}"));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Configuration or discovery error for {Object}", request.ObjectName);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing {Object}", request.ObjectName);
            return StatusCode(500, new ErrorResponse($"Unexpected error: {ex.Message}"));
        }
    }
}

/// <summary>Standard error envelope.</summary>
public record ErrorResponse(string Error);
