using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using System.Text;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ITransferenciasServicio transferenciasServicio,
            ICuentaServicio cuentaServicio,
            IClienteServicio clienteServicio,
            ILogger<ReportsController> logger)
        {
            _transferenciasServicio = transferenciasServicio;
            _cuentaServicio = cuentaServicio;
            _clienteServicio = clienteServicio;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/reports/account-statement/{cuentaId}
        /// Genera extracto de cuenta
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
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(cuentaId);
                if (cuenta == null)
                    return NotFound(new { success = false, message = "Cuenta no encontrada." });

                // Verificar permisos
                var clienteId = GetClienteIdFromToken();
                var userRole = GetUserRole();

                if (userRole == "Cliente" && cuenta.ClienteId != clienteId)
                    return Forbid();

                var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var fin = endDate ?? DateTime.UtcNow;

                var transacciones = await _transferenciasServicio.ObtenerTransaccionesConFiltrosAsync(
                    cuenta.ClienteId, inicio, fin, null, null);

                // Filtrar solo transacciones de esta cuenta
                var transaccionesCuenta = transacciones
                    .Where(t => t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId)
                    .OrderBy(t => t.FechaCreacion)
                    .ToList();

                if (format.ToLower() == "pdf" || format.ToLower() == "txt")
                {
                    var extracto = GenerarExtractoTexto(cuenta, transaccionesCuenta, inicio, fin);
                    var bytes = Encoding.UTF8.GetBytes(extracto);
                    return File(bytes, "text/plain", $"extracto_{cuenta.Numero}_{DateTime.Now:yyyyMMdd}.txt");
                }

                // Formato JSON
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cuenta = new
                        {
                            numero = cuenta.Numero,
                            tipo = cuenta.Tipo,
                            moneda = cuenta.Moneda,
                            titular = cuenta.Cliente?.NombreCompleto
                        },
                        periodo = new
                        {
                            desde = inicio,
                            hasta = fin
                        },
                        saldo = new
                        {
                            inicial = CalcularSaldoInicial(transaccionesCuenta, cuenta.Saldo),
                            final = cuenta.Saldo
                        },
                        transacciones = transaccionesCuenta.Select(t => new
                        {
                            fecha = t.FechaCreacion,
                            tipo = t.Tipo,
                            descripcion = t.Descripcion,
                            monto = t.CuentaOrigenId == cuentaId ? -t.Monto : t.Monto,
                            comision = t.CuentaOrigenId == cuentaId ? -t.Comision : 0,
                            referencia = t.ComprobanteReferencia,
                            estado = t.Estado
                        }),
                        resumen = new
                        {
                            totalTransacciones = transaccionesCuenta.Count,
                            totalDebitos = transaccionesCuenta.Where(t => t.CuentaOrigenId == cuentaId).Sum(t => t.Monto + t.Comision),
                            totalCreditos = transaccionesCuenta.Where(t => t.CuentaDestinoId == cuentaId).Sum(t => t.Monto)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando extracto de cuenta {cuentaId}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/reports/client-summary/{clienteId}
        /// Genera resumen de cliente
        /// </summary>
        [HttpGet("client-summary/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetClientSummary(int clienteId)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);
                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);

                var hoy = DateTime.UtcNow;
                var ultimoMes = hoy.AddMonths(-1);
                var ultimoAno = hoy.AddYears(-1);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cliente = new
                        {
                            id = cliente.Id,
                            nombre = cliente.NombreCompleto,
                            identificacion = cliente.Identificacion,
                            correo = cliente.Correo,
                            telefono = cliente.Telefono,
                            fechaRegistro = cliente.FechaRegistro
                        },
                        cuentas = new
                        {
                            total = cuentas.Count,
                            activas = cuentas.Count(c => c.Estado == "Activa"),
                            saldoTotalCRC = cuentas.Where(c => c.Moneda == "CRC").Sum(c => c.Saldo),
                            saldoTotalUSD = cuentas.Where(c => c.Moneda == "USD").Sum(c => c.Saldo),
                            detalles = cuentas.Select(c => new
                            {
                                numero = c.Numero,
                                tipo = c.Tipo,
                                moneda = c.Moneda,
                                saldo = c.Saldo,
                                estado = c.Estado
                            })
                        },
                        actividad = new
                        {
                            totalTransacciones = transacciones.Count,
                            transaccionesUltimoMes = transacciones.Count(t => t.FechaCreacion >= ultimoMes),
                            transaccionesUltimoAno = transacciones.Count(t => t.FechaCreacion >= ultimoAno),
                            montoTransferidoMes = transacciones
                                .Where(t => t.FechaCreacion >= ultimoMes && t.Estado == "Exitosa")
                                .Sum(t => t.Monto),
                            ultimaTransaccion = transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault()?.FechaCreacion
                        },
                        estadisticas = new
                        {
                            transaccionesPorTipo = transacciones
                                .GroupBy(t => t.Tipo)
                                .Select(g => new { tipo = g.Key, cantidad = g.Count() }),
                            transaccionesPorEstado = transacciones
                                .GroupBy(t => t.Estado)
                                .Select(g => new { estado = g.Key, cantidad = g.Count() })
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando resumen de cliente {clienteId}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/reports/transactions-report
        /// Reporte de transacciones con filtros
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
                    // Obtener todas las transacciones (solo para admin/gestor)
                    var gestorId = GetCurrentUserId();
                    var userRole = GetUserRole();

                    if (userRole == "Gestor")
                    {
                        transacciones = await _transferenciasServicio.ObtenerOperacionesPorGestorAsync(
                            gestorId, inicio, fin);
                    }
                    else
                    {
                        // Admin: obtener todas
                        transacciones = new List<SistemaBancaEnLinea.BC.Modelos.Transaccion>();
                        // TODO: Implementar método para obtener todas las transacciones
                    }
                }

                // Aplicar filtros adicionales
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
                            clienteNombre = t.Cliente?.NombreCompleto,
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
                return StatusCode(500, new { success = false, message = ex.Message });
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
                    },
                    cuentas = new
                    {
                        totalActivas = 0, // TODO: Implementar
                        saldoTotalSistema = 0m // TODO: Implementar
                    },
                    transacciones = new
                    {
                        totalHoy = 0, // TODO: Implementar
                        totalMes = 0, // TODO: Implementar
                        montoTotalMes = 0m // TODO: Implementar
                    }
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo estadísticas de dashboard: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Métodos privados auxiliares
        private string GenerarExtractoTexto(
            SistemaBancaEnLinea.BC.Modelos.Cuenta cuenta,
            List<SistemaBancaEnLinea.BC.Modelos.Transaccion> transacciones,
            DateTime inicio,
            DateTime fin)
        {
            var sb = new StringBuilder();

            sb.AppendLine("===============================================");
            sb.AppendLine("           EXTRACTO DE CUENTA");
            sb.AppendLine("===============================================");
            sb.AppendLine();
            sb.AppendLine($"Cuenta: {cuenta.Numero}");
            sb.AppendLine($"Tipo: {cuenta.Tipo}");
            sb.AppendLine($"Moneda: {cuenta.Moneda}");
            sb.AppendLine($"Titular: {cuenta.Cliente?.NombreCompleto}");
            sb.AppendLine();
            sb.AppendLine($"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}");
            sb.AppendLine("===============================================");
            sb.AppendLine();
            sb.AppendLine($"Saldo Actual: {cuenta.Moneda} {cuenta.Saldo:N2}");
            sb.AppendLine();
            sb.AppendLine("MOVIMIENTOS:");
            sb.AppendLine("-----------------------------------------------");

            foreach (var t in transacciones)
            {
                var esCargo = t.CuentaOrigenId == cuenta.Id;
                var signo = esCargo ? "-" : "+";
                var monto = esCargo ? t.Monto + t.Comision : t.Monto;

                sb.AppendLine($"{t.FechaCreacion:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"  {t.Tipo}: {t.Descripcion}");
                sb.AppendLine($"  Monto: {signo}{cuenta.Moneda} {monto:N2}");
                sb.AppendLine($"  Ref: {t.ComprobanteReferencia}");
                sb.AppendLine();
            }

            sb.AppendLine("===============================================");
            sb.AppendLine($"Total Transacciones: {transacciones.Count}");

            return sb.ToString();
        }

        private decimal CalcularSaldoInicial(
            List<SistemaBancaEnLinea.BC.Modelos.Transaccion> transacciones,
            decimal saldoActual)
        {
            var saldoInicial = saldoActual;

            foreach (var t in transacciones)
            {
                // Revertir los movimientos
                if (t.Estado == "Exitosa")
                {
                    // Si fue cargo, sumar (revertir)
                    // Si fue abono, restar (revertir)
                    // Esta es una simplificación
                }
            }

            return saldoInicial;
        }

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
    }
}