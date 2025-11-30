using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class AdminController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly IProveedorServicioServicio _proveedorServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUsuarioServicio usuarioServicio,
            IProveedorServicioServicio proveedorServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<AdminController> logger)
        {
            _usuarioServicio = usuarioServicio;
            _proveedorServicio = proveedorServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-A2: Desbloquear usuario (solo administradores)
        /// </summary>
        [HttpPut("usuarios/{usuarioId}/desbloquear")]
        public async Task<IActionResult> DesbloquearUsuario(int usuarioId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var resultado = await _usuarioServicio.ToggleBloqueoUsuarioAsync(usuarioId, adminId);
                
                if (!resultado.Exitoso)
                    return BadRequest(new { success = false, message = resultado.Error });

                return Ok(new { success = true, message = "Usuario desbloqueado exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error desbloqueando usuario: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        /// <summary>
        /// Obtener todos los usuarios
        /// </summary>
        [HttpGet("usuarios")]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            try
            {
                var usuarios = await _usuarioServicio.ObtenerTodosAsync();
                return Ok(new
                {
                    success = true,
                    data = usuarios?.Select(u => new
                    {
                        id = u.Id,
                        email = u.Email,
                        role = u.Rol,
                        nombre = u.Nombre ?? u.Email,
                        identificacion = u.Identificacion,
                        telefono = u.Telefono,
                        bloqueado = u.EstaBloqueado,
                        intentosFallidos = u.IntentosFallidos
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-E1: Crear proveedor de servicio (solo administradores)
        /// </summary>
        [HttpPost("proveedores")]
        public async Task<IActionResult> CrearProveedor([FromBody] CrearProveedorRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var proveedor = new SistemaBancaEnLinea.BC.Modelos.ProveedorServicio
                {
                    Nombre = request.Nombre,
                    ReglaValidacionContrato = request.ReglaValidacionContrato,
                    CreadoPorUsuarioId = adminId
                };

                var proveedorCreado = await _proveedorServicio.CrearAsync(proveedor);

                return CreatedAtAction(nameof(ObtenerProveedor), new { id = proveedorCreado.Id }, new
                {
                    success = true,
                    message = "Proveedor creado exitosamente.",
                    data = new
                    {
                        id = proveedorCreado.Id,
                        nombre = proveedorCreado.Nombre,
                        reglaValidacion = proveedorCreado.ReglaValidacionContrato
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener proveedor por ID
        /// </summary>
        [HttpGet("proveedores/{id}")]
        public async Task<IActionResult> ObtenerProveedor(int id)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = proveedor.Id,
                        nombre = proveedor.Nombre,
                        reglaValidacion = proveedor.ReglaValidacionContrato
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener todos los proveedores
        /// </summary>
        [HttpGet("proveedores")]
        public async Task<IActionResult> ObtenerProveedores()
        {
            try
            {
                var proveedores = await _proveedorServicio.ObtenerTodosAsync();
                return Ok(new
                {
                    success = true,
                    data = proveedores.Select(p => new
                    {
                        id = p.Id,
                        nombre = p.Nombre,
                        reglaValidacion = p.ReglaValidacionContrato
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar proveedor
        /// </summary>
        [HttpDelete("proveedores/{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            try
            {
                var resultado = await _proveedorServicio.EliminarAsync(id);
                if (!resultado)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                return Ok(new { success = true, message = "Proveedor eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-G2: Obtener registros de auditoría
        /// Restricción: No puede ver auditoría de otros administradores
        /// </summary>
        [HttpGet("auditoria")]
        public async Task<IActionResult> ObtenerAuditoria(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] string? tipoOperacion)
        {
            try
            {
                var inicio = fechaInicio ?? DateTime.UtcNow.AddDays(-30);
                var fin = fechaFin ?? DateTime.UtcNow;
                var currentAdminId = GetCurrentUserId();

                var registros = await _auditoriaServicio.ObtenerPorFechasAsync(inicio, fin, tipoOperacion);
                
                // Obtener IDs de otros administradores
                var otrosAdmins = await _usuarioServicio.ObtenerPorRolAsync("Administrador");
                var otrosAdminIds = otrosAdmins
                    .Where(a => a.Id != currentAdminId)
                    .Select(a => a.Id)
                    .ToHashSet();
                
                // Filtrar registros de otros administradores (solo ve los propios y los de no-admins)
                registros = registros
                    .Where(r => !otrosAdminIds.Contains(r.UsuarioId))
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = registros.Select(r => new
                    {
                        id = r.Id,
                        fechaHora = r.FechaHora,
                        tipoOperacion = r.TipoOperacion,
                        descripcion = r.Descripcion,
                        usuarioId = r.UsuarioId,
                        usuarioEmail = r.Usuario?.Email,
                        detalleJson = r.DetalleJson
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener registros de auditoría por usuario
        /// Restricción: No puede ver auditoría de otros administradores
        /// </summary>
        [HttpGet("auditoria/usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerAuditoriaUsuario(int usuarioId)
        {
            try
            {
                var currentAdminId = GetCurrentUserId();
                
                // Verificar si el usuario solicitado es otro administrador
                var usuarios = await _usuarioServicio.ObtenerPorRolAsync("Administrador");
                var esOtroAdmin = usuarios.Any(u => u.Id == usuarioId && u.Id != currentAdminId);
                
                if (esOtroAdmin)
                    return StatusCode(403, new { success = false, message = "No puede acceder a reportes de otros administradores." });

                var registros = await _auditoriaServicio.ObtenerPorUsuarioAsync(usuarioId);

                return Ok(new
                {
                    success = true,
                    data = registros.Select(r => new
                    {
                        id = r.Id,
                        fechaHora = r.FechaHora,
                        tipoOperacion = r.TipoOperacion,
                        descripcion = r.Descripcion
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class CrearProveedorRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string ReglaValidacionContrato { get; set; } = string.Empty;
        public int UsuarioAdminId { get; set; }
    }
}