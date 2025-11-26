using Microsoft.AspNetCore.Mvc;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController : ControllerBase
    {
        private readonly GestionCuentasCU _gestionCuentasCU;

        public CuentasController(GestionCuentasCU gestionCuentasCU)
        {
            _gestionCuentasCU = gestionCuentasCU;
        }

        /// <summary>
        /// RF-B1: Abrir una nueva cuenta para un cliente
        /// </summary>
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCuenta([FromBody] AbrirCuentaRequest request)
        {
            try
            {
                if (request.SaldoInicial < 0)
                    return BadRequest(new { mensaje = "El saldo inicial debe ser mayor o igual a 0." });

                var cuenta = await _gestionCuentasCU.AbrirCuentaAsync(
                    request.ClienteId,
                    request.Tipo,
                    request.Moneda,
                    request.SaldoInicial,
                    request.UsuarioCreadorId
                );

                return CreatedAtAction(nameof(ObtenerCuenta), new { id = cuenta.Id },
                    new
                    {
                        mensaje = "Cuenta abierta exitosamente.",
                        numeroCuenta = cuenta.Numero,
                        saldo = cuenta.Saldo
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// RF-B2: Obtener detalles de una cuenta
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerCuenta(int id)
        {
            // TODO: Implementar lógica
            return Ok(new { id });
        }

        /// <summary>
        /// RF-B3: Bloquear una cuenta
        /// </summary>
        [HttpPut("{id}/bloquear")]
        public async Task<IActionResult> BloquearCuenta(int id, [FromBody] BloquearCuentaRequest request)
        {
            try
            {
                await _gestionCuentasCU.BloquearCuentaAsync(id, request.UsuarioId);
                return Ok(new { mensaje = "Cuenta bloqueada exitosamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// RF-B3: Cerrar una cuenta (solo si saldo = 0)
        /// </summary>
        [HttpPut("{id}/cerrar")]
        public async Task<IActionResult> CerrarCuenta(int id, [FromBody] CerrarCuentaRequest request)
        {
            try
            {
                await _gestionCuentasCU.CerrarCuentaAsync(id, request.UsuarioId);
                return Ok(new { mensaje = "Cuenta cerrada exitosamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtener todas las cuentas de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerCuentasCliente(int clienteId)
        {
            // TODO: Implementar lógica
            return Ok();
        }
    }

    // DTOs
    public class AbrirCuentaRequest
    {
        public int ClienteId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; }
        public int UsuarioCreadorId { get; set; }
    }

    public class BloquearCuentaRequest
    {
        public int UsuarioId { get; set; }
    }

    public class CerrarCuentaRequest
    {
        public int UsuarioId { get; set; }
    }
}