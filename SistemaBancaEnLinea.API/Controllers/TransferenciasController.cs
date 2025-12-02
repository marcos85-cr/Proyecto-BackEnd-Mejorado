using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransferenciasController : ControllerBase
    {
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ILogger<TransferenciasController> _logger;

        public TransferenciasController(
            ITransferenciasServicio transferenciasServicio,
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            ILogger<TransferenciasController> logger)
        {
            _transferenciasServicio = transferenciasServicio;
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _logger = logger;
        }

        [HttpPost("pre-check")]
        public async Task<IActionResult> PreCheckTransferencia([FromBody] PreCheckTransferenciaRequest request)
        {
            try
            {
                var transferRequest = new TransferRequest
                {
                    CuentaOrigenId = request.CuentaOrigenId,
                    CuentaDestinoId = request.CuentaDestinoId,
                    BeneficiarioId = request.BeneficiarioId,
                    Monto = request.Monto
                };

                var resultado = await _transferenciasServicio.PreCheckTransferenciaAsync(transferRequest);

                if (!resultado.PuedeEjecutar)
                    return BadRequest(ApiResponse<IEnumerable<string>>.Fail(resultado.Errores.FirstOrDefault() ?? "Validación fallida"));

                var dto = new PreCheckResultDto(
                    resultado.PuedeEjecutar,
                    resultado.SaldoAntes,
                    resultado.Monto,
                    resultado.Comision,
                    resultado.MontoTotal,
                    resultado.SaldoDespues,
                    resultado.RequiereAprobacion,
                    resultado.LimiteDisponible,
                    resultado.Mensaje);

                return Ok(ApiResponse<PreCheckResultDto>.Ok(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en pre-check de transferencia");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarTransferencia(
            [FromBody] EjecutarTransferenciaRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idempotencyKey))
                    return BadRequest(ApiResponse<object>.Fail("La cabecera Idempotency-Key es requerida."));

                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Cliente no identificado."));

                // Buscar cuenta destino por número
                int? cuentaDestinoId = null;
                if (!string.IsNullOrWhiteSpace(request.CuentaDestinoNumero))
                {
                    var cuentaDestino = await _cuentaServicio.ObtenerPorNumeroAsync(request.CuentaDestinoNumero);
                    if (cuentaDestino == null)
                        return BadRequest(ApiResponse<object>.Fail("La cuenta destino no existe."));

                    // Validar que las monedas coincidan
                    var cuentaOrigen = await _cuentaServicio.ObtenerCuentaAsync(request.CuentaOrigenId);
                    if (cuentaOrigen == null)
                        return BadRequest(ApiResponse<object>.Fail("La cuenta origen no existe."));

                    if (cuentaOrigen.Moneda != cuentaDestino.Moneda)
                        return BadRequest(ApiResponse<object>.Fail($"Las monedas de las cuentas no coinciden. Cuenta origen: {cuentaOrigen.Moneda}, Cuenta destino: {cuentaDestino.Moneda}"));

                    cuentaDestinoId = cuentaDestino.Id;
                }

                var transferRequest = new TransferRequest
                {
                    ClienteId = clienteId,
                    CuentaOrigenId = request.CuentaOrigenId,
                    CuentaDestinoId = cuentaDestinoId,
                    BeneficiarioId = request.BeneficiarioId,
                    Monto = request.Monto,
                    Moneda = request.Moneda,
                    Descripcion = request.Descripcion,
                    Programada = request.Programada,
                    FechaProgramada = request.FechaProgramada,
                    IdempotencyKey = idempotencyKey
                };

                var transaccion = await _transferenciasServicio.EjecutarTransferenciaAsync(transferRequest);

                var mensaje = transaccion.Estado switch
                {
                    "PendienteAprobacion" => "Transferencia pendiente de aprobación.",
                    "Programada" => "Transferencia programada exitosamente.",
                    _ => "Transferencia ejecutada exitosamente."
                };

                var dto = new TransferenciaEjecutadaDto(
                    transaccion.Id,
                    transaccion.Estado,
                    transaccion.ComprobanteReferencia,
                    transaccion.FechaCreacion,
                    transaccion.FechaEjecucion);

                return CreatedAtAction(nameof(ObtenerTransferencia), new { id = transaccion.Id },
                    ApiResponse<TransferenciaEjecutadaDto>.Ok(dto, mensaje));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando transferencia");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerTransferencia(int id)
        {
            try
            {
                var transaccion = await _transferenciasServicio.ObtenerTransaccionAsync(id);
                if (transaccion == null)
                    return NotFound(ApiResponse<object>.Fail("Transferencia no encontrada."));

                var dto = new TransferenciaTransaccionDetalleDto(
                    transaccion.Id,
                    transaccion.Tipo,
                    transaccion.Estado,
                    transaccion.Monto,
                    transaccion.Moneda,
                    transaccion.Comision,
                    transaccion.Descripcion,
                    transaccion.ComprobanteReferencia,
                    transaccion.FechaCreacion,
                    transaccion.FechaEjecucion,
                    transaccion.CuentaOrigen?.Numero,
                    transaccion.CuentaDestino?.Numero,
                    transaccion.Beneficiario?.Alias,
                    transaccion.SaldoAnterior,
                    transaccion.SaldoPosterior);

                return Ok(ApiResponse<TransferenciaTransaccionDetalleDto>.Ok(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transferencia {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("mis-transferencias")]
        public async Task<IActionResult> ObtenerMisTransferencias()
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Cliente no identificado."));

                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);

                var dtos = transacciones.Select(t => new TransferenciaTransaccionListaDto(
                    t.Id, t.Tipo, t.Estado, t.Monto, t.Moneda, t.Comision,
                    t.Descripcion, t.ComprobanteReferencia, t.FechaCreacion, t.FechaEjecucion));

                return Ok(ApiResponse<IEnumerable<TransferenciaTransaccionListaDto>>.Ok(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mis transferencias");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerHistorialTransferencias(int clienteId)
        {
            try
            {
                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);

                var dtos = transacciones.Select(t => new TransferenciaHistorialDto(
                    t.Id, t.Tipo, t.Estado, t.Monto, t.Moneda, t.FechaCreacion));

                return Ok(ApiResponse<IEnumerable<TransferenciaHistorialDto>>.Ok(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de transferencias para cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}/aprobar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AprobarTransferencia(int id)
        {
            try
            {
                var aprobadorId = GetUsuarioId();
                var transaccion = await _transferenciasServicio.AprobarTransaccionAsync(id, aprobadorId);

                var dto = new TransferenciaEstadoDto(transaccion.Id, transaccion.Estado);

                return Ok(ApiResponse<TransferenciaEstadoDto>.Ok(dto, "Transferencia aprobada exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aprobando transferencia {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}/rechazar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RechazarTransferencia(int id, [FromBody] RechazarTransferenciaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Razon))
                    return BadRequest(ApiResponse<object>.Fail("Debe proporcionar una razón."));

                var aprobadorId = GetUsuarioId();
                var transaccion = await _transferenciasServicio.RechazarTransaccionAsync(id, aprobadorId, request.Razon);

                var dto = new TransferenciaEstadoDto(transaccion.Id, transaccion.Estado);

                return Ok(ApiResponse<TransferenciaEstadoDto>.Ok(dto, "Transferencia rechazada."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechazando transferencia {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}/comprobante")]
        public async Task<IActionResult> DescargarComprobante(int id)
        {
            try
            {
                var comprobante = await _transferenciasServicio.DescargarComprobanteAsync(id);
                return File(comprobante, "text/plain", $"comprobante_{id}.txt");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando comprobante de transferencia {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor"));
            }
        }

        #region Helpers

        private async Task<int> GetClienteIdAsync()
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return 0;
            
            var cliente = await _clienteServicio.ObtenerPorUsuarioAsync(usuarioId);
            return cliente?.Id ?? 0;
        }

        private int GetUsuarioId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        #endregion
    }
}
