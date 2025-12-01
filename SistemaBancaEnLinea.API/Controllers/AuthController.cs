using Microsoft.AspNetCore.Mvc;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUsuarioServicio usuarioServicio,
            ILogger<AuthController> logger)
        {
            _usuarioServicio = usuarioServicio;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var resultado = await _usuarioServicio.IniciarSesionAsync(request.Email, request.Password);

                if (!resultado.Exitoso)
                    return Unauthorized(ApiResponse.Fail(resultado.Error!));

                var loginDto = UsuarioReglas.MapearALoginDto(resultado.Token!);
                return Ok(ApiResponse<LoginDto>.Ok(loginDto, "Inicio de sesión exitoso"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }
        
        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailRequest request)
        {
            try
            {
                var existe = await _usuarioServicio.ExisteEmailAsync(request.Email);
                return Ok(ApiResponse<EmailDisponibilidadDto>.Ok(
                    UsuarioReglas.CrearEmailDisponibilidadDto(existe)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando email");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }
    }
}
