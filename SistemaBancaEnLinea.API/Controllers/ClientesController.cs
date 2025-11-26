using Microsoft.AspNetCore.Mvc;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        /// <summary>
        /// RF-A3: Obtener perfil del cliente
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerCliente(int id)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// RF-A3: Actualizar información del cliente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] ActualizarClienteRequest request)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// Obtener resumen de cuentas del cliente
        /// </summary>
        [HttpGet("{id}/resumen")]
        public async Task<IActionResult> ObtenerResumenCliente(int id)
        {
            // TODO: Implementar lógica
            return Ok();
        }
    }
    // Modelo para la solicitud de actualización del cliente
    public class ActualizarClienteRequest
    {
        public string? NombreCompleto { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }
}