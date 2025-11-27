using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BeneficiariosController : ControllerBase
    {
        private readonly IBeneficiarioServicio _beneficiarioServicio;
        private readonly ILogger<BeneficiariosController> _logger;

        public BeneficiariosController(
            IBeneficiarioServicio beneficiarioServicio,
            ILogger<BeneficiariosController> logger)
        {
            _beneficiarioServicio = beneficiarioServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-C1: Crear beneficiario (inicia en estado Inactivo)
        /// </summary>
        [HttpPost("crear")]
        public async Task<IActionResult> CrearBeneficiario([FromBody] CrearBeneficiarioRequest request)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var beneficiario = new Beneficiario
                {
                    ClienteId = clienteId,
                    Alias = request.Alias,
                    Banco = request.Banco,
                    Moneda = request.Moneda,
                    NumeroCuentaDestino = request.NumeroCuentaDestino,
                    Pais = request.Pais
                };

                var beneficiarioCreado = await _beneficiarioServicio.CrearBeneficiarioAsync(beneficiario);

                return CreatedAtAction(nameof(ObtenerBeneficiario), new { id = beneficiarioCreado.Id }, new
                {
                    success = true,
                    message = "Beneficiario creado. Debe confirmarlo antes de poder usarlo.",
                    data = new
                    {
                        id = beneficiarioCreado.Id,
                        alias = beneficiarioCreado.Alias,
                        banco = beneficiarioCreado.Banco,
                        numeroCuenta = beneficiarioCreado.NumeroCuentaDestino,
                        estado = beneficiarioCreado.Estado
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando beneficiario: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener beneficiario por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerBeneficiario(int id)
        {
            try
            {
                var beneficiario = await _beneficiarioServicio.ObtenerBeneficiarioAsync(id);
                if (beneficiario == null)
                    return NotFound(new { success = false, message = "Beneficiario no encontrado." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = beneficiario.Id,
                        alias = beneficiario.Alias,
                        banco = beneficiario.Banco,
                        moneda = beneficiario.Moneda,
                        numeroCuenta = beneficiario.NumeroCuentaDestino,
                        pais = beneficiario.Pais,
                        estado = beneficiario.Estado,
                        fechaCreacion = beneficiario.FechaCreacion
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-C1: Confirmar beneficiario
        /// </summary>
        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarBeneficiario(int id)
        {
            try
            {
                var beneficiario = await _beneficiarioServicio.ConfirmarBeneficiarioAsync(id);

                return Ok(new
                {
                    success = true,
                    message = "Beneficiario confirmado exitosamente.",
                    data = new
                    {
                        id = beneficiario.Id,
                        alias = beneficiario.Alias,
                        estado = beneficiario.Estado
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
        /// Obtener beneficiarios del cliente actual
        /// </summary>
        [HttpGet("mis-beneficiarios")]
        public async Task<IActionResult> ObtenerMisBeneficiarios()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var beneficiarios = await _beneficiarioServicio.ObtenerMisBeneficiariosAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = beneficiarios.Select(b => new
                    {
                        id = b.Id,
                        alias = b.Alias,
                        banco = b.Banco,
                        moneda = b.Moneda,
                        numeroCuenta = b.NumeroCuentaDestino,
                        pais = b.Pais,
                        estado = b.Estado,
                        fechaCreacion = b.FechaCreacion
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener beneficiarios de un cliente específico (admin/gestor)
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerBeneficiariosCliente(int clienteId)
        {
            try
            {
                var beneficiarios = await _beneficiarioServicio.ObtenerMisBeneficiariosAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = beneficiarios.Select(b => new
                    {
                        id = b.Id,
                        alias = b.Alias,
                        banco = b.Banco,
                        moneda = b.Moneda,
                        numeroCuenta = b.NumeroCuentaDestino,
                        pais = b.Pais,
                        estado = b.Estado
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar alias de beneficiario
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarBeneficiario(int id, [FromBody] ActualizarBeneficiarioRequest request)
        {
            try
            {
                var beneficiario = await _beneficiarioServicio.ActualizarBeneficiarioAsync(id, request.Alias);

                return Ok(new
                {
                    success = true,
                    message = "Beneficiario actualizado.",
                    data = new
                    {
                        id = beneficiario.Id,
                        alias = beneficiario.Alias,
                        estado = beneficiario.Estado
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
        /// Eliminar beneficiario
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarBeneficiario(int id)
        {
            try
            {
                await _beneficiarioServicio.EliminarBeneficiarioAsync(id);
                return Ok(new { success = true, message = "Beneficiario eliminado." });
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

        private int GetClienteIdFromToken()
        {
            var clienteIdClaim = User.FindFirst("client_id")?.Value;
            return int.TryParse(clienteIdClaim, out var clienteId) ? clienteId : 0;
        }
    }

    public class CrearBeneficiarioRequest
    {
        public string Alias { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string NumeroCuentaDestino { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
    }

    public class ActualizarBeneficiarioRequest
    {
        public string Alias { get; set; } = string.Empty;
    }
}