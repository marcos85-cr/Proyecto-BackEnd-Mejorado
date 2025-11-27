using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        /// <summary>
        /// POST: api/validation/cedula
        /// Valida una cédula costarricense
        /// </summary>
        [HttpPost("cedula")]
        [AllowAnonymous]
        public IActionResult ValidarCedula([FromBody] ValidarCedulaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Cedula))
                    return BadRequest(new { success = false, message = "Cédula requerida" });

                var esValida = ValidacionCedulaReglas.ValidarIdentificacion(request.Cedula);
                var tipo = ValidacionCedulaReglas.ObtenerTipoIdentificacion(request.Cedula);
                var formatoValido = ValidacionCedulaReglas.FormatearCedula(request.Cedula);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        esValida,
                        tipo,
                        cedulaFormateada = formatoValido,
                        mensaje = esValida
                            ? $"Identificación válida ({tipo})"
                            : "Identificación no válida"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/validation/check-unique-identification/{identificacion}
        /// Verifica si una identificación ya está registrada
        /// </summary>
        [HttpGet("check-unique-identification/{identificacion}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUniqueIdentification(
            string identificacion,
            [FromServices] SistemaBancaEnLinea.BW.Interfaces.BW.IClienteServicio clienteServicio)
        {
            try
            {
                var existe = await clienteServicio.ExisteIdentificacionAsync(identificacion);

                return Ok(new
                {
                    success = true,
                    available = !existe,
                    message = existe
                        ? "Esta identificación ya está registrada"
                        : "Identificación disponible"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class ValidarCedulaRequest
    {
        public string Cedula { get; set; } = string.Empty;
    }
}