using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUsuarioServicio usuarioServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<UsersController> logger)
        {
            _usuarioServicio = usuarioServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/users
        /// Obtiene todos los usuarios (Admin/Gestor)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var usuarios = await _usuarioServicio.ObtenerTodosAsync();

                return Ok(new
                {
                    success = true,
                    data = usuarios.Select(u => new
                    {
                        id = u.Id.ToString(),
                        email = u.Email,
                        role = u.Rol,
                        nombre = u.Nombre ?? u.Email,
                        identificacion = u.Identificacion,
                        telefono = u.Telefono,
                        bloqueado = u.EstaBloqueado,
                        intentosFallidos = u.IntentosFallidos,
                        fechaCreacion = u.FechaCreacion,
                        cuentasActivas = 0 // TODO: Calcular desde cliente asociado
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo usuarios: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/users/{id}
        /// Obtiene un usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var usuario = await _usuarioServicio.ObtenerPorIdAsync(id);
                if (usuario == null)
                    return NotFound(new { success = false, message = "Usuario no encontrado." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = usuario.Id.ToString(),
                        email = usuario.Email,
                        role = usuario.Rol,
                        nombre = usuario.Nombre ?? usuario.Email,
                        identificacion = usuario.Identificacion,
                        telefono = usuario.Telefono,
                        bloqueado = usuario.EstaBloqueado,
                        intentosFallidos = usuario.IntentosFallidos,
                        fechaCreacion = usuario.FechaCreacion,
                        fechaBloqueo = usuario.FechaBloqueo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo usuario {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/users
        /// Crea un nuevo usuario (Solo Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos" });

                var usuario = await _usuarioServicio.RegistrarUsuarioAsync(
                    request.Email,
                    request.Password,
                    request.Role
                );

                // Actualizar información adicional
                usuario.Nombre = request.Nombre;
                usuario.Identificacion = request.Identificacion;
                usuario.Telefono = request.Telefono;
                await _usuarioServicio.ActualizarUsuarioAsync(usuario);

                var adminId = GetCurrentUserId();
                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    "CreacionUsuario",
                    $"Usuario {request.Email} creado por administrador"
                );

                return CreatedAtAction(nameof(GetUser), new { id = usuario.Id }, new
                {
                    success = true,
                    message = "Usuario creado exitosamente",
                    data = new
                    {
                        id = usuario.Id.ToString(),
                        email = usuario.Email,
                        role = usuario.Rol,
                        nombre = usuario.Nombre
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando usuario: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/users/{id}
        /// Actualiza un usuario
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var usuario = await _usuarioServicio.ObtenerPorIdAsync(id);
                if (usuario == null)
                    return NotFound(new { success = false, message = "Usuario no encontrado." });

                // Actualizar campos permitidos
                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    usuario.Nombre = request.Nombre;

                if (!string.IsNullOrWhiteSpace(request.Telefono))
                    usuario.Telefono = request.Telefono;

                if (!string.IsNullOrWhiteSpace(request.Identificacion))
                    usuario.Identificacion = request.Identificacion;

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != usuario.Email)
                {
                    if (await _usuarioServicio.ExisteEmailAsync(request.Email))
                        return BadRequest(new { success = false, message = "El email ya está registrado." });

                    usuario.Email = request.Email;
                }

                // Actualizar rol si se especifica
                if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != usuario.Rol)
                {
                    usuario.Rol = request.Role;
                }

                var usuarioActualizado = await _usuarioServicio.ActualizarUsuarioAsync(usuario);

                var adminId = GetCurrentUserId();
                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    "ActualizacionUsuario",
                    $"Usuario {usuario.Email} actualizado"
                );

                return Ok(new
                {
                    success = true,
                    message = "Usuario actualizado exitosamente",
                    data = new
                    {
                        id = usuarioActualizado.Id.ToString(),
                        email = usuarioActualizado.Email,
                        role = usuarioActualizado.Rol,
                        nombre = usuarioActualizado.Nombre,
                        identificacion = usuarioActualizado.Identificacion,
                        telefono = usuarioActualizado.Telefono
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error actualizando usuario {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/users/{id}/block
        /// Bloquea o desbloquea un usuario
        /// </summary>
        [HttpPut("{id}/block")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ToggleBlockUser(int id)
        {
            try
            {
                var usuario = await _usuarioServicio.ObtenerPorIdAsync(id);
                if (usuario == null)
                    return NotFound(new { success = false, message = "Usuario no encontrado." });

                bool nuevoEstado = !usuario.EstaBloqueado;

                if (nuevoEstado)
                {
                    // Bloquear
                    usuario.EstaBloqueado = true;
                    usuario.FechaBloqueo = DateTime.UtcNow;
                }
                else
                {
                    // Desbloquear
                    await _usuarioServicio.DesbloquearUsuarioAsync(id);
                    usuario = await _usuarioServicio.ObtenerPorIdAsync(id);
                }

                var adminId = GetCurrentUserId();
                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    nuevoEstado ? "BloqueoUsuario" : "DesbloqueoUsuario",
                    $"Usuario {usuario!.Email} {(nuevoEstado ? "bloqueado" : "desbloqueado")}"
                );

                return Ok(new
                {
                    success = true,
                    message = $"Usuario {(nuevoEstado ? "bloqueado" : "desbloqueado")} exitosamente",
                    data = new
                    {
                        id = usuario.Id.ToString(),
                        bloqueado = usuario.EstaBloqueado,
                        fechaBloqueo = usuario.FechaBloqueo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error bloqueando/desbloqueando usuario {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/users/check-email/{email}
        /// Verifica disponibilidad de email
        /// </summary>
        [HttpGet("check-email/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            try
            {
                var existe = await _usuarioServicio.ExisteEmailAsync(email);
                return Ok(new
                {
                    success = true,
                    available = !existe
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verificando email: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    // DTOs
    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Role { get; set; } = "Cliente";
    }

    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? Nombre { get; set; }
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}