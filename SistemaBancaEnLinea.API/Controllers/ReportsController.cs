using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportesServicio _reportesServicio;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportesServicio reportesServicio,
            ILogger<ReportsController> logger)
        {
            _reportesServicio = reportesServicio;
            _logger = logger;
        }

        [HttpGet("account-statement/{cuentaId}")]
        public async Task<IActionResult> GetAccountStatement(
            int cuentaId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string format = "json")
        {
            try
            {
                var usuarioId = GetCurrentUserId();
                var rol = GetUserRole();

                var (archivo, numeroCuenta) = await _reportesServicio.GenerarExtractoConAccesoAsync(
                    cuentaId, startDate, endDate, format, usuarioId, rol);

                if (archivo != null)
                {
                    var extension = format.ToLower() == "pdf" ? "pdf" : "csv";
                    var contentType = format.ToLower() == "pdf" ? "application/pdf" : "text/csv";
                    return File(archivo, contentType, $"extracto_{numeroCuenta}_{DateTime.Now:yyyyMMdd}.{extension}");
                }

                var extracto = await _reportesServicio.GenerarExtractoCuentaAsync(
                    cuentaId, startDate ?? DateTime.UtcNow.AddMonths(-1), endDate ?? DateTime.UtcNow);
                return Ok(ApiResponse<ExtractoCuentaDto>.Ok(extracto));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando extracto de cuenta {CuentaId}", cuentaId);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("my-summary")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> GetMySummary([FromQuery] string format = "json")
        {
            try
            {
                var usuarioId = GetCurrentUserId();

                if (format.ToLower() == "pdf")
                {
                    var archivo = await _reportesServicio.GenerarResumenParaUsuarioAsync(usuarioId, format);
                    if (archivo != null)
                        return File(archivo, "application/pdf", $"resumen_{DateTime.Now:yyyyMMdd}.pdf");
                }

                // Para JSON, obtener el resumen del cliente
                var resumen = await _reportesServicio.GenerarResumenParaUsuarioJsonAsync(usuarioId);
                return Ok(ApiResponse<ResumenClienteDto>.Ok(resumen));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando resumen");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("client-summary/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetClientSummary(int clienteId, [FromQuery] string format = "json")
        {
            try
            {
                if (format.ToLower() == "pdf")
                {
                    var pdfBytes = await _reportesServicio.GenerarResumenClientePdfAsync(clienteId);
                    return File(pdfBytes, "application/pdf", $"resumen_cliente_{clienteId}_{DateTime.Now:yyyyMMdd}.pdf");
                }

                var resumen = await _reportesServicio.GenerarResumenClienteAsync(clienteId);
                return Ok(ApiResponse<ResumenClienteDto>.Ok(resumen));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando resumen de cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("transactions-report")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetTransactionsReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? tipo,
            [FromQuery] string? estado,
            [FromQuery] int? clienteId)
        {
            try
            {
                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;
                var usuarioId = GetCurrentUserId();
                var rol = GetUserRole();

                var reporte = await _reportesServicio.GenerarReporteTransaccionesAsync(
                    inicio, fin, tipo, estado, clienteId, usuarioId, rol);

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de transacciones");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _reportesServicio.GenerarEstadisticasDashboardAsync();
                return Ok(ApiResponse<object>.Ok(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de dashboard");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// RF-G1: Reporte de volumen de transacciones diarias
        /// </summary>
        [HttpGet("daily-volume")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetDailyTransactionVolume(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;
                var usuarioId = GetCurrentUserId();
                var rol = GetUserRole();

                var reporte = await _reportesServicio.GenerarVolumenDiarioAsync(inicio, fin, usuarioId, rol);
                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de volumen diario");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// RF-G1: Reporte de clientes más activos
        /// </summary>
        [HttpGet("most-active-clients")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetMostActiveClients(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int top = 10)
        {
            try
            {
                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;
                var usuarioId = GetCurrentUserId();
                var rol = GetUserRole();

                var reporte = await _reportesServicio.GenerarClientesMasActivosAsync(inicio, fin, top, usuarioId, rol);
                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de clientes más activos");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// RF-G1: Reporte de totales por período
        /// </summary>
        [HttpGet("period-totals")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetPeriodTotals(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;
                var usuarioId = GetCurrentUserId();
                var rol = GetUserRole();

                var totales = await _reportesServicio.GenerarTotalesPorPeriodoAsync(inicio, fin, usuarioId, rol);
                return Ok(ApiResponse<object>.Ok(totales));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de totales por período");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        #region Helpers

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst("role")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? "Cliente";
        }

        #endregion
    }
}
