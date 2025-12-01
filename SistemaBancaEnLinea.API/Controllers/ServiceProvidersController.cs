using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceProvidersController : ControllerBase
    {
        private readonly IProveedorServicioServicio _proveedorServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceProvidersController> _logger;

        public ServiceProvidersController(
            IProveedorServicioServicio proveedorServicio,
            IAuditoriaServicio auditoriaServicio,
            IMapper mapper,
            ILogger<ServiceProvidersController> logger)
        {
            _proveedorServicio = proveedorServicio;
            _auditoriaServicio = auditoriaServicio;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProviders()
        {
            try
            {
                var proveedores = await _proveedorServicio.ObtenerTodosAsync();

                return Ok(ApiResponse<IEnumerable<ProveedorListaDto>>.Ok(
                    _mapper.Map<IEnumerable<ProveedorListaDto>>(proveedores)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proveedores");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProvider(int id)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado."));

                return Ok(ApiResponse<ProveedorDetalleDto>.Ok(
                    _mapper.Map<ProveedorDetalleDto>(proveedor)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateProvider([FromBody] CrearProveedorRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();

                var proveedor = new ProveedorServicio
                {
                    Nombre = request.Nombre,
                    ReglaValidacionContrato = request.ReglaValidacionContrato,
                    CreadoPorUsuarioId = adminId
                };

                var proveedorCreado = await _proveedorServicio.CrearAsync(proveedor);

                await _auditoriaServicio.RegistrarAsync(
                    adminId, "CreacionProveedor", $"Proveedor {request.Nombre} creado");

                return CreatedAtAction(nameof(GetProvider), new { id = proveedorCreado.Id },
                    ApiResponse<ProveedorCreacionDto>.Ok(
                        _mapper.Map<ProveedorCreacionDto>(proveedorCreado),
                        "Proveedor creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando proveedor");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] ActualizarProveedorRequest request)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado."));

                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    proveedor.Nombre = request.Nombre;

                if (!string.IsNullOrWhiteSpace(request.ReglaValidacion))
                    proveedor.ReglaValidacionContrato = request.ReglaValidacion;

                await _proveedorServicio.ActualizarAsync(id, proveedor);

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "ActualizacionProveedor", $"Proveedor {proveedor.Nombre} actualizado");

                return Ok(ApiResponse<ProveedorCreacionDto>.Ok(
                    _mapper.Map<ProveedorCreacionDto>(proveedor),
                    "Proveedor actualizado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            try
            {
                var resultado = await _proveedorServicio.EliminarAsync(id);
                if (!resultado)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado."));

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "EliminacionProveedor", $"Proveedor {id} eliminado");

                return Ok(ApiResponse.Ok("Proveedor eliminado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("{id}/validate-reference")]
        public async Task<IActionResult> ValidateReference(int id, [FromBody] ValidarReferenciaRequest request)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado."));

                var esValido = ProveedorServicioReglas.ValidarReferencia(
                    request.NumeroReferencia, proveedor.ReglaValidacionContrato);

                var monto = esValido ? new Random().Next(5000, 50000) : (decimal?)null;
                var nombre = esValido ? "Cliente de Ejemplo" : null;

                return Ok(ApiResponse<ValidacionReferenciaDto>.Ok(
                    new ValidacionReferenciaDto(esValido, monto, nombre,
                        esValido ? "Referencia válida" : "Número de referencia no válido")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando referencia");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #region Métodos Privados

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        #endregion
    }
}
