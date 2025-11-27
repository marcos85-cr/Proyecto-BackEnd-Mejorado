using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagosServiciosController : ControllerBase
    {
        private readonly IPagosServiciosServicio _pagosServicio;
        private readonly ILogger<PagosServiciosController> _logger;

        public PagosServiciosController(
            IPagosServiciosServicio pagosServicio,
            ILogger<PagosServiciosController> logger)
        {
            _pagosServicio = pagosServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-E1: Obtener lista de proveedores de servicio
        /// </summary>
        [HttpGet("proveedores")]
        public async Task<IActionResult> ObtenerProveedores()
        {
            try
            {
                var proveedores = await _pagosServicio.ObtenerProveedoresAsync();

                return Ok(new
                {
                    success = true,
                    data = proveedores.Select(p => new
                    {
                        id = p.Id,
                        nombre = p.Nombre,
                        reglaValidacion = p.ReglaValidacionContrato
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo proveedores: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Validar número de contrato
        /// </summary>
        [HttpPost("validar-contrato")]
        public async Task<IActionResult> ValidarContrato([FromBody] ValidarContratoRequest request)
        {
            try
            {
                var esValido = await _pagosServicio.ValidarNumeroContratoAsync(
                    request.ProveedorId,
                    request.NumeroContrato);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        esValido = esValido,
                        mensaje = esValido
                            ? "Número de contrato válido."
                            : "El número de contrato no cumple con el formato requerido."
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-E2: Realizar pago de servicio
        /// </summary>
        [HttpPost("realizar-pago")]
        public async Task<IActionResult> RealizarPagoServicio(
            [FromBody] RealizarPagoServicioRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var pagoRequest = new PagoServicioRequest
                {
                    ClienteId = clienteId,
                    CuentaOrigenId = request.CuentaOrigenId,
                    ProveedorServicioId = request.ProveedorServicioId,
                    NumeroContrato = request.NumeroContrato,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion,
                    IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
                };

                var transaccion = await _pagosServicio.RealizarPagoAsync(pagoRequest);

                return CreatedAtAction(nameof(ObtenerPago), new { id = transaccion.Id }, new
                {
                    success = true,
                    message = "Pago realizado exitosamente.",
                    data = new
                    {
                        transaccionId = transaccion.Id,
                        comprobanteReferencia = transaccion.ComprobanteReferencia,
                        monto = transaccion.Monto,
                        comision = transaccion.Comision,
                        estado = transaccion.Estado,
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
                _logger.LogError($"Error realizando pago: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-E3: Programar pago de servicio
        /// </summary>
        [HttpPost("programar-pago")]
        public async Task<IActionResult> ProgramarPagoServicio(
            [FromBody] ProgramarPagoServicioRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var pagoRequest = new PagoServicioRequest
                {
                    ClienteId = clienteId,
                    CuentaOrigenId = request.CuentaOrigenId,
                    ProveedorServicioId = request.ProveedorServicioId,
                    NumeroContrato = request.NumeroContrato,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion,
                    FechaProgramada = request.FechaProgramada,
                    IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
                };

                var transaccion = await _pagosServicio.ProgramarPagoAsync(pagoRequest);

                return CreatedAtAction(nameof(ObtenerPago), new { id = transaccion.Id }, new
                {
                    success = true,
                    message = "Pago programado exitosamente.",
                    data = new
                    {
                        transaccionId = transaccion.Id,
                        estado = transaccion.Estado,
                        fechaProgramada = request.FechaProgramada
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error programando pago: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener detalles de un pago
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPago(int id)
        {
            try
            {
                // Usar el servicio de transferencias para obtener la transacción
                return Ok(new { success = true, message = "Implementar usando ITransferenciasServicio" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener historial de pagos del cliente actual
        /// </summary>
        [HttpGet("mis-pagos")]
        public async Task<IActionResult> ObtenerMisPagos()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var pagos = await _pagosServicio.ObtenerHistorialPagosAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = pagos.Select(p => new
                    {
                        id = p.Id,
                        proveedor = p.ProveedorServicio?.Nombre,
                        numeroContrato = p.NumeroContrato,
                        monto = p.Monto,
                        moneda = p.Moneda,
                        comision = p.Comision,
                        estado = p.Estado,
                        fechaCreacion = p.FechaCreacion,
                        comprobanteReferencia = p.ComprobanteReferencia
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener historial de pagos de un cliente específico (admin/gestor)
        /// </summary>
        [HttpGet("cliente/{clienteId}/historial")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerHistorialPagos(int clienteId)
        {
            try
            {
                var pagos = await _pagosServicio.ObtenerHistorialPagosAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = pagos.Select(p => new
                    {
                        id = p.Id,
                        proveedor = p.ProveedorServicio?.Nombre,
                        monto = p.Monto,
                        estado = p.Estado,
                        fechaCreacion = p.FechaCreacion
                    })
                });
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
    }

    public class ValidarContratoRequest
    {
        public int ProveedorId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
    }

    public class RealizarPagoServicioRequest
    {
        public int CuentaOrigenId { get; set; }
        public int ProveedorServicioId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
    }

    public class ProgramarPagoServicioRequest
    {
        public int CuentaOrigenId { get; set; }
        public int ProveedorServicioId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaProgramada { get; set; }
        public string? Descripcion { get; set; }
    }
}