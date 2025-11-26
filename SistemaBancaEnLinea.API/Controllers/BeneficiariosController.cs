using Microsoft.AspNetCore.Mvc;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BeneficiariosController : ControllerBase
    {
        /// <summary>
        /// RF-C1: Crear beneficiario (inicia en estado Inactivo)
        /// </summary>
        [HttpPost("crear")]
        public async Task<IActionResult> CrearBeneficiario([FromBody] CrearBeneficiarioRequest request)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// RF-C1: Confirmar beneficiario
        /// </summary>
        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarBeneficiario(int id)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// Obtener beneficiarios de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerBeneficiariosCliente(int clienteId)
        {
            // TODO: Implementar lógica
            return Ok();
        }

        /// <summary>
        /// Eliminar beneficiario
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarBeneficiario(int id)
        {
            // TODO: Implementar lógica
            return Ok();
        }
    }

    public class CrearBeneficiarioRequest
    {
        public int ClienteId { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string NumeroCuentaDestino { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
    }
}