using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(ICuentaServicio cuentaServicio, ILogger<AccountsController> logger)
        {
            _cuentaServicio = cuentaServicio;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/accounts/my-accounts
        /// Obtiene todas las cuentas del cliente actual
        /// </summary>
        [HttpGet("my-accounts")]
        public async Task<IActionResult> GetMyAccounts()
        {
            try
            {
                var clienteIdClaim = User.FindFirst("client_id")?.Value;
                if (!int.TryParse(clienteIdClaim, out var clienteId))
                {
                    return Unauthorized(new { success = false, message = "Cliente no identificado" });
                }

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = cuentas.Select(c => new
                    {
                        id = c.Id,
                        numeroCuenta = c.Numero,
                        tipo = c.Tipo,
                        moneda = c.Moneda,
                        saldo = c.Saldo,
                        estado = c.Estado,
                        clienteId = c.ClienteId,
                        clienteNombre = c.Cliente?.NombreCompleto,
                        fechaApertura = c.FechaApertura ?? DateTime.UtcNow,
                        limiteDiario = 500000m,
                        saldoDisponible = c.Saldo
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo cuentas: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/accounts/{id}
        /// Obtiene los detalles de una cuenta específica
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(new { success = false, message = "Cuenta no encontrada" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = cuenta.Id,
                        numeroCuenta = cuenta.Numero,
                        tipo = cuenta.Tipo,
                        moneda = cuenta.Moneda,
                        saldo = cuenta.Saldo,
                        estado = cuenta.Estado,
                        clienteId = cuenta.ClienteId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/accounts
        /// Crea una nueva cuenta para el cliente
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos" });

                var clienteIdClaim = User.FindFirst("client_id")?.Value;
                if (!int.TryParse(clienteIdClaim, out var clienteId))
                    return Unauthorized(new { success = false, message = "Cliente no identificado" });

                // Validar tipo de cuenta
                if (!CuentasReglas.ValidarTipoCuenta(request.Tipo))
                    return BadRequest(new { success = false, message = "Tipo de cuenta inválido" });

                // Validar moneda
                if (!CuentasReglas.ValidarMoneda(request.Moneda))
                    return BadRequest(new { success = false, message = "Moneda inválida" });

                var cuenta = await _cuentaServicio.CrearCuentaAsync(
                    clienteId,
                    request.Tipo,
                    request.Moneda,
                    request.SaldoInicial
                );

                return CreatedAtAction(nameof(GetAccount), new { id = cuenta.Id }, new
                {
                    success = true,
                    message = "Cuenta creada exitosamente",
                    data = new
                    {
                        id = cuenta.Id,
                        numeroCuenta = cuenta.Numero,
                        tipo = cuenta.Tipo,
                        moneda = cuenta.Moneda,
                        saldo = cuenta.Saldo,
                        estado = cuenta.Estado
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
        /// PUT: api/accounts/{id}/block
        /// Bloquea una cuenta
        /// </summary>
        [HttpPut("{id}/block")]
        public async Task<IActionResult> BlockAccount(int id)
        {
            try
            {
                await _cuentaServicio.BloquearCuentaAsync(id);
                return Ok(new { success = true, message = "Cuenta bloqueada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/accounts/{id}/close
        /// Cierra una cuenta
        /// </summary>
        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseAccount(int id)
        {
            try
            {
                await _cuentaServicio.CerrarCuentaAsync(id);
                return Ok(new { success = true, message = "Cuenta cerrada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/accounts/{id}/balance
        /// Obtiene el saldo de una cuenta
        /// </summary>
        [HttpGet("{id}/balance")]
        public async Task<IActionResult> GetBalance(int id)
        {
            try
            {
                var cuenta = await _cuentaServicio.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(new { success = false, message = "Cuenta no encontrada" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        saldo = cuenta.Saldo,
                        disponible = cuenta.Saldo,
                        moneda = cuenta.Moneda
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    // DTOs
    public class CreateAccountRequest
    {
        public string Tipo { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; }
    }
}