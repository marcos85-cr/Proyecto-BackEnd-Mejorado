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
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportesServicio reportesServicio,
            ITransferenciasServicio transferenciasServicio,
            ICuentaServicio cuentaServicio,
            IClienteServicio clienteServicio,
            ILogger<ReportsController> logger)
        {
            _reportesServicio = reportesServicio;
            _transferenciasServicio = transferenciasServicio;
            _cuentaServicio = cuentaServicio;
            _clienteServicio = clienteServicio;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/reports/account-statement/{cuentaId}
        /// Genera extracto de cuenta en JSON, PDF o CSV
        /// </summary>
        [HttpGet("account-statement/{cuentaId}")]
        public async Task<IActionResult> GetAccountStatement(
            int cuentaId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string format = "json")
        {
            try
            {
                // Validar propiedad de la cuenta
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(cuentaId);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada."));

                var clienteId = GetClienteIdFromToken();
                var userRole = GetUserRole();

                // RF-F1: Cliente solo puede ver sus propias cuentas
                if (userRole == "Cliente" && cuenta.ClienteId != clienteId)
                    return Forbid();

                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;

                switch (format.ToLower())
                {
                    case "pdf":
                        var pdfBytes = await _reportesServicio.GenerarExtractoPdfAsync(cuentaId, inicio, fin);
                        return File(pdfBytes, "application/pdf", $"extracto_{cuenta.Numero}_{DateTime.Now:yyyyMMdd}.pdf");

                    case "csv":
                        var csvBytes = await _reportesServicio.GenerarExtractoCsvAsync(cuentaId, inicio, fin);
                        return File(csvBytes, "text/csv", $"extracto_{cuenta.Numero}_{DateTime.Now:yyyyMMdd}.csv");

                    default: // json
                        var extracto = await _reportesServicio.GenerarExtractoCuentaAsync(cuentaId, inicio, fin);
                        return Ok(ApiResponse<ExtractoCuentaDto>.Ok(extracto));
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando extracto de cuenta {cuentaId}: {ex.Message}");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// GET: api/reports/my-summary
        /// Resumen del cliente autenticado
        /// </summary>
        [HttpGet("my-summary")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> GetMySummary([FromQuery] string format = "json")
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                if (format.ToLower() == "pdf")
                {
                    var pdfBytes = await _reportesServicio.GenerarResumenClientePdfAsync(clienteId);
                    return File(pdfBytes, "application/pdf", $"resumen_{DateTime.Now:yyyyMMdd}.pdf");
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
                _logger.LogError($"Error generando resumen: {ex.Message}");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// GET: api/reports/client-summary/{clienteId}
        /// Genera resumen de cliente (solo Admin/Gestor)
        /// </summary>
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
                _logger.LogError($"Error generando resumen de cliente {clienteId}: {ex.Message}");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// GET: api/reports/transactions-report
        /// Reporte de transacciones con filtros (Admin/Gestor)
        /// </summary>
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

                List<SistemaBancaEnLinea.BC.Modelos.Transaccion> transacciones;

                if (clienteId.HasValue)
                {
                    transacciones = await _transferenciasServicio.ObtenerTransaccionesConFiltrosAsync(
                        clienteId.Value, inicio, fin, tipo, estado);
                }
                else
                {
                    var gestorId = GetCurrentUserId();
                    var userRole = GetUserRole();

                    if (userRole == "Gestor")
                    {
                        transacciones = await _transferenciasServicio.ObtenerOperacionesPorGestorAsync(
                            gestorId, inicio, fin);
                    }
                    else
                    {
                        transacciones = new List<SistemaBancaEnLinea.BC.Modelos.Transaccion>();
                    }
                }

                // Filtros adicionales
                if (!string.IsNullOrEmpty(tipo))
                    transacciones = transacciones.Where(t => t.Tipo == tipo).ToList();

                if (!string.IsNullOrEmpty(estado))
                    transacciones = transacciones.Where(t => t.Estado == estado).ToList();

                var resumen = new
                {
                    totalTransacciones = transacciones.Count,
                    montoTotal = transacciones.Where(t => t.Estado == "Exitosa").Sum(t => t.Monto),
                    comisionesTotal = transacciones.Where(t => t.Estado == "Exitosa").Sum(t => t.Comision),
                    porTipo = transacciones.GroupBy(t => t.Tipo).Select(g => new
                    {
                        tipo = g.Key,
                        cantidad = g.Count(),
                        monto = g.Sum(t => t.Monto)
                    }),
                    porEstado = transacciones.GroupBy(t => t.Estado).Select(g => new
                    {
                        estado = g.Key,
                        cantidad = g.Count()
                    })
                };

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        periodo = new { desde = inicio, hasta = fin },
                        resumen,
                        transacciones = transacciones.Select(t => new
                        {
                            id = t.Id,
                            fecha = t.FechaCreacion,
                            tipo = t.Tipo,
                            clienteNombre = t.Cliente?.UsuarioAsociado?.Nombre ?? "N/A",
                            monto = t.Monto,
                            moneda = t.Moneda,
                            comision = t.Comision,
                            estado = t.Estado,
                            referencia = t.ComprobanteReferencia
                        }).OrderByDescending(t => t.fecha)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando reporte de transacciones: {ex.Message}");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// GET: api/reports/dashboard-stats
        /// Estadísticas para dashboard de administrador
        /// </summary>
        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var clientes = await _clienteServicio.ObtenerTodosAsync();

                var hoy = DateTime.UtcNow.Date;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

                var stats = new
                {
                    clientes = new
                    {
                        total = clientes.Count,
                        activos = clientes.Count(c => c.Estado == "Activo"),
                        nuevosEsteMes = clientes.Count(c => c.FechaRegistro >= inicioMes)
                    }
                };

                return Ok(ApiResponse<object>.Ok(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo estadísticas de dashboard: {ex.Message}");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        #region Helpers

        private int GetClienteIdFromToken()
        {
            var clienteIdClaim = User.FindFirst("client_id")?.Value;
            return int.TryParse(clienteIdClaim, out var clienteId) ? clienteId : 0;
        }

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
