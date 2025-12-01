using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using BwPagoRequest = SistemaBancaEnLinea.BW.Interfaces.BW.PagoServicioRequest;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagosServiciosController : ControllerBase
    {
        private readonly IPagosServiciosServicio _pagosServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly IMapper _mapper;
        private readonly ILogger<PagosServiciosController> _logger;

        public PagosServiciosController(
            IPagosServiciosServicio pagosServicio,
            IClienteServicio clienteServicio,
            IAuditoriaServicio auditoriaServicio,
            IMapper mapper,
            ILogger<PagosServiciosController> logger)
        {
            _pagosServicio = pagosServicio;
            _clienteServicio = clienteServicio;
            _auditoriaServicio = auditoriaServicio;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("proveedores")]
        public async Task<IActionResult> ObtenerProveedores()
        {
            try
            {
                var proveedores = await _pagosServicio.ObtenerProveedoresAsync();

                return Ok(ApiResponse<IEnumerable<ProveedorServicioDto>>.Ok(
                    _mapper.Map<IEnumerable<ProveedorServicioDto>>(proveedores)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proveedores");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("validar-contrato")]
        public async Task<IActionResult> ValidarContrato([FromBody] ValidarContratoRequest request)
        {
            try
            {
                var proveedor = await _pagosServicio.ObtenerProveedorAsync(request.ProveedorId);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado."));

                var esValido = await _pagosServicio.ValidarNumeroContratoAsync(
                    request.ProveedorId, request.NumeroContrato);

                return Ok(ApiResponse<ValidacionContratoResponse>.Ok(
                    new ValidacionContratoResponse(
                        esValido,
                        esValido ? "Número de contrato válido." : "El número de contrato no cumple con el formato requerido.",
                        proveedor.Nombre)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando contrato");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("realizar-pago")]
        public async Task<IActionResult> RealizarPagoServicio(
            [FromBody] RealizarPagoRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var pagoRequest = new BwPagoRequest
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

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "PagoServicio", 
                    $"Pago de {request.Monto} realizado. Contrato: {request.NumeroContrato}");

                return CreatedAtAction(nameof(ObtenerPago), new { id = transaccion.Id },
                    ApiResponse<PagoRealizadoDto>.Ok(
                        _mapper.Map<PagoRealizadoDto>(transaccion),
                        "Pago realizado exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error realizando pago");
                return StatusCode(500, ApiResponse.Fail("Error interno al procesar el pago"));
            }
        }

        [HttpPost("programar-pago")]
        public async Task<IActionResult> ProgramarPagoServicio(
            [FromBody] ProgramarPagoRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var pagoRequest = new BwPagoRequest
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

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "ProgramacionPagoServicio",
                    $"Pago de {request.Monto} programado para {request.FechaProgramada:dd/MM/yyyy}");

                return CreatedAtAction(nameof(ObtenerPago), new { id = transaccion.Id },
                    ApiResponse<PagoProgramadoDto>.Ok(
                        new PagoProgramadoDto(
                            transaccion.Id, 
                            transaccion.Estado, 
                            request.FechaProgramada, 
                            transaccion.Monto, 
                            transaccion.Comision,
                            transaccion.ProveedorServicio?.Nombre),
                        "Pago programado exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error programando pago");
                return StatusCode(500, ApiResponse.Fail("Error interno al programar el pago"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPago(int id)
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var pagos = await _pagosServicio.ObtenerHistorialPagosAsync(clienteId);
                var pago = pagos.FirstOrDefault(p => p.Id == id);

                if (pago == null)
                    return NotFound(ApiResponse.Fail("Pago no encontrado."));

                return Ok(ApiResponse<PagoDetalleDto>.Ok(
                    _mapper.Map<PagoDetalleDto>(pago)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo pago {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("mis-pagos")]
        public async Task<IActionResult> ObtenerMisPagos()
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var pagos = await _pagosServicio.ObtenerHistorialPagosAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<PagoListaDto>>.Ok(
                    _mapper.Map<IEnumerable<PagoListaDto>>(pagos).OrderByDescending(p => p.FechaCreacion)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de pagos");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("cliente/{clienteId}/historial")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerHistorialPagos(int clienteId)
        {
            try
            {
                var pagos = await _pagosServicio.ObtenerHistorialPagosAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<PagoResumenDto>>.Ok(
                    _mapper.Map<IEnumerable<PagoResumenDto>>(pagos)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de pagos del cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #region Métodos Privados

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
