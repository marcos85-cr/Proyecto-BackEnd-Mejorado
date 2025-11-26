using Microsoft.AspNetCore.Mvc;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;

        public AuthController(IUsuarioServicio usuarioServicio)
        {
            _usuarioServicio = usuarioServicio;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { mensaje = "Email y contraseña son requeridos." });
            }

            var resultado = await _usuarioServicio.IniciarSesionAsync(request.Email, request.Password);

            if (!resultado.Exitoso)
            {
                return Unauthorized(new { mensaje = resultado.Error });
            }

            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                token = resultado.Token
            });
        }

        // POST: api/auth/registro
        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.Rol))
                {
                    return BadRequest(new { mensaje = "Todos los campos son requeridos." });
                }

                var usuario = await _usuarioServicio.RegistrarUsuarioAsync(
                    request.Email,
                    request.Password,
                    request.Rol
                );

                return CreatedAtAction(nameof(Registro), new
                {
                    mensaje = "Usuario registrado exitosamente.",
                    usuarioId = usuario.Id,
                    email = usuario.Email,
                    rol = usuario.Rol
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }

    // DTOs
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegistroRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}