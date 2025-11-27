using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransferenciasController : ControllerBase
    {
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly ILogger<TransferenciasController> _logger;

        public TransferenciasController(
            ITransferenciasServicio transferenciasServicio,
            ILogger<TransferenciasController> logger)
        {
            _transferenciasServicio = transferenciasServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-D1: Pre-check de transferencia (validar antes de ejecutar)
        /// </summary>
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
                    return BadRequest(new { success = false, errores = resultado.Errores });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        puedeEjecutar = resultado.PuedeEjecutar,
                        saldoAntes = resultado.SaldoAntes,
                        monto = resultado.Monto,
                        comision = resultado.Comision,
                        montoTotal = resultado.MontoTotal,
                        saldoDespues = resultado.SaldoDespues,
                        requiereAprobacion = resultado.RequiereAprobacion,
                        limiteDisponible = resultado.LimiteDisponible,
                        mensaje = resultado.Mensaje
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en pre-check: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-D2: Ejecutar transferencia (requiere Idempotency-Key)
        /// </summary>
        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarTransferencia(
            [FromBody] EjecutarTransferenciaRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idempotencyKey))
                    return BadRequest(new { success = false, message = "La cabecera Idempotency-Key es requerida." });

                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var transferRequest = new TransferRequest
                {
                    ClienteId = clienteId,
                    CuentaOrigenId = request.CuentaOrigenId,
                    CuentaDestinoId = request.CuentaDestinoId,
                    BeneficiarioId = request.BeneficiarioId,
                    Monto = request.Monto,
                    Moneda = request.Moneda,
                    Descripcion = request.Descripcion,
                    Programada = request.Programada,
                    FechaProgramada = request.FechaProgramada,
                    IdempotencyKey = idempotencyKey
                };

                var transaccion = await _transferenciasServicio.EjecutarTransferenciaAsync(transferRequest);

                return CreatedAtAction(nameof(ObtenerTransferencia), new { id = transaccion.Id }, new
                {
                    success = true,
                    message = transaccion.Estado == "PendienteAprobacion"
                        ? "Transferencia pendiente de aprobación."
                        : transaccion.Estado == "Programada"
                            ? "Transferencia programada exitosamente."
                            : "Transferencia ejecutada exitosamente.",
                    data = new
                    {
                        transaccionId = transaccion.Id,
                        estado = transaccion.Estado,
                        comprobanteReferencia = transaccion.ComprobanteReferencia,
                        fechaCreacion = transaccion.FechaCreacion,
                        fechaEjecucion = transaccion.FechaEjecucion
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ejecutando transferencia: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener detalles de una transferencia
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerTransferencia(int id)
        {
            try
            {
                var transaccion = await _transferenciasServicio.ObtenerTransaccionAsync(id);
                if (transaccion == null)
                    return NotFound(new { success = false, message = "Transferencia no encontrada." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = transaccion.Id,
                        tipo = transaccion.Tipo,
                        estado = transaccion.Estado,
                        monto = transaccion.Monto,
                        moneda = transaccion.Moneda,
                        comision = transaccion.Comision,
                        descripcion = transaccion.Descripcion,
                        comprobanteReferencia = transaccion.ComprobanteReferencia,
                        fechaCreacion = transaccion.FechaCreacion,
                        fechaEjecucion = transaccion.FechaEjecucion,
                        cuentaOrigenNumero = transaccion.CuentaOrigen?.Numero,
                        cuentaDestinoNumero = transaccion.CuentaDestino?.Numero,
                        beneficiarioAlias = transaccion.Beneficiario?.Alias,
                        saldoAnterior = transaccion.SaldoAnterior,
                        saldoPosterior = transaccion.SaldoPosterior
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener mis transferencias
        /// </summary>
        [HttpGet("mis-transferencias")]
        public async Task<IActionResult> ObtenerMisTransferencias()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = transacciones.Select(t => new
                    {
                        id = t.Id,
                        tipo = t.Tipo,
                        estado = t.Estado,
                        monto = t.Monto,
                        moneda = t.Moneda,
                        comision = t.Comision,
                        descripcion = t.Descripcion,
                        comprobanteReferencia = t.ComprobanteReferencia,
                        fechaCreacion = t.FechaCreacion,
                        fechaEjecucion = t.FechaEjecucion
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener historial de transferencias de un cliente (admin/gestor)
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerHistorialTransferencias(int clienteId)
        {
            try
            {
                var transacciones = await _transferenciasServicio.ObtenerMisTransaccionesAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = transacciones.Select(t => new
                    {
                        id = t.Id,
                        tipo = t.Tipo,
                        estado = t.Estado,
                        monto = t.Monto,
                        moneda = t.Moneda,
                        fechaCreacion = t.FechaCreacion
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Aprobar transferencia pendiente (solo admin)
        /// </summary>
        [HttpPut("{id}/aprobar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AprobarTransferencia(int id)
        {
            try
            {
                var aprobadorId = GetUserIdFromToken();
                var transaccion = await _transferenciasServicio.AprobarTransaccionAsync(id, aprobadorId);

                return Ok(new
                {
                    success = true,
                    message = "Transferencia aprobada exitosamente.",
                    data = new
                    {
                        id = transaccion.Id,
                        estado = transaccion.Estado
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Rechazar transferencia pendiente (solo admin)
        /// </summary>
        [HttpPut("{id}/rechazar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RechazarTransferencia(int id, [FromBody] RechazarTransferenciaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Razon))
                    return BadRequest(new { success = false, message = "Debe proporcionar una razón." });

                var aprobadorId = GetUserIdFromToken();
                var transaccion = await _transferenciasServicio.RechazarTransaccionAsync(id, aprobadorId, request.Razon);

                return Ok(new
                {
                    success = true,
                    message = "Transferencia rechazada.",
                    data = new
                    {
                        id = transaccion.Id,
                        estado = transaccion.Estado
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Descargar comprobante de transferencia
        /// </summary>
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
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private int GetClienteIdFromToken()
        {
            var clienteIdClaim = User.FindFirst("client_id")?.Value;
            return int.TryParse(clienteIdClaim, out var clienteId) ? clienteId : 0;
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    // DTOs
    public class PreCheckTransferenciaRequest
    {
        public int CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; }
        public int? BeneficiarioId { get; set; }
        public decimal Monto { get; set; }
    }

    public class EjecutarTransferenciaRequest
    {
        public int CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; }
        public int? BeneficiarioId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "CRC";
        public string? Descripcion { get; set; }
        public bool Programada { get; set; }
        public DateTime? FechaProgramada { get; set; }
    }

    public class RechazarTransferenciaRequest
    {
        public string Razon { get; set; } = string.Empty;
    }
}