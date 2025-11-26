using Microsoft.AspNetCore.Mvc;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramacionController : ControllerBase
    {
        /// <summary>
        /// RF-D3/RF-E3: Obtener programaciones pendientes de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerProgramacionesCliente(int clienteId)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// RF-D3/RF-E3: Cancelar una programación (hasta 24 horas antes)
        /// </summary>
        [HttpDelete("{programacionId}")]
        public async Task<IActionResult> CancelarProgramacion(int programacionId, [FromBody] CancelarProgramacionRequest request)
        {
            // TODO: Implementar lógica
            return Ok(new { mensaje = "Programación cancelada exitosamente." });
        }
    }

    public class CancelarProgramacionRequest
    {
        public int ClienteId { get; set; }
    }
}