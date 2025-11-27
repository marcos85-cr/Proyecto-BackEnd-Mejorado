using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    /// <summary>
    /// Controlador para funcionalidades del rol Gestor
    /// Gestión de cartera de clientes y operaciones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Gestor")]
    public class GestorController : ControllerBase
    {
        private readonly IClienteServicio _clienteServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<GestorController> _logger;

        public GestorController(
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            ITransferenciasServicio transferenciasServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<GestorController> logger)
        {
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _transferenciasServicio = transferenciasServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        #region ========== HELPERS ==========

        private int GetGestorIdFromToken()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetGestorNombreFromToken()
        {
            return User.FindFirst("nombre")?.Value ?? "Gestor";
        }

        #endregion

        #region ========== DASHBOARD ==========

        /// <summary>
        /// GET: api/gestor/dashboard/stats
        /// Obtiene estadísticas del dashboard del gestor
        /// </summary>
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                // Obtener clientes asignados al gestor
                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);

                int totalClientes = misClientes.Count;
                int totalCuentasActivas = 0;
                decimal volumenTotal = 0;

                // Calcular estadísticas de cuentas
                foreach (var cliente in misClientes)
                {
                    var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(cliente.Id);
                    var cuentasActivas = cuentas.Where(c => c.Estado == "Activa").ToList();
                    totalCuentasActivas += cuentasActivas.Count;
                    volumenTotal += cuentasActivas.Sum(c => c.Saldo);
                }

                // Obtener operaciones
                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var operacionesHoy = await _transferenciasServicio.ObtenerOperacionesDeHoyPorClientesAsync(clienteIds);
                var pendientesAprobacion = await _transferenciasServicio.ObtenerOperacionesPendientesPorClientesAsync(clienteIds);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        myClients = totalClientes,
                        activeAccounts = totalCuentasActivas,
                        todayOperations = operacionesHoy.Count,
                        pendingApprovals = pendientesAprobacion.Count,
                        totalVolume = volumenTotal
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo stats del dashboard: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/gestor/operaciones-pendientes
        /// Obtiene las operaciones pendientes de aprobación
        /// </summary>
        [HttpGet("operaciones-pendientes")]
        public async Task<IActionResult> GetOperacionesPendientes()
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var operacionesPendientes = await _transferenciasServicio.ObtenerOperacionesPendientesPorClientesAsync(clienteIds);

                return Ok(new
                {
                    success = true,
                    data = operacionesPendientes.Select(op => new
                    {
                        id = op.Id.ToString(),
                        clienteId = op.ClienteId.ToString(),
                        clienteNombre = op.Cliente?.NombreCompleto ?? "N/A",
                        tipo = op.Tipo,
                        descripcion = op.Descripcion,
                        monto = op.Monto,
                        moneda = op.Moneda,
                        comision = op.Comision,
                        estado = op.Estado,
                        fecha = op.FechaCreacion,
                        cuentaOrigenNumero = op.CuentaOrigen?.Numero,
                        cuentaDestinoNumero = op.CuentaDestino?.Numero,
                        requiereAprobacion = true,
                        esUrgente = op.Monto > 200000
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo operaciones pendientes: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region ========== GESTIÓN DE CLIENTES ==========

        /// <summary>
        /// GET: api/gestor/mis-clientes
        /// Obtiene todos los clientes asignados al gestor
        /// </summary>
        [HttpGet("mis-clientes")]
        public async Task<IActionResult> GetMisClientes()
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);

                var clientesResponse = new List<object>();
                int totalCuentas = 0;
                decimal volumenTotal = 0;

                foreach (var cliente in misClientes)
                {
                    var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(cliente.Id);
                    var cuentasActivas = cuentas.Where(c => c.Estado == "Activa").ToList();
                    var volumenCliente = cuentasActivas.Sum(c => c.Saldo);

                    var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(cliente.Id);
                    var ultimaOperacion = transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault();

                    totalCuentas += cuentasActivas.Count;
                    volumenTotal += volumenCliente;

                    clientesResponse.Add(new
                    {
                        id = cliente.Id.ToString(),
                        nombre = cliente.NombreCompleto,
                        email = cliente.Correo,
                        identificacion = cliente.Identificacion,
                        telefono = cliente.Telefono,
                        cuentasActivas = cuentasActivas.Count,
                        ultimaOperacion = ultimaOperacion?.FechaCreacion ?? cliente.FechaRegistro,
                        estado = cliente.Estado,
                        volumenTotal = volumenCliente
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = clientesResponse,
                    stats = new
                    {
                        totalClients = misClientes.Count,
                        totalAccounts = totalCuentas,
                        totalVolume = volumenTotal
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo clientes del gestor: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/gestor/clientes/{clienteId}
        /// Obtiene el detalle de un cliente específico
        /// </summary>
        [HttpGet("clientes/{clienteId}")]
        public async Task<IActionResult> GetDetalleCliente(int clienteId)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                // Verificar que el cliente pertenece al gestor
                if (cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);
                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);
                var cuentasActivas = cuentas.Where(c => c.Estado == "Activa").ToList();
                var ultimaOperacion = transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = cliente.Id,
                        identificacion = cliente.Identificacion,
                        nombre = cliente.NombreCompleto,
                        nombreCompleto = cliente.NombreCompleto,
                        telefono = cliente.Telefono,
                        correo = cliente.Correo,
                        email = cliente.Correo,
                        estado = cliente.Estado,
                        fechaRegistro = cliente.FechaRegistro,
                        ultimaOperacion = ultimaOperacion?.FechaCreacion,
                        cuentasActivas = cuentasActivas.Count,
                        volumenTotal = cuentasActivas.Sum(c => c.Saldo),
                        totalTransacciones = transacciones.Count,
                        cuentas = cuentas.Select(c => new
                        {
                            id = c.Id,
                            numero = c.Numero,
                            tipo = c.Tipo,
                            moneda = c.Moneda,
                            saldo = c.Saldo,
                            estado = c.Estado,
                            fechaApertura = c.FechaApertura
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo detalle del cliente: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/gestor/clientes/{clienteId}/cuentas
        /// Obtiene las cuentas de un cliente
        /// </summary>
        [HttpGet("clientes/{clienteId}/cuentas")]
        public async Task<IActionResult> GetCuentasCliente(int clienteId)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                if (cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = cuentas.Select(c => new
                    {
                        id = c.Id,
                        numeroCuenta = c.Numero,
                        numero = c.Numero,
                        tipo = c.Tipo,
                        moneda = c.Moneda,
                        saldo = c.Saldo,
                        estado = c.Estado,
                        fechaApertura = c.FechaApertura ?? DateTime.UtcNow,
                        clienteId = c.ClienteId,
                        clienteNombre = cliente.NombreCompleto
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo cuentas del cliente: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/gestor/clientes/{clienteId}/transacciones
        /// Obtiene las transacciones de un cliente con filtros
        /// </summary>
        [HttpGet("clientes/{clienteId}/transacciones")]
        public async Task<IActionResult> GetTransaccionesCliente(
            int clienteId, 
            [FromQuery] DateTime? fechaInicio, 
            [FromQuery] DateTime? fechaFin,
            [FromQuery] string? tipo, 
            [FromQuery] string? estado)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                if (cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                var transacciones = await _transferenciasServicio.ObtenerTransaccionesConFiltrosAsync(
                    clienteId, fechaInicio, fechaFin, tipo, estado);

                return Ok(new
                {
                    success = true,
                    data = transacciones.Select(t => new
                    {
                        id = t.Id.ToString(),
                        tipo = t.Tipo,
                        descripcion = t.Descripcion,
                        monto = t.Monto,
                        moneda = t.Moneda,
                        comision = t.Comision,
                        estado = t.Estado,
                        fecha = t.FechaCreacion,
                        fechaEjecucion = t.FechaEjecucion,
                        cuentaOrigenNumero = t.CuentaOrigen?.Numero,
                        cuentaDestinoNumero = t.CuentaDestino?.Numero,
                        comprobanteReferencia = t.ComprobanteReferencia
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo transacciones del cliente: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/gestor/clientes/{clienteId}/cuentas
        /// Crea una nueva cuenta para un cliente
        /// </summary>
        [HttpPost("clientes/{clienteId}/cuentas")]
        public async Task<IActionResult> CrearCuentaParaCliente(int clienteId, [FromBody] CrearCuentaGestorRequest request)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                if (cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                // Validaciones
                if (!CuentasReglas.ValidarTipoCuenta(request.Tipo))
                    return BadRequest(new { success = false, message = "Tipo de cuenta inválido. Use: Ahorros, Corriente, Inversión o Plazo fijo" });

                if (!CuentasReglas.ValidarMoneda(request.Moneda))
                    return BadRequest(new { success = false, message = "Moneda inválida. Use: CRC o USD" });

                if (request.SaldoInicial < 0)
                    return BadRequest(new { success = false, message = "El saldo inicial no puede ser negativo." });

                // Crear cuenta
                var cuenta = await _cuentaServicio.CrearCuentaAsync(clienteId, request.Tipo, request.Moneda, request.SaldoInicial);

                // Registrar auditoría
                await _auditoriaServicio.RegistrarAsync(gestorId, "CreacionCuentaPorGestor",
                    $"Gestor creó cuenta {cuenta.Numero} para cliente {cliente.NombreCompleto}");

                return CreatedAtAction(nameof(GetCuentasCliente), new { clienteId = clienteId }, new
                {
                    success = true,
                    message = "Cuenta creada exitosamente.",
                    data = new
                    {
                        id = cuenta.Id,
                        numero = cuenta.Numero,
                        numeroCuenta = cuenta.Numero,
                        tipo = cuenta.Tipo,
                        moneda = cuenta.Moneda,
                        saldo = cuenta.Saldo,
                        estado = cuenta.Estado,
                        clienteId = clienteId,
                        cliente = cliente.NombreCompleto,
                        fechaApertura = cuenta.FechaApertura
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando cuenta para cliente: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region ========== GESTIÓN DE OPERACIONES ==========

        /// <summary>
        /// GET: api/gestor/operaciones
        /// Obtiene todas las operaciones con filtros
        /// </summary>
        [HttpGet("operaciones")]
        public async Task<IActionResult> GetOperaciones(
            [FromQuery] string? estado, 
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate,
            [FromQuery] decimal? minAmount, 
            [FromQuery] decimal? maxAmount,
            [FromQuery] string? clientName, 
            [FromQuery] string? operationType)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var todasOperaciones = await _transferenciasServicio.ObtenerOperacionesPorClientesAsync(clienteIds);

                // Aplicar filtros
                var operacionesFiltradas = todasOperaciones.AsEnumerable();

                if (!string.IsNullOrEmpty(estado) && estado != "all")
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.Estado == estado);

                if (startDate.HasValue)
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.FechaCreacion.Date >= startDate.Value.Date);

                if (endDate.HasValue)
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.FechaCreacion.Date <= endDate.Value.Date);

                if (minAmount.HasValue && minAmount > 0)
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.Monto >= minAmount.Value);

                if (maxAmount.HasValue && maxAmount > 0)
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.Monto <= maxAmount.Value);

                if (!string.IsNullOrEmpty(clientName))
                {
                    var searchTerm = clientName.ToLower().Trim();
                    operacionesFiltradas = operacionesFiltradas.Where(op =>
                        op.Cliente != null && op.Cliente.NombreCompleto.ToLower().Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(operationType))
                {
                    var searchType = operationType.ToLower().Trim();
                    operacionesFiltradas = operacionesFiltradas.Where(op => op.Tipo.ToLower().Contains(searchType));
                }

                var operacionesList = operacionesFiltradas.OrderByDescending(op => op.FechaCreacion).ToList();

                // Resumen
                var today = DateTime.UtcNow.Date;
                var pending = todasOperaciones.Count(op => op.Estado == "PendienteAprobacion");
                var approvedToday = todasOperaciones.Count(op => op.Estado == "Exitosa" && op.FechaEjecucion?.Date == today);
                var rejectedToday = todasOperaciones.Count(op => op.Estado == "Rechazada" && op.FechaCreacion.Date == today);

                return Ok(new
                {
                    success = true,
                    data = operacionesList.Select(op => new
                    {
                        id = op.Id.ToString(),
                        clienteId = op.ClienteId.ToString(),
                        clienteNombre = op.Cliente?.NombreCompleto ?? "N/A",
                        tipo = op.Tipo,
                        descripcion = op.Descripcion,
                        monto = op.Monto,
                        moneda = op.Moneda,
                        comision = op.Comision,
                        estado = op.Estado,
                        fecha = op.FechaCreacion,
                        cuentaOrigenNumero = op.CuentaOrigen?.Numero,
                        cuentaDestinoNumero = op.CuentaDestino?.Numero,
                        requiereAprobacion = op.Estado == "PendienteAprobacion",
                        esUrgente = op.Monto > 200000 && op.Estado == "PendienteAprobacion"
                    }),
                    summary = new 
                    { 
                        pending = pending, 
                        approved = approvedToday, 
                        rejected = rejectedToday 
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo operaciones: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/gestor/operaciones/{operacionId}
        /// Obtiene el detalle de una operación
        /// </summary>
        [HttpGet("operaciones/{operacionId}")]
        public async Task<IActionResult> GetDetalleOperacion(int operacionId)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(new { success = false, message = "Operación no encontrada." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = operacion.Id.ToString(),
                        clienteId = operacion.ClienteId.ToString(),
                        clienteNombre = operacion.Cliente?.NombreCompleto ?? "N/A",
                        tipo = operacion.Tipo,
                        descripcion = operacion.Descripcion,
                        monto = operacion.Monto,
                        moneda = operacion.Moneda,
                        comision = operacion.Comision,
                        estado = operacion.Estado,
                        fecha = operacion.FechaCreacion,
                        fechaEjecucion = operacion.FechaEjecucion,
                        cuentaOrigenNumero = operacion.CuentaOrigen?.Numero,
                        cuentaDestinoNumero = operacion.CuentaDestino?.Numero,
                        beneficiarioAlias = operacion.Beneficiario?.Alias,
                        comprobanteReferencia = operacion.ComprobanteReferencia,
                        saldoAnterior = operacion.SaldoAnterior,
                        saldoPosterior = operacion.SaldoPosterior,
                        requiereAprobacion = operacion.Estado == "PendienteAprobacion",
                        esUrgente = operacion.Monto > 200000
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo detalle de operación: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/gestor/operaciones/{operacionId}/aprobar
        /// Aprueba una operación pendiente
        /// </summary>
        [HttpPut("operaciones/{operacionId}/aprobar")]
        public async Task<IActionResult> AprobarOperacion(int operacionId)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(new { success = false, message = "Operación no encontrada." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                if (operacion.Estado != "PendienteAprobacion")
                    return BadRequest(new { success = false, message = "La operación no está pendiente de aprobación." });

                var operacionAprobada = await _transferenciasServicio.AprobarTransaccionAsync(operacionId, gestorId);

                await _auditoriaServicio.RegistrarAsync(gestorId, "AprobacionOperacion",
                    $"Gestor aprobó operación {operacionId} por {operacion.Moneda} {operacion.Monto}");

                return Ok(new
                {
                    success = true,
                    message = "Operación aprobada exitosamente.",
                    data = new 
                    { 
                        id = operacionAprobada.Id, 
                        estado = operacionAprobada.Estado, 
                        fechaEjecucion = operacionAprobada.FechaEjecucion 
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error aprobando operación: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/gestor/operaciones/{operacionId}/rechazar
        /// Rechaza una operación pendiente
        /// </summary>
        [HttpPut("operaciones/{operacionId}/rechazar")]
        public async Task<IActionResult> RechazarOperacion(int operacionId, [FromBody] RechazarOperacionRequest request)
        {
            try
            {
                var gestorId = GetGestorIdFromToken();
                if (gestorId == 0)
                    return Unauthorized(new { success = false, message = "Gestor no identificado." });

                if (string.IsNullOrWhiteSpace(request.Razon) || request.Razon.Trim().Length < 10)
                    return BadRequest(new { success = false, message = "El motivo debe tener al menos 10 caracteres." });

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(new { success = false, message = "Operación no encontrada." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return Forbid();

                if (operacion.Estado != "PendienteAprobacion")
                    return BadRequest(new { success = false, message = "La operación no está pendiente de aprobación." });

                var operacionRechazada = await _transferenciasServicio.RechazarTransaccionAsync(operacionId, gestorId, request.Razon);

                await _auditoriaServicio.RegistrarAsync(gestorId, "RechazoOperacion",
                    $"Gestor rechazó operación {operacionId}. Razón: {request.Razon}");

                return Ok(new
                {
                    success = true,
                    message = "Operación rechazada.",
                    data = new 
                    { 
                        id = operacionRechazada.Id, 
                        estado = operacionRechazada.Estado 
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rechazando operación: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    #region ========== DTOs ==========

    /// <summary>
    /// DTO para crear cuenta desde gestor
    /// </summary>
    public class CrearCuentaGestorRequest
    {
        public string Tipo { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; } = 0;
    }

    /// <summary>
    /// DTO para rechazar operación
    /// </summary>
    public class RechazarOperacionRequest
    {
        public string Razon { get; set; } = string.Empty;
    }

    #endregion
}