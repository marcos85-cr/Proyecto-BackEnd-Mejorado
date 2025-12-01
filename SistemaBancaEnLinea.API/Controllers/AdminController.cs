using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class AdminController : ControllerBase
    {
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly IClienteServicio _clienteServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ITransferenciasServicio _transferenciasServicio;
        private readonly IProveedorServicioServicio _proveedorServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUsuarioServicio usuarioServicio,
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            ITransferenciasServicio transferenciasServicio,
            IProveedorServicioServicio proveedorServicio,
            IAuditoriaServicio auditoriaServicio,
            IMapper mapper,
            ILogger<AdminController> logger)
        {
            _usuarioServicio = usuarioServicio;
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _transferenciasServicio = transferenciasServicio;
            _proveedorServicio = proveedorServicio;
            _auditoriaServicio = auditoriaServicio;
            _mapper = mapper;
            _logger = logger;
        }

        #region Dashboard

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var usuarios = await _usuarioServicio.ObtenerTodosAsync();
                var totalUsuarios = usuarios.Count;
                var usuariosActivos = usuarios.Count(u => !u.EstaBloqueado);
                var usuariosBloqueados = usuarios.Count(u => u.EstaBloqueado);

                var clientes = await _clienteServicio.ObtenerTodosAsync();
                var totalClientes = clientes.Count;
                var clienteIds = clientes.Select(c => c.Id).ToList();

                var cuentas = await _cuentaServicio.ObtenerTodasConRelacionesAsync();
                var totalCuentas = cuentas.Count;
                var cuentasActivas = cuentas.Count(c => c.Estado == "Activa");
                var volumenTotal = cuentas.Where(c => c.Estado == "Activa").Sum(c => c.Saldo);

                var proveedores = await _proveedorServicio.ObtenerTodosAsync();
                var totalProveedores = proveedores.Count;

                var operacionesHoy = clienteIds.Count > 0 
                    ? await _transferenciasServicio.ObtenerOperacionesDeHoyPorClientesAsync(clienteIds)
                    : new List<BC.Modelos.Transaccion>();

                return Ok(ApiResponse<AdminDashboardDto>.Ok(new AdminDashboardDto(
                    totalUsuarios,
                    usuariosActivos,
                    usuariosBloqueados,
                    totalClientes,
                    totalCuentas,
                    cuentasActivas,
                    totalProveedores,
                    operacionesHoy.Count,
                    volumenTotal)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo stats del dashboard admin");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #endregion

        #region Usuarios

        [HttpPut("usuarios/{usuarioId}/desbloquear")]
        public async Task<IActionResult> DesbloquearUsuario(int usuarioId)
        {
            try
            {
                var resultado = await _usuarioServicio.ToggleBloqueoUsuarioAsync(usuarioId, GetCurrentUserId());

                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse.Fail(resultado.Error!));

                return Ok(ApiResponse.Ok("Usuario desbloqueado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desbloqueando usuario {Id}", usuarioId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            try
            {
                var usuarios = await _usuarioServicio.ObtenerTodosAsync();
                return Ok(ApiResponse<IEnumerable<UsuarioListaDto>>.Ok(
                    _mapper.Map<IEnumerable<UsuarioListaDto>>(usuarios)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost("proveedores")]
        public async Task<IActionResult> CrearProveedor([FromBody] CrearProveedorRequest request)
        {
            try
            {
                var proveedor = new BC.Modelos.ProveedorServicio
                {
                    Nombre = request.Nombre,
                    ReglaValidacionContrato = request.ReglaValidacionContrato,
                    FormatoContrato = request.FormatoContrato,
                    CreadoPorUsuarioId = GetCurrentUserId()
                };

                var creado = await _proveedorServicio.CrearAsync(proveedor);

                return CreatedAtAction(nameof(ObtenerProveedor), new { id = creado.Id },
                    ApiResponse<ProveedorDto>.Ok(
                        new ProveedorDto(creado.Id, creado.Nombre, creado.ReglaValidacionContrato, creado.FormatoContrato),
                        "Proveedor creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando proveedor");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("proveedores/{id}")]
        public async Task<IActionResult> ObtenerProveedor(int id)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado"));

                return Ok(ApiResponse<ProveedorDto>.Ok(
                    new ProveedorDto(proveedor.Id, proveedor.Nombre, proveedor.ReglaValidacionContrato, proveedor.FormatoContrato)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("proveedores")]
        public async Task<IActionResult> ObtenerProveedores()
        {
            try
            {
                var proveedores = await _proveedorServicio.ObtenerTodosAsync();
                return Ok(ApiResponse<IEnumerable<ProveedorDto>>.Ok(
                    proveedores.Select(p => new ProveedorDto(p.Id, p.Nombre, p.ReglaValidacionContrato, p.FormatoContrato))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proveedores");
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("proveedores/{id}")]
        public async Task<IActionResult> ActualizarProveedor(int id, [FromBody] ActualizarProveedorRequest request)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado"));

                if (request.Nombre != null)
                    proveedor.Nombre = request.Nombre;
                if (request.ReglaValidacion != null)
                    proveedor.ReglaValidacionContrato = request.ReglaValidacion;
                if (request.FormatoContrato != null)
                    proveedor.FormatoContrato = request.FormatoContrato;

                var actualizado = await _proveedorServicio.ActualizarAsync(id, proveedor);

                return Ok(ApiResponse<ProveedorDto>.Ok(
                    new ProveedorDto(actualizado.Id, actualizado.Nombre, actualizado.ReglaValidacionContrato, actualizado.FormatoContrato),
                    "Proveedor actualizado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpDelete("proveedores/{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            try
            {
                var resultado = await _proveedorServicio.EliminarAsync(id);
                if (!resultado)
                    return NotFound(ApiResponse.Fail("Proveedor no encontrado"));

                return Ok(ApiResponse.Ok("Proveedor eliminado exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando proveedor {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

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

                var otrosAdmins = await _usuarioServicio.ObtenerPorRolAsync("Administrador");
                var otrosAdminIds = otrosAdmins
                    .Where(a => a.Id != currentAdminId)
                    .Select(a => a.Id)
                    .ToHashSet();

                var registrosFiltrados = registros
                    .Where(r => !otrosAdminIds.Contains(r.UsuarioId))
                    .Select(r => new AuditoriaDto(
                        r.Id, r.FechaHora, r.TipoOperacion, r.Descripcion,
                        r.UsuarioId, r.Usuario?.Email, r.DetalleJson))
                    .ToList();

                return Ok(ApiResponse<IEnumerable<AuditoriaDto>>.Ok(registrosFiltrados));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo auditoría");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("auditoria/usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerAuditoriaUsuario(int usuarioId)
        {
            try
            {
                var currentAdminId = GetCurrentUserId();

                var admins = await _usuarioServicio.ObtenerPorRolAsync("Administrador");
                if (admins.Any(u => u.Id == usuarioId && u.Id != currentAdminId))
                    return StatusCode(403, ApiResponse.Fail("No puede acceder a reportes de otros administradores"));

                var registros = await _auditoriaServicio.ObtenerPorUsuarioAsync(usuarioId);

                return Ok(ApiResponse<IEnumerable<AuditoriaResumenDto>>.Ok(
                    registros.Select(r => new AuditoriaResumenDto(
                        r.Id, r.FechaHora, r.TipoOperacion, r.Descripcion))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo auditoría de usuario {Id}", usuarioId);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        #endregion

        #region Helpers

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value ??
                        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        #endregion
    }
}
