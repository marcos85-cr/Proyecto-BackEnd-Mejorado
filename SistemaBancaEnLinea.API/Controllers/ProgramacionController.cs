using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProgramacionController : ControllerBase
    {
        private readonly IProgramacionServicio _programacionServicio;
        private readonly ILogger<ProgramacionController> _logger;

        public ProgramacionController(
            IProgramacionServicio programacionServicio,
            ILogger<ProgramacionController> logger)
        {
            _programacionServicio = programacionServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-D3/RF-E3: Obtener programaciones del cliente actual
        /// </summary>
        [HttpGet("mis-programaciones")]
        public async Task<IActionResult> ObtenerMisProgramaciones()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var programaciones = await _programacionServicio.ObtenerProgramacionesClienteAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = programaciones.Select(p => new
                    {
                        transaccionId = p.TransaccionId,
                        tipo = p.Transaccion?.Tipo,
                        monto = p.Transaccion?.Monto,
                        moneda = p.Transaccion?.Moneda,
                        descripcion = p.Transaccion?.Descripcion,
                        fechaProgramada = p.FechaProgramada,
                        fechaLimiteCancelacion = p.FechaLimiteCancelacion,
                        estadoJob = p.EstadoJob,
                        puedeCancelarse = p.EstadoJob == "Pendiente" && DateTime.UtcNow < p.FechaLimiteCancelacion
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo programaciones: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-D3/RF-E3: Obtener programaciones de un cliente específico (admin/gestor)
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerProgramacionesCliente(int clienteId)
        {
            try
            {
                var programaciones = await _programacionServicio.ObtenerProgramacionesClienteAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = programaciones.Select(p => new
                    {
                        transaccionId = p.TransaccionId,
                        tipo = p.Transaccion?.Tipo,
                        monto = p.Transaccion?.Monto,
                        moneda = p.Transaccion?.Moneda,
                        fechaProgramada = p.FechaProgramada,
                        estadoJob = p.EstadoJob
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener detalle de una programación
        /// </summary>
        [HttpGet("{programacionId}")]
        public async Task<IActionResult> ObtenerProgramacion(int programacionId)
        {
            try
            {
                var programacion = await _programacionServicio.ObtenerProgramacionAsync(programacionId);
                if (programacion == null)
                    return NotFound(new { success = false, message = "Programación no encontrada." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        transaccionId = programacion.TransaccionId,
                        tipo = programacion.Transaccion?.Tipo,
                        monto = programacion.Transaccion?.Monto,
                        moneda = programacion.Transaccion?.Moneda,
                        descripcion = programacion.Transaccion?.Descripcion,
                        fechaProgramada = programacion.FechaProgramada,
                        fechaLimiteCancelacion = programacion.FechaLimiteCancelacion,
                        estadoJob = programacion.EstadoJob,
                        puedeCancelarse = programacion.EstadoJob == "Pendiente" &&
                                         DateTime.UtcNow < programacion.FechaLimiteCancelacion
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-D3/RF-E3: Cancelar una programación (hasta 24 horas antes)
        /// </summary>
        [HttpDelete("{programacionId}")]
        public async Task<IActionResult> CancelarProgramacion(int programacionId)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var resultado = await _programacionServicio.CancelarProgramacionAsync(programacionId, clienteId);

                return Ok(new
                {
                    success = true,
                    message = "Programación cancelada exitosamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelando programación: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private int GetClienteIdFromToken()
        {
            var clienteIdClaim = User.FindFirst("client_id")?.Value;
            return int.TryParse(clienteIdClaim, out var clienteId) ? clienteId : 0;
        }
    }
}