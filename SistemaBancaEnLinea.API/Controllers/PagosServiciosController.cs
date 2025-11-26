using Microsoft.AspNetCore.Mvc;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagosServiciosController : ControllerBase
    {
        /// <summary>
        /// RF-E1: Obtener lista de proveedores de servicio
        /// </summary>
        [HttpGet("proveedores")]
        public async Task<IActionResult> ObtenerProveedores()
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// RF-E2: Realizar pago de servicio
        /// </summary>
        [HttpPost("realizar-pago")]
        public async Task<IActionResult> RealizarPagoServicio([FromBody] RealizarPagoServicioRequest request)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// RF-E3: Programar pago de servicio
        /// </summary>
        [HttpPost("programar-pago")]
        public async Task<IActionResult> ProgramarPagoServicio([FromBody] ProgramarPagoServicioRequest request)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// Obtener historial de pagos de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}/historial")]
        public async Task<IActionResult> ObtenerHistorialPagos(int clienteId)
        {
            // TODO: Implementar lógica
            return Ok();
        }
    }

    public class RealizarPagoServicioRequest
    {
        public int ClienteId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int ProveedorServicioId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
    }

    public class ProgramarPagoServicioRequest
    {
        public int ClienteId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int ProveedorServicioId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaProgramada { get; set; }
        public string? Descripcion { get; set; }
    }
}