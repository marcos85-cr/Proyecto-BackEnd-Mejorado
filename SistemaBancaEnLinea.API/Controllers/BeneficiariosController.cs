using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BeneficiariosController : ControllerBase
    {
        private readonly IBeneficiarioServicio _beneficiarioServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<BeneficiariosController> _logger;

        public BeneficiariosController(
            IBeneficiarioServicio beneficiarioServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<BeneficiariosController> logger)
        {
            _beneficiarioServicio = beneficiarioServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearBeneficiario([FromBody] CrearBeneficiarioRequest request)
        {
            try
            {
                var clienteId = GetClienteId();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var beneficiario = BeneficiariosReglas.MapearDesdeRequest(request, clienteId);
                var beneficiarioCreado = await _beneficiarioServicio.CrearBeneficiarioAsync(beneficiario);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "CreacionBeneficiario", $"Beneficiario {request.Alias} creado");

                return CreatedAtAction(nameof(ObtenerBeneficiario), new { id = beneficiarioCreado.Id },
                    ApiResponse<BeneficiarioCreacionDto>.Ok(
                        BeneficiariosReglas.MapearACreacionDto(beneficiarioCreado),
                        "Beneficiario creado. Debe confirmarlo antes de poder usarlo."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando beneficiario");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerBeneficiario(int id)
        {
            try
            {
                var beneficiario = await _beneficiarioServicio.ObtenerBeneficiarioAsync(id);
                if (beneficiario == null)
                    return NotFound(ApiResponse.Fail("Beneficiario no encontrado."));

                if (!PuedoAccederBeneficiario(beneficiario.ClienteId))
                    return Forbid();

                var tieneOperaciones = await _beneficiarioServicio.TieneOperacionesPendientesAsync(id);

                return Ok(ApiResponse<BeneficiarioDetalleDto>.Ok(
                    BeneficiariosReglas.MapearADetalleDto(beneficiario, tieneOperaciones)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarBeneficiario(int id)
        {
            try
            {
                var beneficiarioExistente = await _beneficiarioServicio.ObtenerBeneficiarioAsync(id);
                if (beneficiarioExistente == null)
                    return NotFound(ApiResponse.Fail("Beneficiario no encontrado."));

                if (!PuedoAccederBeneficiario(beneficiarioExistente.ClienteId))
                    return Forbid();

                var beneficiario = await _beneficiarioServicio.ConfirmarBeneficiarioAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "ConfirmacionBeneficiario", $"Beneficiario {beneficiario.Alias} confirmado");

                return Ok(ApiResponse<BeneficiarioConfirmacionDto>.Ok(
                    BeneficiariosReglas.MapearAConfirmacionDto(beneficiario),
                    "Beneficiario confirmado exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirmando beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("mis-beneficiarios")]
        public async Task<IActionResult> ObtenerMisBeneficiarios()
        {
            try
            {
                var clienteId = GetClienteId();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var beneficiarios = await _beneficiarioServicio.ObtenerMisBeneficiariosAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<BeneficiarioListaDto>>.Ok(
                    BeneficiariosReglas.MapearAListaDto(beneficiarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiarios del cliente");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerBeneficiariosCliente(int clienteId)
        {
            try
            {
                var beneficiarios = await _beneficiarioServicio.ObtenerMisBeneficiariosAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<BeneficiarioListaDto>>.Ok(
                    BeneficiariosReglas.MapearAListaDto(beneficiarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiarios del cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarBeneficiario(int id, [FromBody] ActualizarBeneficiarioRequest request)
        {
            try
            {
                var beneficiarioExistente = await _beneficiarioServicio.ObtenerBeneficiarioAsync(id);
                if (beneficiarioExistente == null)
                    return NotFound(ApiResponse.Fail("Beneficiario no encontrado."));

                if (!PuedoAccederBeneficiario(beneficiarioExistente.ClienteId))
                    return Forbid();

                var beneficiario = await _beneficiarioServicio.ActualizarBeneficiarioAsync(id, request.NuevoAlias);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "ActualizacionBeneficiario", $"Beneficiario {id} actualizado a alias: {request.NuevoAlias}");

                return Ok(ApiResponse<BeneficiarioActualizacionDto>.Ok(
                    BeneficiariosReglas.MapearAActualizacionDto(beneficiario),
                    "Beneficiario actualizado."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarBeneficiario(int id)
        {
            try
            {
                var beneficiarioExistente = await _beneficiarioServicio.ObtenerBeneficiarioAsync(id);
                if (beneficiarioExistente == null)
                    return NotFound(ApiResponse.Fail("Beneficiario no encontrado."));

                if (!PuedoAccederBeneficiario(beneficiarioExistente.ClienteId))
                    return Forbid();

                await _beneficiarioServicio.EliminarBeneficiarioAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "EliminacionBeneficiario", $"Beneficiario {beneficiarioExistente.Alias} eliminado");

                return Ok(ApiResponse.Ok("Beneficiario eliminado."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #region Métodos Privados

        private int GetClienteId()
        {
            var claim = User.FindFirst("client_id")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private int GetUsuarioId()
        {
            var claim = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private string GetUserRole() =>
            User.FindFirst("role")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
            ?? "Cliente";

        private bool PuedoAccederBeneficiario(int beneficiarioClienteId)
        {
            var role = GetUserRole();
            if (role is "Administrador" or "Gestor")
                return true;

            var clienteId = GetClienteId();
            return clienteId > 0 && clienteId == beneficiarioClienteId;
        }

        #endregion
    }
}
