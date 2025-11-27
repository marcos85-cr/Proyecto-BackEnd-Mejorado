using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceProvidersController : ControllerBase
    {
        private readonly IProveedorServicioServicio _proveedorServicio;
        private readonly IAuditoriaServicio _auditoriaServicio;
        private readonly ILogger<ServiceProvidersController> _logger;

        public ServiceProvidersController(
            IProveedorServicioServicio proveedorServicio,
            IAuditoriaServicio auditoriaServicio,
            ILogger<ServiceProvidersController> logger)
        {
            _proveedorServicio = proveedorServicio;
            _auditoriaServicio = auditoriaServicio;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/serviceproviders
        /// Obtiene todos los proveedores de servicios
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProviders()
        {
            try
            {
                var proveedores = await _proveedorServicio.ObtenerTodosAsync();

                return Ok(new
                {
                    success = true,
                    data = proveedores.Select(p => new
                    {
                        id = p.Id.ToString(),
                        nombre = p.Nombre,
                        tipo = ExtractTipoFromNombre(p.Nombre),
                        icon = GetIconForTipo(ExtractTipoFromNombre(p.Nombre)),
                        codigoValidacion = p.ReglaValidacionContrato,
                        regex = p.ReglaValidacionContrato,
                        activo = true,
                        creadoPor = p.CreadoPor?.Nombre ?? "Sistema",
                        fechaCreacion = DateTime.UtcNow
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo proveedores: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/serviceproviders/{id}
        /// Obtiene un proveedor por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProvider(int id)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = proveedor.Id.ToString(),
                        nombre = proveedor.Nombre,
                        tipo = ExtractTipoFromNombre(proveedor.Nombre),
                        icon = GetIconForTipo(ExtractTipoFromNombre(proveedor.Nombre)),
                        codigoValidacion = proveedor.ReglaValidacionContrato,
                        regex = proveedor.ReglaValidacionContrato,
                        activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo proveedor {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/serviceproviders
        /// Crea un nuevo proveedor (Solo Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateProvider([FromBody] CreateProviderRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos" });

                var adminId = GetCurrentUserId();

                var proveedor = new ProveedorServicio
                {
                    Nombre = request.Nombre,
                    ReglaValidacionContrato = request.Regex ?? request.CodigoValidacion,
                    CreadoPorUsuarioId = adminId
                };

                var proveedorCreado = await _proveedorServicio.CrearAsync(proveedor);

                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    "CreacionProveedor",
                    $"Proveedor {request.Nombre} creado"
                );

                return CreatedAtAction(nameof(GetProvider), new { id = proveedorCreado.Id }, new
                {
                    success = true,
                    message = "Proveedor creado exitosamente",
                    data = new
                    {
                        id = proveedorCreado.Id.ToString(),
                        nombre = proveedorCreado.Nombre,
                        codigoValidacion = proveedorCreado.ReglaValidacionContrato
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando proveedor: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/serviceproviders/{id}
        /// Actualiza un proveedor
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateProviderRequest request)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    proveedor.Nombre = request.Nombre;

                if (!string.IsNullOrWhiteSpace(request.Regex))
                    proveedor.ReglaValidacionContrato = request.Regex;

                await _proveedorServicio.ActualizarAsync(id, proveedor);

                var adminId = GetCurrentUserId();
                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    "ActualizacionProveedor",
                    $"Proveedor {proveedor.Nombre} actualizado"
                );

                return Ok(new
                {
                    success = true,
                    message = "Proveedor actualizado exitosamente",
                    data = new
                    {
                        id = proveedor.Id.ToString(),
                        nombre = proveedor.Nombre,
                        codigoValidacion = proveedor.ReglaValidacionContrato
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error actualizando proveedor {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE: api/serviceproviders/{id}
        /// Elimina un proveedor
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            try
            {
                var resultado = await _proveedorServicio.EliminarAsync(id);
                if (!resultado)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                var adminId = GetCurrentUserId();
                await _auditoriaServicio.RegistrarAsync(
                    adminId,
                    "EliminacionProveedor",
                    $"Proveedor {id} eliminado"
                );

                return Ok(new { success = true, message = "Proveedor eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error eliminando proveedor {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/serviceproviders/{id}/validate-reference
        /// Valida un número de referencia/contrato
        /// </summary>
        [HttpPost("{id}/validate-reference")]
        public async Task<IActionResult> ValidateReference(int id, [FromBody] ValidateReferenceRequest request)
        {
            try
            {
                var proveedor = await _proveedorServicio.ObtenerPorIdAsync(id);
                if (proveedor == null)
                    return NotFound(new { success = false, message = "Proveedor no encontrado." });

                // Validar usando regex
                var isValid = System.Text.RegularExpressions.Regex.IsMatch(
                    request.NumeroReferencia,
                    proveedor.ReglaValidacionContrato
                );

                // Simular datos de validación
                var response = new
                {
                    valid = isValid,
                    monto = isValid ? new Random().Next(5000, 50000) : (int?)null,
                    nombre = isValid ? "Cliente de Ejemplo" : null,
                    message = isValid ? "Referencia válida" : "Número de referencia no válido"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validando referencia: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string ExtractTipoFromNombre(string nombre)
        {
            if (nombre.Contains("Electricidad") || nombre.Contains("ICE") || nombre.Contains("CNFL"))
                return "Electricidad";
            if (nombre.Contains("Agua") || nombre.Contains("AyA"))
                return "Agua";
            if (nombre.Contains("Teléfono") || nombre.Contains("Telefonía") || nombre.Contains("Kolbi") || nombre.Contains("Movistar"))
                return "Telefonía";
            if (nombre.Contains("Internet") || nombre.Contains("Cable"))
                return "Internet";
            if (nombre.Contains("Seguro"))
                return "Seguro";
            if (nombre.Contains("Municipalidad"))
                return "Municipalidades";
            if (nombre.Contains("Judicial") || nombre.Contains("Cobro"))
                return "Cobro Judicial";

            return "Otros";
        }

        private string GetIconForTipo(string tipo)
        {
            return tipo switch
            {
                "Electricidad" => "flash-outline",
                "Agua" => "water-outline",
                "Telefonía" => "call-outline",
                "Internet" => "wifi-outline",
                "Cable" => "tv-outline",
                "Seguro" => "shield-checkmark-outline",
                "Municipalidades" => "business-outline",
                "Cobro Judicial" => "document-text-outline",
                _ => "apps-outline"
            };
        }
    }

    // DTOs
    public class CreateProviderRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string CodigoValidacion { get; set; } = string.Empty;
        public string Regex { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }

    public class UpdateProviderRequest
    {
        public string? Nombre { get; set; }
        public string? Tipo { get; set; }
        public string? Icon { get; set; }
        public string? CodigoValidacion { get; set; }
        public string? Regex { get; set; }
        public bool? Activo { get; set; }
    }

    public class ValidateReferenceRequest
    {
        public string NumeroReferencia { get; set; } = string.Empty;
    }
}