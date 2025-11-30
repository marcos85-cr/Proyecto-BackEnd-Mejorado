using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var usuarios = await _usuarioServicio.ObtenerTodosAsync();
                return Ok(ApiResponse<IEnumerable<UsuarioListaDto>>.Ok(
                    UsuarioReglas.MapearAListaDto(usuarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var usuario = await _usuarioServicio.ObtenerPorIdAsync(id);
                if (usuario == null)
                    return NotFound(ApiResponse.Fail("Usuario no encontrado."));

                return Ok(ApiResponse<UsuarioDetalleDto>.Ok(
                    UsuarioReglas.MapearADetalleDto(usuario)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateUser([FromBody] UsuarioRequest request)
        {
            try
            {
                var resultado = await _usuarioServicio.CrearUsuarioAsync(request);

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "CreacionUsuario", $"Usuario {request.Email} creado");

                return CreatedAtAction(nameof(GetUser), new { id = resultado.Datos!.Id },
                    ApiResponse<UsuarioCreacionDto>.Ok(
                        UsuarioReglas.MapearACreacionDto(resultado.Datos),
                        "Usuario creado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UsuarioRequest request)
        {
            try
            {
                var resultado = await _usuarioServicio.ActualizarUsuarioAsync(id, request);

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "ActualizacionUsuario", $"Usuario {resultado.Datos!.Email} actualizado");

                return Ok(ApiResponse<UsuarioActualizacionDto>.Ok(
                    UsuarioReglas.MapearAActualizacionDto(resultado.Datos),
                    "Usuario actualizado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        /// <summary>
        /// PUT: api/users/{id}/block - Bloquea/desbloquea usuario
        /// </summary>
        [HttpPut("{id}/block")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ToggleBlockUser(int id)
        {
            try
            {
                var resultado = await _usuarioServicio.ToggleBloqueoUsuarioAsync(id, GetCurrentUserId());

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                var accion = resultado.Datos!.EstaBloqueado ? "bloqueado" : "desbloqueado";

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(),
                    resultado.Datos.EstaBloqueado ? "BloqueoUsuario" : "DesbloqueoUsuario",
                    $"Usuario {resultado.Datos.Email} {accion}");

                return Ok(ApiResponse<UsuarioBloqueoDto>.Ok(
                    UsuarioReglas.MapearABloqueoDto(resultado.Datos),
                    $"Usuario {accion} exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bloqueando/desbloqueando usuario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        /// <summary>
        /// GET: api/users/check-email/{email} - Verifica disponibilidad de email
        /// </summary>
        [HttpGet("check-email/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            try
            {
                var existe = await _usuarioServicio.ExisteEmailAsync(email);
                return Ok(ApiResponse<EmailDisponibilidadDto>.Ok(
                    UsuarioReglas.CrearEmailDisponibilidadDto(existe)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando email");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        /// <summary>
        /// PUT: api/users/{id}/change-password - Cambia contraseña
        /// </summary>
        [HttpPut("{id}/change-password")]
        //[Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] CambioContrasenaRequest request)
        {
            try
            {
                var resultado = await _usuarioServicio.CambiarContrasenaAsync(
                    id,
                    GetCurrentUserId(),
                    GetCurrentUserRole(),
                    request.ContrasenaActual,
                    request.NuevaContrasena);

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                var usuario = await _usuarioServicio.ObtenerPorIdAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "CambioContrasena", $"Contraseña cambiada para usuario {usuario?.Email}");

                return Ok(ApiResponse<CambioContrasenaDto>.Ok(
                    UsuarioReglas.MapearACambioContrasenaDto(usuario!),
                    "Contraseña actualizada exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña para usuario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        /// <summary>
        /// DELETE: api/users/{id} - Elimina un usuario
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var resultado = await _usuarioServicio.EliminarUsuarioAsync(id, GetCurrentUserId());

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                await _auditoriaServicio.RegistrarAsync(
                    GetCurrentUserId(), "EliminacionUsuario", $"Usuario {id} eliminado");

                return Ok(ApiResponse.Ok("Usuario eliminado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #region Métodos Privados - Extracción de Claims

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value ??
                        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole() =>
            User.FindFirst("role")?.Value ?? "";

        #endregion
    }
}
