using Microsoft.AspNetCore.Mvc;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly IClienteServicio _clienteServicio;

        public AuthController(IUsuarioServicio usuarioServicio, IClienteServicio clienteServicio)
        {
            _usuarioServicio = usuarioServicio;
            _clienteServicio = clienteServicio;
        }

        /// <summary>
        /// POST: api/auth/login
        /// Inicia sesión de usuario con email y contraseña
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email y contraseña son requeridos."
                });
            }

            var resultado = await _usuarioServicio.IniciarSesionAsync(request.Email, request.Password);

            if (!resultado.Exitoso)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = resultado.Error
                });
            }

            return Ok(new
            {
                
                success = true,
                message = "Inicio de sesión exitoso.",
                token = resultado.Token,
                user = new
                {
                    id = resultado.Usuario.Id,
                    email = resultado.Usuario.Email,
                    role = resultado.Usuario.Rol,
                    nombre = resultado.Usuario.Nombre ?? "Usuario",
                    identificacion = resultado.Usuario.Identificacion,
                    telefono = resultado.Usuario.Telefono,
                    bloqueado = resultado.Usuario.EstaBloqueado,
                    intentosFallidos = resultado.Usuario.IntentosFallidos
                },
                expiresIn = 1200 // 20 minutos
            });
        }

        /// <summary>
        /// POST: api/auth/registro
        /// Registra un nuevo usuario con sus datos personales
        /// </summary>
        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.Nombre) ||
                    string.IsNullOrWhiteSpace(request.Identificacion) ||
                    string.IsNullOrWhiteSpace(request.Telefono))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Todos los campos son requeridos."
                    });
                }

                // Validar que las contraseñas coincidan
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Las contraseñas no coinciden."
                    });
                }

                // Validar formato de email
                try
                {
                    var addr = new System.Net.Mail.MailAddress(request.Email);
                }
                catch
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El email no es válido."
                    });
                }

                // Validar formato de contraseña
                if (!AutenticacionReglas.ValidarFormatoPassword(request.Password))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La contraseña debe tener: mínimo 8 caracteres, 1 mayúscula, 1 número y 1 símbolo."
                    });
                }

                // Validar rol
                if (!string.IsNullOrWhiteSpace(request.Rol) && !AutenticacionReglas.ValidarRol(request.Rol))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Rol no válido. Use: Administrador, Gestor o Cliente."
                    });
                }

                // Registrar usuario
                var usuario = await _usuarioServicio.RegistrarUsuarioAsync(
                    request.Email,
                    request.Password,
                    request.Rol ?? "Cliente"
                );

                // Crear cliente asociado (solo si es cliente)
                if (usuario.Rol == "Cliente")
                {
                    await _clienteServicio.CrearClienteAsync(new SistemaBancaEnLinea.BC.Modelos.Cliente
                    {
                        Identificacion = request.Identificacion,
                        NombreCompleto = request.Nombre,
                        Telefono = request.Telefono,
                        Correo = request.Email,
                        UsuarioAsociado = usuario
                    });
                }

                return CreatedAtAction(nameof(Registro), new
                {
                    success = true,
                    message = "Usuario registrado exitosamente.",
                    user = new
                    {
                        id = usuario.Id,
                        email = usuario.Email,
                        role = usuario.Rol,
                        nombre = request.Nombre,
                        identificacion = request.Identificacion,
                        telefono = request.Telefono
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al registrar usuario: " + ex.Message
                });
            }
        }

        /// <summary>
        /// POST: api/auth/check-email
        /// Verifica si un email ya está registrado
        /// </summary>
        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { available = false });
            }

            var existe = await _usuarioServicio.ExisteEmailAsync(request.Email);
            return Ok(new { available = !existe });
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
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Rol { get; set; }
    }

    public class CheckEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}