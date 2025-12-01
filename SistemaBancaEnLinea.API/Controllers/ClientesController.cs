using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
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
        private readonly IMapper _mapper;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            IMapper mapper,
            ILogger<ClientesController> logger)
        {
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _mapper = mapper;
            _logger = logger;
        }

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
                    var gestorId = GetUsuarioIdFromToken();
                    clientes = await _clienteServicio.ObtenerClientesPorGestorAsync(gestorId);
                }
                else
                {
                    clientes = await _clienteServicio.ObtenerTodosAsync();
                }

                return Ok(ApiResponse<object>.Ok(
                    _mapper.Map<IEnumerable<ClienteListaDto>>(clientes)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lista de clientes");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));

                var role = GetUserRole();
                if (role == "Gestor")
                {
                    var gestorId = GetUsuarioIdFromToken();
                    if (cliente.GestorAsignadoId != gestorId)
                        return StatusCode(403, ApiResponse<object>.Fail("No puede acceder a clientes fuera de su cartera."));
                }

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(id);
                
                // ClienteDetalleDto requiere datos calculados, mapeo manual
                var usuario = cliente.UsuarioAsociado;
                var cuentasActivas = cuentas.Count(c => c.Estado == ESTADO_CUENTA_ACTIVA);
                var saldoTotal = cuentas.Where(c => c.Estado == ESTADO_CUENTA_ACTIVA).Sum(c => c.Saldo);
                
                var clienteDto = new ClienteDetalleDto(
                    cliente.Id,
                    usuario?.Id ?? 0,
                    usuario?.Identificacion ?? "",
                    usuario?.Nombre ?? "",
                    usuario?.Telefono,
                    usuario?.Email ?? "",
                    cliente.Direccion,
                    cliente.FechaNacimiento,
                    cliente.Estado,
                    cliente.FechaRegistro,
                    cliente.UltimaOperacion,
                    cuentasActivas,
                    saldoTotal,
                    usuario != null ? _mapper.Map<UsuarioVinculadoDto>(usuario) : null,
                    cliente.GestorAsignado != null ? _mapper.Map<GestorAsignadoDto>(cliente.GestorAsignado) : null,
                    _mapper.Map<List<CuentaClienteDto>>(cuentas)
                );
                
                return Ok(ApiResponse<object>.Ok(clienteDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            try
            {
                var clienteId = await GetClienteIdFromTokenAsync();
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

        [HttpPost]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> Crear([FromBody] ClienteRequest request)
        {
            try
            {
                var resultado = await _clienteServicio.CrearClienteAsync(request);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return CreatedAtAction(
                    nameof(ObtenerPorId), 
                    new { id = resultado.Datos!.Id }, 
                    ApiResponse<object>.Ok(
                        _mapper.Map<ClienteCreacionDto>(resultado.Datos),
                        "Cliente creado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando cliente");
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ClienteActualizarRequest request)
        {
            try
            {
                var role = GetUserRole();
                if (role == "Gestor")
                {
                    var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                    if (cliente == null)
                        return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
                    
                    var gestorId = GetUsuarioIdFromToken();
                    if (cliente.GestorAsignadoId != gestorId)
                        return StatusCode(403, ApiResponse<object>.Fail("No puede modificar clientes fuera de su cartera."));
                    
                    request = request with { GestorId = null };
                }

                var resultado = await _clienteServicio.ActualizarClienteAsync(id, request);
                
                if (!resultado.Exitoso)
                    return BadRequest(ApiResponse<object>.Fail(resultado.Error!));

                return Ok(ApiResponse<object>.Ok(
                    _mapper.Map<ClienteActualizacionDto>(resultado.Datos!),
                    "Cliente actualizado exitosamente."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cliente {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ClienteActualizarRequest request)
        {
            try
            {
                var clienteId = await GetClienteIdFromTokenAsync();
                if (clienteId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Cliente no identificado."));

                request = request with { GestorId = null };
                
                return await Actualizar(clienteId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando mi perfil");
                return StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."));
            }
        }

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

        private async Task<int> GetClienteIdFromTokenAsync()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == 0) return 0;
            
            var cliente = await _clienteServicio.ObtenerPorUsuarioAsync(usuarioId);
            return cliente?.Id ?? 0;
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