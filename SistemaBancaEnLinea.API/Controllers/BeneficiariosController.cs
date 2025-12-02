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
    public class BeneficiariosController : ControllerBase
    {
        private readonly IBeneficiarioServicio _beneficiarioServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly IMapper _mapper;
        private readonly ILogger<BeneficiariosController> _logger;

        public BeneficiariosController(
            IBeneficiarioServicio beneficiarioServicio,
            IClienteServicio clienteServicio,
            IAuditoriaServicio auditoriaServicio,
            IMapper mapper,
            ILogger<BeneficiariosController> logger)
        {
            _beneficiarioServicio = beneficiarioServicio;
            _clienteServicio = clienteServicio;
            _auditoriaServicio = auditoriaServicio;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearBeneficiario([FromBody] CrearBeneficiarioRequest request)
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var beneficiario = _mapper.Map<Beneficiario>(request);
                beneficiario.ClienteId = clienteId;
                
                var beneficiarioCreado = await _beneficiarioServicio.CrearBeneficiarioAsync(beneficiario);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "CreacionBeneficiario", $"Beneficiario {request.Alias} creado");

                return CreatedAtAction(nameof(ObtenerBeneficiario), new { id = beneficiarioCreado.Id },
                    ApiResponse<BeneficiarioCreacionDto>.Ok(
                        _mapper.Map<BeneficiarioCreacionDto>(beneficiarioCreado),
                        "Beneficiario creado. Debe confirmarlo antes de poder usarlo."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando beneficiario");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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

                if (!await PuedoAccederBeneficiarioAsync(beneficiario.ClienteId))
                    return Forbid();

                var tieneOperaciones = await _beneficiarioServicio.TieneOperacionesPendientesAsync(id);
                var dto = _mapper.Map<BeneficiarioDetalleDto>(beneficiario);
                dto = dto with { TieneOperacionesPendientes = tieneOperaciones };

                return Ok(ApiResponse<BeneficiarioDetalleDto>.Ok(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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

                if (!await PuedoAccederBeneficiarioAsync(beneficiarioExistente.ClienteId))
                    return Forbid();

                var beneficiario = await _beneficiarioServicio.ConfirmarBeneficiarioAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "ConfirmacionBeneficiario", $"Beneficiario {beneficiario.Alias} confirmado");

                return Ok(ApiResponse<BeneficiarioConfirmacionDto>.Ok(
                    _mapper.Map<BeneficiarioConfirmacionDto>(beneficiario),
                    "Beneficiario confirmado exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirmando beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("mis-beneficiarios")]
        public async Task<IActionResult> ObtenerMisBeneficiarios()
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var beneficiarios = await _beneficiarioServicio.ObtenerMisBeneficiariosAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<BeneficiarioListaDto>>.Ok(
                    _mapper.Map<IEnumerable<BeneficiarioListaDto>>(beneficiarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiarios del cliente");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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
                    _mapper.Map<IEnumerable<BeneficiarioListaDto>>(beneficiarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo beneficiarios del cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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

                if (!await PuedoAccederBeneficiarioAsync(beneficiarioExistente.ClienteId))
                    return Forbid();

                var beneficiario = await _beneficiarioServicio.ActualizarBeneficiarioAsync(id, request.NuevoAlias);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "ActualizacionBeneficiario", $"Beneficiario {id} actualizado a alias: {request.NuevoAlias}");

                return Ok(ApiResponse<BeneficiarioActualizacionDto>.Ok(
                    _mapper.Map<BeneficiarioActualizacionDto>(beneficiario),
                    "Beneficiario actualizado."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando beneficiario {Id}", id);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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

                if (!await PuedoAccederBeneficiarioAsync(beneficiarioExistente.ClienteId))
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
                return StatusCode(500, ApiResponse.Fail(ex.Message));
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

        private string GetUserRole() =>
            User.FindFirst("role")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
            ?? "Cliente";

        private async Task<bool> PuedoAccederBeneficiarioAsync(int beneficiarioClienteId)
        {
            var role = GetUserRole();
            if (role is "Administrador" or "Gestor")
                return true;

            var clienteId = await GetClienteIdAsync();
            return clienteId > 0 && clienteId == beneficiarioClienteId;
        }

        #endregion
    }
}
