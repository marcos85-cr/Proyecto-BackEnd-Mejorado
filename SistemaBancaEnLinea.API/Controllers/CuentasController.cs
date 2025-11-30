using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CuentasController : ControllerBase
    {
        private readonly ICuentaServicio _cuentaServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<CuentasController> _logger;

        public CuentasController(
            ICuentaServicio cuentaServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<CuentasController> logger)
        {
            _cuentaServicio = cuentaServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        [HttpGet("my-accounts")]
        public async Task<IActionResult> GetMyAccounts()
        {
            try
            {
                var clienteId = GetClienteId();
                if (clienteId == null)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado"));

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId.Value);

                return Ok(ApiResponse<IEnumerable<CuentaListaDto>>.Ok(
                    CuentasReglas.MapearAListaDto(cuentas)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cuentas del cliente");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> GetAllAccounts()
        {
            try
            {
                var cuentas = await _cuentaServicio.ObtenerTodasConRelacionesAsync();

                return Ok(ApiResponse<IEnumerable<CuentaCompletaDto>>.Ok(
                    CuentasReglas.MapearACompletaDto(cuentas)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todas las cuentas");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaConRelacionesAsync(id);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada"));

                if (!PuedoAccederCuenta(cuenta.ClienteId))
                    return Forbid();

                return Ok(ApiResponse<CuentaCompletaDto>.Ok(
                    CuentasReglas.MapearACompletaDto(cuenta)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cuenta {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CrearCuentaRequest request)
        {
            try
            {
                var clienteId = GetClienteId();
                if (clienteId == null)
                    return Unauthorized(ApiResponse.Fail("Cliente no identificado"));

                var validacion = CuentasReglas.ValidarCreacionCuenta(
                    request.Tipo, request.Moneda, request.SaldoInicial);
                
                if (!validacion.EsValido)
                    return BadRequest(ApiResponse.Fail(validacion.Error!));

                var cuenta = await _cuentaServicio.CrearCuentaAsync(
                    clienteId.Value, request.Tipo, request.Moneda, request.SaldoInicial);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "AperturaCuenta", 
                    $"Cuenta {cuenta.Numero} creada - {cuenta.Tipo} {cuenta.Moneda}");

                return CreatedAtAction(nameof(GetAccount), new { id = cuenta.Id },
                    ApiResponse<CuentaCreacionDto>.Ok(
                        CuentasReglas.MapearACreacionDto(cuenta),
                        "Cuenta creada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando cuenta");
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpPut("{id}/block")]
        public async Task<IActionResult> BlockAccount(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada"));

                // Validar permisos de modificación
                var (permitido, error) = await PuedoModificarCuentaAsync(cuenta);
                if (!permitido)
                    return StatusCode(403, ApiResponse.Fail(error!));

                var estadoAnterior = cuenta.Estado;
                await _cuentaServicio.BloquearCuentaAsync(id);

                cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                var accion = cuenta!.Estado == "Bloqueada" ? "bloqueada" : "desbloqueada";

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), 
                    cuenta.Estado == "Bloqueada" ? "BloqueoCuenta" : "DesbloqueoCuenta", 
                    $"Cuenta {cuenta.Numero} {accion}");

                return Ok(ApiResponse<CuentaEstadoDto>.Ok(
                    CuentasReglas.MapearAEstadoDto(cuenta, $"Cuenta {accion} exitosamente")));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bloqueando/desbloqueando cuenta {Id}", id);
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseAccount(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada"));

                // Validar permisos de modificación
                var (permitido, error) = await PuedoModificarCuentaAsync(cuenta);
                if (!permitido)
                    return StatusCode(403, ApiResponse.Fail(error!));

                await _cuentaServicio.CerrarCuentaAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "CierreCuenta", $"Cuenta {cuenta.Numero} cerrada");

                cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);

                return Ok(ApiResponse<CuentaEstadoDto>.Ok(
                    CuentasReglas.MapearAEstadoDto(cuenta!, "Cuenta cerrada exitosamente")));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cerrando cuenta {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpGet("{id}/balance")]
        public async Task<IActionResult> GetBalance(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada"));

                if (!PuedoAccederCuenta(cuenta.ClienteId))
                    return Forbid();

                return Ok(ApiResponse<CuentaBalanceDto>.Ok(
                    CuentasReglas.MapearABalanceDto(cuenta)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo balance de cuenta {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(ApiResponse.Fail("Cuenta no encontrada"));

                // Validar permisos de modificación
                var (permitido, error) = await PuedoModificarCuentaAsync(cuenta);
                if (!permitido)
                    return StatusCode(403, ApiResponse.Fail(error!));

                await _cuentaServicio.EliminarCuentaAsync(id);

                await _auditoriaServicio.RegistrarAsync(
                    GetUsuarioId(), "EliminacionCuenta", $"Cuenta {cuenta.Numero} eliminada (inactiva)");

                return Ok(ApiResponse<CuentaEstadoDto>.Ok(
                    new CuentaEstadoDto(cuenta.Id, cuenta.Numero, "Inactiva", "Cuenta eliminada exitosamente")));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cuenta {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
            }
        }

        private int? GetClienteId()
        {
            var claim = User.FindFirst("client_id")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private int GetUsuarioId()
        {
            var claim = User.FindFirst("user_id")?.Value 
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst("role")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                ?? "Cliente";
        }

        private bool PuedoAccederCuenta(int cuentaClienteId)
        {
            var role = GetUserRole();
            if (role is "Administrador" or "Gestor")
                return true;

            var clienteId = GetClienteId();
            return clienteId.HasValue && clienteId.Value == cuentaClienteId;
        }

        /// <summary>
        /// Valida si el usuario puede modificar la cuenta (bloquear, cerrar, eliminar)
        /// Restricción: Admin no puede modificar cuentas de otros admins
        /// </summary>
        private async Task<(bool Permitido, string? Error)> PuedoModificarCuentaAsync(Cuenta cuenta)
        {
            var role = GetUserRole();
            var clienteId = GetClienteId();

            // Cliente solo puede modificar sus propias cuentas
            if (role == "Cliente")
            {
                if (!clienteId.HasValue || clienteId.Value != cuenta.ClienteId)
                    return (false, "No tiene permiso para modificar esta cuenta.");
                return (true, null);
            }

            // Gestor puede modificar cuentas de sus clientes asignados
            if (role == "Gestor")
            {
                var cuentaConRelaciones = await _cuentaServicio.ObtenerCuentaConRelacionesAsync(cuenta.Id);
                if (cuentaConRelaciones?.Cliente?.GestorAsignadoId != GetUsuarioId())
                    return (false, "Solo puede modificar cuentas de clientes asignados a usted.");
                return (true, null);
            }

            // Administrador: no puede modificar cuentas de otros administradores
            if (role == "Administrador")
            {
                var cuentaConRelaciones = await _cuentaServicio.ObtenerCuentaConRelacionesAsync(cuenta.Id);
                var rolUsuarioCuenta = cuentaConRelaciones?.Cliente?.UsuarioAsociado?.Rol;
                
                if (rolUsuarioCuenta == "Administrador" && cuentaConRelaciones?.Cliente?.UsuarioAsociado?.Id != GetUsuarioId())
                    return (false, "No puede modificar cuentas de otros administradores.");
                
                return (true, null);
            }

            return (false, "Rol no autorizado.");
        }
    }
}
