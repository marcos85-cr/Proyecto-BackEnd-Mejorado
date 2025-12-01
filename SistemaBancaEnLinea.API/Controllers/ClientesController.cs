using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using static SistemaBancaEnLinea.BC.ReglasDeNegocio.ConstantesGenerales;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServicio _clienteServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            ILogger<ClientesController> logger)
        {
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los clientes (solo admin/gestor)
        /// Restricción: Gestor solo ve sus clientes asignados
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerTodos()
        {
            try
            {
                var role = GetUserRole();
                List<BC.Modelos.Cliente> clientes;

                if (role == "Gestor")
                {
                    // Gestor solo ve sus clientes asignados
                    var gestorId = GetUsuarioIdFromToken();
                    clientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                }
                else
                {
                    clientes = await _clienteServicio.ObtenerTodosAsync();
                }

                var clientesDto = clientes.Select(ClientesReglas.MapearAListaDto).ToList();
                
                return Ok(ApiResponse<object>.Ok(clientesDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lista de clientes");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Obtener cliente por ID
        /// Restricción: Gestor solo puede ver clientes de su cartera
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));

                // Validar acceso para Gestor
                var role = GetUserRole();
                if (role == "Gestor")
                {
                    var gestorId = GetUsuarioIdFromToken();
                    if (cliente.GestorAsignadoId != gestorId)
                        return StatusCode(403, ApiResponse<object>.Fail("No puede acceder a clientes fuera de su cartera."));
                }

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(id);
                var clienteDto = ClientesReglas.MapearADetalleDto(cliente, cuentas);
                
                return Ok(ApiResponse<object>.Ok(clienteDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Obtener mi perfil de cliente
        /// </summary>
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Cliente no identificado."));

                return await ObtenerPorId(clienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mi perfil");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Crear nuevo cliente con usuario y gestor asociado
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> Crear([FromBody] ClienteRequest request)
        {
            try
            {
                var resultado = await _clienteServicio.CrearClienteAsync(request);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                var clienteDto = ClientesReglas.MapearACreacionDto(resultado.Datos!);
                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = resultado.Datos!.Id }, 
                    ApiResponse<object>.Ok(clienteDto, "Cliente creado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando cliente");
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Actualizar cliente existente
        /// Restricción: Gestor solo puede actualizar clientes de su cartera
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ClienteActualizarRequest request)
        {
            try
            {
                // Validar acceso para Gestor
                var role = GetUserRole();
                if (role == "Gestor")
                {
                    var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                    if (cliente == null)
                        return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
                    
                    var gestorId = GetUsuarioIdFromToken();
                    if (cliente.GestorAsignadoId != gestorId)
                        return StatusCode(403, ApiResponse<object>.Fail("No puede modificar clientes fuera de su cartera."));
                    
                    // Gestor no puede cambiar el gestor asignado
                    request = request with { GestorId = null };
                }

                var resultado = await _clienteServicio.ActualizarClienteAsync(id, request);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                var clienteDto = ClientesReglas.MapearAActualizacionDto(resultado.Datos!);
                return Ok(ApiResponse<object>.Ok(clienteDto, "Cliente actualizado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Actualizar mi perfil
        /// </summary>
        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ClienteActualizarRequest request)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Cliente no identificado."));

                // El cliente no puede cambiar su propio gestor
                request = request with { GestorId = null };
                
                return await Actualizar(clienteId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando mi perfil");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Eliminar cliente
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var resultado = await _clienteServicio.EliminarClienteAsync(id);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(null!, "Cliente eliminado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Vincular usuario a cliente
        /// </summary>
        [HttpPut("{id}/vincular-usuario/{usuarioId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> VincularUsuario(int id, int usuarioId)
        {
            try
            {
                var resultado = await _clienteServicio.VincularUsuarioAsync(id, usuarioId);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(null!, "Usuario vinculado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error vinculando usuario {UsuarioId} a cliente {ClienteId}", usuarioId, id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Desvincular usuario del cliente
        /// </summary>
        [HttpDelete("{id}/desvincular-usuario")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DesvincularUsuario(int id)
        {
            try
            {
                var resultado = await _clienteServicio.DesvincularUsuarioAsync(id);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(null!, "Usuario desvinculado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desvinculando usuario del cliente {ClienteId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Asignar gestor a cliente
        /// </summary>
        [HttpPut("{id}/asignar-gestor/{gestorId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AsignarGestor(int id, int gestorId)
        {
            try
            {
                var resultado = await _clienteServicio.AsignarClienteAGestorAsync(id, gestorId);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(null!, "Gestor asignado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando gestor al cliente {ClienteId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Desasignar gestor del cliente
        /// </summary>
        [HttpDelete("{id}/desasignar-gestor")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DesasignarGestor(int id)
        {
            try
            {
                var resultado = await _clienteServicio.DesasignarClienteDeGestorAsync(id);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(null!, "Gestor desasignado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desasignando gestor del cliente {ClienteId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Obtener resumen de cuentas del cliente
        /// </summary>
        [HttpGet("{id}/resumen")]
        public async Task<IActionResult> ObtenerResumen(int id)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(id);
                var resumenDto = MapearResumen(cliente, cuentas);
                
                return Ok(ApiResponse<object>.Ok(resumenDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen del cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        private static object MapearResumen(BC.Modelos.Cliente cliente, List<BC.Modelos.Cuenta> cuentas)
        {
            return new
            {
                cliente = new
                {
                    id = cliente.Id,
                    nombre = cliente.UsuarioAsociado?.Nombre ?? "N/A",
                    identificacion = cliente.UsuarioAsociado?.Identificacion ?? "N/A"
                },
                resumenCuentas = new
                {
                    totalCuentas = cuentas.Count,
                    cuentasActivas = cuentas.Count(c => c.Estado == ESTADO_CUENTA_ACTIVA),
                    cuentasBloqueadas = cuentas.Count(c => c.Estado == ESTADO_CUENTA_BLOQUEADA),
                    saldoTotalCRC = cuentas.Where(c => c.Moneda == MONEDA_COLONES && c.Estado == ESTADO_CUENTA_ACTIVA).Sum(c => c.Saldo),
                    saldoTotalUSD = cuentas.Where(c => c.Moneda == MONEDA_DOLARES && c.Estado == ESTADO_CUENTA_ACTIVA).Sum(c => c.Saldo)
                },
                cuentas = cuentas.Select(c => new
                {
                    id = c.Id,
                    numero = c.Numero,
                    tipo = c.Tipo,
                    moneda = c.Moneda,
                    saldo = c.Saldo,
                    estado = c.Estado
                })
            };
        }

        private int GetClienteIdFromToken()
        {
            var clienteIdClaim = User.FindFirst("client_id")?.Value;
            return int.TryParse(clienteIdClaim, out var clienteId) ? clienteId : 0;
        }

        private int GetUsuarioIdFromToken()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst("role")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? "Cliente";
        }
    }
}