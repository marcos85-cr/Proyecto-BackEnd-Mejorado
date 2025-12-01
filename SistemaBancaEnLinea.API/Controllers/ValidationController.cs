using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly IClienteServicio _clienteServicio;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IClienteServicio clienteServicio,
            ILogger<ValidationController> logger)
        {
            _clienteServicio = clienteServicio;
            _logger = logger;
        }

        [HttpPost("cedula")]
        [AllowAnonymous]
        public IActionResult ValidarCedula([FromBody] ValidarCedulaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Cedula))
                    return BadRequest(ApiResponse.Fail("Cédula requerida"));

                return Ok(ApiResponse<ValidacionCedulaDto>.Ok(
                    ValidacionCedulaReglas.CrearValidacionDto(request.Cedula)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando cédula");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("check-unique-identification/{identificacion}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUniqueIdentification(string identificacion)
        {
            try
            {
                var existe = await _clienteServicio.ExisteIdentificacionAsync(identificacion);

                return Ok(ApiResponse<IdentificacionDisponibilidadDto>.Ok(
                    ValidacionCedulaReglas.CrearDisponibilidadDto(existe)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando identificación");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }
    }
}
