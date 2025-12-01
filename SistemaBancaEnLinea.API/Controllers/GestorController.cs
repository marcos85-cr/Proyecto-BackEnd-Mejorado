using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
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

        #region Dashboard

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                int totalCuentasActivas = 0;
                decimal volumenTotal = 0;

                foreach (var cliente in misClientes)
                {
                    var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(cliente.Id);
                    var activas = cuentas.Where(c => c.Estado == "Activa").ToList();
                    totalCuentasActivas += activas.Count;
                    volumenTotal += activas.Sum(c => c.Saldo);
                }

                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var operacionesHoy = await _transferenciasServicio.ObtenerOperacionesDeHoyPorClientesAsync(clienteIds);
                var pendientes = await _transferenciasServicio.ObtenerOperacionesPendientesPorClientesAsync(clienteIds);

                return Ok(ApiResponse<GestorDashboardDto>.Ok(new GestorDashboardDto(
                    misClientes.Count, totalCuentasActivas, operacionesHoy.Count, 
                    pendientes.Count, volumenTotal)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo stats del dashboard");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("operaciones-pendientes")]
        public async Task<IActionResult> GetOperacionesPendientes()
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var pendientes = await _transferenciasServicio.ObtenerOperacionesPendientesPorClientesAsync(clienteIds);

                return Ok(ApiResponse<IEnumerable<OperacionPendienteDto>>.Ok(
                    pendientes.Select(op => new OperacionPendienteDto(
                        op.Id, op.ClienteId, op.Cliente?.UsuarioAsociado?.Nombre ?? "N/A",
                        op.Tipo, op.Descripcion, op.Monto, op.Moneda, op.Comision, op.Estado,
                        op.FechaCreacion, op.CuentaOrigen?.Numero, op.CuentaDestino?.Numero,
                        true, op.Monto > 200000))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo operaciones pendientes");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #endregion

        #region Clientes

        [HttpGet("mis-clientes")]
        public async Task<IActionResult> GetMisClientes()
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                var clientesDto = new List<ClienteGestorDto>();
                int totalCuentas = 0;
                decimal volumenTotal = 0;

                foreach (var cliente in misClientes)
                {
                    var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(cliente.Id);
                    var activas = cuentas.Where(c => c.Estado == "Activa").ToList();
                    var volumen = activas.Sum(c => c.Saldo);
                    var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(cliente.Id);
                    var ultima = transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault();

                    totalCuentas += activas.Count;
                    volumenTotal += volumen;

                    clientesDto.Add(new ClienteGestorDto(
                        cliente.Id, cliente.UsuarioAsociado?.Nombre ?? "N/A",
                        cliente.UsuarioAsociado?.Email ?? "N/A",
                        cliente.UsuarioAsociado?.Identificacion ?? "N/A",
                        cliente.UsuarioAsociado?.Telefono ?? "N/A",
                        activas.Count, ultima?.FechaCreacion ?? cliente.FechaRegistro,
                        cliente.Estado, volumen));
                }

                return Ok(ApiResponse<ClientesGestorResponseDto>.Ok(new ClientesGestorResponseDto(
                    clientesDto, new ClientesStatsDto(misClientes.Count, totalCuentas, volumenTotal))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo clientes del gestor");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("clientes/{clienteId}")]
        public async Task<IActionResult> GetDetalleCliente(int clienteId)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(ApiResponse.Fail("Cliente no encontrado"));

                if (cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede acceder a clientes fuera de su cartera"));

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);
                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);
                var activas = cuentas.Where(c => c.Estado == "Activa").ToList();
                var ultima = transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault();

                var cuentasDto = cuentas.Select(c => new CuentaSimpleDto(
                    c.Id, c.Numero, c.Tipo, c.Moneda, c.Saldo, c.Estado, c.FechaApertura)).ToList();

                return Ok(ApiResponse<ClienteDetalleGestorDto>.Ok(new ClienteDetalleGestorDto(
                    cliente.Id, cliente.UsuarioAsociado?.Identificacion ?? "N/A",
                    cliente.UsuarioAsociado?.Nombre ?? "N/A",
                    cliente.UsuarioAsociado?.Telefono ?? "N/A",
                    cliente.UsuarioAsociado?.Email ?? "N/A",
                    cliente.Estado, cliente.FechaRegistro, ultima?.FechaCreacion,
                    activas.Count, activas.Sum(c => c.Saldo), transacciones.Count, cuentasDto)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle del cliente {Id}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("clientes/{clienteId}/cuentas")]
        public async Task<IActionResult> GetCuentasCliente(int clienteId)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(ApiResponse.Fail("Cliente no encontrado"));

                if (cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede acceder a clientes fuera de su cartera"));

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<CuentaGestorDto>>.Ok(
                    cuentas.Select(c => new CuentaGestorDto(
                        c.Id, c.Numero, c.Tipo, c.Moneda, c.Saldo, c.Estado,
                        c.FechaApertura ?? DateTime.UtcNow, clienteId,
                        cliente.UsuarioAsociado?.Nombre ?? "N/A"))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cuentas del cliente {Id}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

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
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(ApiResponse.Fail("Cliente no encontrado"));

                if (cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede acceder a clientes fuera de su cartera"));

                var transacciones = await _transferenciasServicio.ObtenerTransaccionesConFiltrosAsync(
                    clienteId, fechaInicio, fechaFin, tipo, estado);

                return Ok(ApiResponse<IEnumerable<TransaccionGestorDto>>.Ok(
                    transacciones.Select(t => new TransaccionGestorDto(
                        t.Id, t.Tipo, t.Descripcion, t.Monto, t.Moneda, t.Comision, t.Estado,
                        t.FechaCreacion, t.FechaEjecucion, t.CuentaOrigen?.Numero,
                        t.CuentaDestino?.Numero, t.ComprobanteReferencia))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transacciones del cliente {Id}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("clientes/{clienteId}/cuentas")]
        public async Task<IActionResult> CrearCuentaParaCliente(int clienteId, [FromBody] CrearCuentaGestorRequest request)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(ApiResponse.Fail("Cliente no encontrado"));

                if (cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("Solo puede crear cuentas para clientes de su cartera"));

                if (!CuentasReglas.ValidarTipoCuenta(request.Tipo))
                    return BadRequest(ApiResponse.Fail("Tipo de cuenta inválido"));

                if (!CuentasReglas.ValidarMoneda(request.Moneda))
                    return BadRequest(ApiResponse.Fail("Moneda inválida. Use: CRC o USD"));

                if (request.SaldoInicial < 0)
                    return BadRequest(ApiResponse.Fail("El saldo inicial no puede ser negativo"));

                var cuentasExistentes = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);
                var validacion = CuentasReglas.ValidarMaximoCuentasMismoTipoMoneda(
                    cuentasExistentes, request.Tipo, request.Moneda);

                if (!validacion.EsValido)
                    return BadRequest(ApiResponse.Fail(validacion.Error!));

                var cuenta = await _cuentaServicio.CrearCuentaAsync(
                    clienteId, request.Tipo, request.Moneda, request.SaldoInicial);

                await _auditoriaServicio.RegistrarAsync(gestorId, "CreacionCuentaPorGestor",
                    $"Cuenta {cuenta.Numero} creada para cliente {cliente.UsuarioAsociado?.Nombre}");

                return CreatedAtAction(nameof(GetCuentasCliente), new { clienteId },
                    ApiResponse<CuentaCreadaGestorDto>.Ok(new CuentaCreadaGestorDto(
                        cuenta.Id, cuenta.Numero, cuenta.Tipo, cuenta.Moneda, cuenta.Saldo,
                        cuenta.Estado, clienteId, cliente.UsuarioAsociado?.Nombre ?? "N/A",
                        cuenta.FechaApertura), "Cuenta creada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando cuenta para cliente {Id}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #endregion

        #region Operaciones

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
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var misClientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                var clienteIds = misClientes.Select(c => c.Id).ToList();
                var todas = await _transferenciasServicio.ObtenerOperacionesPorClientesAsync(clienteIds);

                var filtradas = todas.AsEnumerable();

                if (!string.IsNullOrEmpty(estado) && estado != "all")
                    filtradas = filtradas.Where(op => op.Estado == estado);
                if (startDate.HasValue)
                    filtradas = filtradas.Where(op => op.FechaCreacion.Date >= startDate.Value.Date);
                if (endDate.HasValue)
                    filtradas = filtradas.Where(op => op.FechaCreacion.Date <= endDate.Value.Date);
                if (minAmount.HasValue && minAmount > 0)
                    filtradas = filtradas.Where(op => op.Monto >= minAmount.Value);
                if (maxAmount.HasValue && maxAmount > 0)
                    filtradas = filtradas.Where(op => op.Monto <= maxAmount.Value);
                if (!string.IsNullOrEmpty(clientName))
                    filtradas = filtradas.Where(op => (op.Cliente?.UsuarioAsociado?.Nombre ?? "").ToLower().Contains(clientName.ToLower()));
                if (!string.IsNullOrEmpty(operationType))
                    filtradas = filtradas.Where(op => op.Tipo.ToLower().Contains(operationType.ToLower()));

                var lista = filtradas.OrderByDescending(op => op.FechaCreacion).ToList();
                var hoy = DateTime.UtcNow.Date;

                var operacionesDto = lista.Select(op => new OperacionDto(
                    op.Id, op.ClienteId, op.Cliente?.UsuarioAsociado?.Nombre ?? "N/A",
                    op.Tipo, op.Descripcion, op.Monto, op.Moneda, op.Comision, op.Estado,
                    op.FechaCreacion, op.CuentaOrigen?.Numero, op.CuentaDestino?.Numero,
                    op.Estado == "PendienteAprobacion",
                    op.Monto > 200000 && op.Estado == "PendienteAprobacion"));

                var resumen = new OperacionesResumenDto(
                    todas.Count(op => op.Estado == "PendienteAprobacion"),
                    todas.Count(op => op.Estado == "Exitosa" && op.FechaEjecucion?.Date == hoy),
                    todas.Count(op => op.Estado == "Rechazada" && op.FechaCreacion.Date == hoy));

                return Ok(ApiResponse<OperacionesResponseDto>.Ok(
                    new OperacionesResponseDto(operacionesDto, resumen)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo operaciones");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("operaciones/{operacionId}")]
        public async Task<IActionResult> GetDetalleOperacion(int operacionId)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(ApiResponse.Fail("Operación no encontrada"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede acceder a operaciones fuera de su cartera"));

                return Ok(ApiResponse<OperacionDetalleDto>.Ok(new OperacionDetalleDto(
                    operacion.Id, operacion.ClienteId, operacion.Cliente?.UsuarioAsociado?.Nombre ?? "N/A",
                    operacion.Tipo, operacion.Descripcion, operacion.Monto, operacion.Moneda,
                    operacion.Comision, operacion.Estado, operacion.FechaCreacion, operacion.FechaEjecucion,
                    operacion.CuentaOrigen?.Numero, operacion.CuentaDestino?.Numero,
                    operacion.Beneficiario?.Alias, operacion.ComprobanteReferencia,
                    operacion.SaldoAnterior, operacion.SaldoPosterior,
                    operacion.Estado == "PendienteAprobacion", operacion.Monto > 200000)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle de operación {Id}", operacionId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("operaciones/{operacionId}/aprobar")]
        public async Task<IActionResult> AprobarOperacion(int operacionId)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(ApiResponse.Fail("Operación no encontrada"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede aprobar operaciones fuera de su cartera"));

                if (operacion.Estado != "PendienteAprobacion")
                    return BadRequest(ApiResponse.Fail("La operación no está pendiente de aprobación"));

                var validacion = TransferenciasReglas.ValidarAprobacionGestor(operacion.Monto);
                if (!validacion.EsValido)
                    return BadRequest(ApiResponse.Fail(validacion.Error!));

                var aprobada = await _transferenciasServicio.AprobarTransaccionAsync(operacionId, gestorId);

                await _auditoriaServicio.RegistrarAsync(gestorId, "AprobacionOperacion",
                    $"Operación {operacionId} aprobada por {operacion.Moneda} {operacion.Monto}");

                return Ok(ApiResponse<OperacionResultadoDto>.Ok(
                    new OperacionResultadoDto(aprobada.Id, aprobada.Estado, aprobada.FechaEjecucion),
                    "Operación aprobada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aprobando operación {Id}", operacionId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("operaciones/{operacionId}/rechazar")]
        public async Task<IActionResult> RechazarOperacion(int operacionId, [FromBody] RechazarOperacionRequest request)
        {
            try
            {
                var gestorId = GetGestorId();
                if (gestorId == 0)
                    return Unauthorized(ApiResponse.Fail("Gestor no identificado"));

                if (string.IsNullOrWhiteSpace(request.Razon) || request.Razon.Trim().Length < 10)
                    return BadRequest(ApiResponse.Fail("El motivo debe tener al menos 10 caracteres"));

                var operacion = await _transferenciasServicio.ObtenerTransaccionAsync(operacionId);
                if (operacion == null)
                    return NotFound(ApiResponse.Fail("Operación no encontrada"));

                var cliente = await _clienteServicio.ObtenerClienteAsync(operacion.ClienteId);
                if (cliente == null || cliente.GestorAsignadoId != gestorId)
                    return StatusCode(403, ApiResponse.Fail("No puede rechazar operaciones fuera de su cartera"));

                if (operacion.Estado != "PendienteAprobacion")
                    return BadRequest(ApiResponse.Fail("La operación no está pendiente de aprobación"));

                var rechazada = await _transferenciasServicio.RechazarTransaccionAsync(operacionId, gestorId, request.Razon);

                await _auditoriaServicio.RegistrarAsync(gestorId, "RechazoOperacion",
                    $"Operación {operacionId} rechazada. Razón: {request.Razon}");

                return Ok(ApiResponse<OperacionResultadoDto>.Ok(
                    new OperacionResultadoDto(rechazada.Id, rechazada.Estado, null),
                    "Operación rechazada"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechazando operación {Id}", operacionId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #endregion

        #region Helpers

        private int GetGestorId()
        {
            var claim = User.FindFirst("sub")?.Value ??
                        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        #endregion
    }
}
