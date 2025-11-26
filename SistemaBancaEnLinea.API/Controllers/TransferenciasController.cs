using Microsoft.AspNetCore.Mvc;
using SistemaBancaEnLinea.BW.CU;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransferenciasController : ControllerBase
    {
        private readonly TransferenciasCU _transferenciasCU;

        public TransferenciasController(TransferenciasCU transferenciasCU)
        {
            _transferenciasCU = transferenciasCU;
        }

        /// <summary>
        /// RF-D1: Pre-check de transferencia (validar antes de ejecutar)
        /// </summary>
        [HttpPost("pre-check")]
        public async Task<IActionResult> PreCheckTransferencia([FromBody] PreCheckTransferenciaRequest request)
        {
            try
            {
                var resultado = await _transferenciasCU.PreCheckTransferenciaAsync(
                    request.CuentaOrigenId,
                    request.CuentaDestinoId,
                    request.BeneficiarioId,
                    request.Monto
                );

                if (!resultado.EsValido)
                    return BadRequest(new { errores = resultado.Errores });

                return Ok(new
                {
                    esValido = resultado.EsValido,
                    saldoAntes = resultado.SaldoAntes,
                    montoADebitar = resultado.MontoADebitar,
                    comision = resultado.Comision,
                    saldoDespues = resultado.SaldoDespues,
                    requiereAprobacion = resultado.RequiereAprobacion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
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
                    return BadRequest(new { mensaje = "La cabecera Idempotency-Key es requerida." });

                var transaccion = await _transferenciasCU.EjecutarTransferenciaAsync(
                    request.ClienteId,
                    request.CuentaOrigenId,
                    request.CuentaDestinoId,
                    request.BeneficiarioId,
                    request.Monto,
                    request.Moneda,
                    idempotencyKey,
                    request.Descripcion
                );

                return CreatedAtAction(nameof(ObtenerTransferencia), new { id = transaccion.Id },
                    new
                    {
                        mensaje = "Transferencia ejecutada.",
                        transaccionId = transaccion.Id,
                        estado = transaccion.Estado,
                        comprobanteReferencia = transaccion.ComprobanteReferencia
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtener detalles de una transferencia
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerTransferencia(int id)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// Obtener historial de transferencias de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerHistorialTransferencias(int clienteId)
        {
            // TODO: Implementar lógica
            return Ok();
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
        public int ClienteId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; }
        public int? BeneficiarioId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}