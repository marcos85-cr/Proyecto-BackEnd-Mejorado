using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServicio _clienteServicio;
        private readonly ICuentaServicio _cuentaServicio;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteServicio clienteServicio,
            ICuentaServicio cuentaServicio,
            ILogger<ClientesController> logger)
        {
            _clienteServicio = clienteServicio;
            _cuentaServicio = cuentaServicio;
            _logger = logger;
        }

        /// <summary>
        /// RF-A3: Obtener perfil del cliente por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerCliente(int id)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(id);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = cliente.Id,
                        identificacion = cliente.Identificacion,
                        nombre = cliente.NombreCompleto,
                        nombreCompleto = cliente.NombreCompleto,
                        telefono = cliente.Telefono,
                        correo = cliente.Correo,
                        estado = cliente.Estado,
                        fechaRegistro = cliente.FechaRegistro,
                        ultimaOperacion = cliente.UltimaOperacion,
                        cuentasActivas = cuentas.Count(c => c.Estado == "Activa"),
                        saldoTotal = cuentas.Where(c => c.Estado == "Activa").Sum(c => c.Saldo)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo cliente: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener mi perfil de cliente
        /// </summary>
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                var cliente = await _clienteServicio.ObtenerClienteAsync(clienteId);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(clienteId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = cliente.Id,
                        identificacion = cliente.Identificacion,
                        nombre = cliente.NombreCompleto,
                        nombreCompleto = cliente.NombreCompleto,
                        telefono = cliente.Telefono,
                        correo = cliente.Correo,
                        estado = cliente.Estado,
                        fechaRegistro = cliente.FechaRegistro,
                        ultimaOperacion = cliente.UltimaOperacion,
                        cuentasActivas = cuentas.Count(c => c.Estado == "Activa"),
                        saldoTotal = cuentas.Where(c => c.Estado == "Activa").Sum(c => c.Saldo)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// RF-A3: Actualizar información del cliente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] ActualizarClienteRequest request)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                // Actualizar campos permitidos
                if (!string.IsNullOrWhiteSpace(request.NombreCompleto))
                    cliente.NombreCompleto = request.NombreCompleto;
                if (!string.IsNullOrWhiteSpace(request.Telefono))
                    cliente.Telefono = request.Telefono;
                if (!string.IsNullOrWhiteSpace(request.Correo))
                    cliente.Correo = request.Correo;

                var clienteActualizado = await _clienteServicio.ActualizarClienteAsync(cliente);

                return Ok(new
                {
                    success = true,
                    message = "Cliente actualizado exitosamente.",
                    data = new
                    {
                        id = clienteActualizado.Id,
                        nombre = clienteActualizado.NombreCompleto,
                        nombreCompleto = clienteActualizado.NombreCompleto,
                        telefono = clienteActualizado.Telefono,
                        correo = clienteActualizado.Correo
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
        /// Actualizar mi perfil
        /// </summary>
        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ActualizarClienteRequest request)
        {
            try
            {
                var clienteId = GetClienteIdFromToken();
                if (clienteId == 0)
                    return Unauthorized(new { success = false, message = "Cliente no identificado." });

                return await ActualizarCliente(clienteId, request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener resumen de cuentas del cliente
        /// </summary>
        [HttpGet("{id}/resumen")]
        public async Task<IActionResult> ObtenerResumenCliente(int id)
        {
            try
            {
                var cliente = await _clienteServicio.ObtenerClienteAsync(id);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado." });

                var cuentas = await _cuentaServicio.ObtenerMisCuentasAsync(id);

                var resumen = new
                {
                    cliente = new
                    {
                        id = cliente.Id,
                        nombre = cliente.NombreCompleto,
                        identificacion = cliente.Identificacion
                    },
                    resumenCuentas = new
                    {
                        totalCuentas = cuentas.Count,
                        cuentasActivas = cuentas.Count(c => c.Estado == "Activa"),
                        cuentasBloqueadas = cuentas.Count(c => c.Estado == "Bloqueada"),
                        saldoTotalCRC = cuentas.Where(c => c.Moneda == "CRC" && c.Estado == "Activa").Sum(c => c.Saldo),
                        saldoTotalUSD = cuentas.Where(c => c.Moneda == "USD" && c.Estado == "Activa").Sum(c => c.Saldo)
                    },
                    cuentas = cuentas.Select(c => new
                    {
                        id = c.Id,
                        numero = c.Numero,
                        tipo = c.Tipo,
                        moneda = c.Moneda,
                        saldo = c.Saldo,
                        estado = c.Estado
                    })
                };

                return Ok(new { success = true, data = resumen });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener todos los clientes (solo admin/gestor)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administrador,Gestor")]
        public async Task<IActionResult> ObtenerTodosLosClientes()
        {
            try
            {
                var clientes = await _clienteServicio.ObtenerTodosAsync();

                return Ok(new
                {
                    success = true,
                    data = clientes.Select(c => new
                    {
                        id = c.Id,
                        identificacion = c.Identificacion,
                        nombreCompleto = c.NombreCompleto,
                        telefono = c.Telefono,
                        correo = c.Correo,
                        estado = c.Estado,
                        fechaRegistro = c.FechaRegistro,
                        cuentasActivas = c.Cuentas?.Count(cu => cu.Estado == "Activa") ?? 0
                    })
                });
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

    public class ActualizarClienteRequest
    {
        public string? NombreCompleto { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }
}