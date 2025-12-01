using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProgramacionController : ControllerBase
    {
        private readonly IProgramacionServicio _programacionServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly IMapper _mapper;
        private readonly ILogger<ProgramacionController> _logger;

        public ProgramacionController(
            IProgramacionServicio programacionServicio,
            IClienteServicio clienteServicio,
            IMapper mapper,
            ILogger<ProgramacionController> logger)
        {
            _programacionServicio = programacionServicio;
            _clienteServicio = clienteServicio;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("mis-programaciones")]
        public async Task<IActionResult> ObtenerMisProgramaciones()
        {
            try
            {
                var clienteId = await GetClienteIdAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado."));

                var programaciones = await _programacionServicio.ObtenerProgramacionesClienteAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<ProgramacionListaDto>>.Ok(
                    _mapper.Map<IEnumerable<ProgramacionListaDto>>(programaciones)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo programaciones");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerProgramacionesCliente(int clienteId)
        {
            try
            {
                var programaciones = await _programacionServicio.ObtenerProgramacionesClienteAsync(clienteId);

                return Ok(ApiResponse<IEnumerable<ProgramacionResumenDto>>.Ok(
                    _mapper.Map<IEnumerable<ProgramacionResumenDto>>(programaciones)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo programaciones del cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{programacionId}")]
        public async Task<IActionResult> ObtenerProgramacion(int programacionId)
        {
            try
            {
                var programacion = await _programacionServicio.ObtenerProgramacionAsync(programacionId);
                if (programacion == null)
                    return NotFound(ApiResponse.Fail("Programación no encontrada."));

                var clienteId = await GetClienteIdAsync();
                var role = GetUserRole();

                // Validar acceso
                if (role == "Cliente" && programacion.Transaccion?.ClienteId != clienteId)
                    return Forbid();

                return Ok(ApiResponse<ProgramacionDetalleDto>.Ok(
                    _mapper.Map<ProgramacionDetalleDto>(programacion)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo programación {Id}", programacionId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{programacionId}/cancelar")]
        public async Task<IActionResult> CancelarProgramacion(int programacionId)
        {
            try
            {
                var programacion = await _programacionServicio.ObtenerProgramacionAsync(programacionId);
                if (programacion == null)
                    return NotFound(ApiResponse.Fail("Programación no encontrada."));

                var clienteId = await GetClienteIdAsync();
                var role = GetUserRole();

                if (role == "Cliente" && programacion.Transaccion?.ClienteId != clienteId)
                    return Forbid();

                await _programacionServicio.CancelarProgramacionAsync(programacionId, clienteId);

                return Ok(ApiResponse.Ok("Programación cancelada exitosamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando programación {Id}", programacionId);
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

        private string GetUserRole() =>
            User.FindFirst("role")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
            ?? "Cliente";

        #endregion
    }
}
