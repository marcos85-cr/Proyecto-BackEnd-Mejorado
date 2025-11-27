using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class ProveedorServicioServicio : IProveedorServicioServicio
    {
        private readonly ProveedorServicioAcciones _proveedorAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ILogger<ProveedorServicioServicio> _logger;

        public ProveedorServicioServicio(
            ProveedorServicioAcciones proveedorAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ILogger<ProveedorServicioServicio> logger)
        {
            _proveedorAcciones = proveedorAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _logger = logger;
        }

        public async Task<List<ProveedorServicio>> ObtenerTodosAsync()
        {
            return await _proveedorAcciones.ObtenerTodosAsync();
        }

        public async Task<ProveedorServicio?> ObtenerPorIdAsync(int id)
        {
            return await _proveedorAcciones.ObtenerPorIdAsync(id);
        }

        public async Task<ProveedorServicio> CrearAsync(ProveedorServicio proveedor)
        {
            // Validar nombre
            if (!PagosServiciosReglas.ValidarNombreProveedor(proveedor.Nombre))
                throw new InvalidOperationException(
                    $"El nombre debe tener entre {PagosServiciosReglas.LONGITUD_MINIMA_NOMBRE_PROVEEDOR} " +
                    $"y {PagosServiciosReglas.LONGITUD_MAXIMA_NOMBRE_PROVEEDOR} caracteres.");

            // Validar que no exista
            if (await _proveedorAcciones.ExisteNombreAsync(proveedor.Nombre))
                throw new InvalidOperationException("Ya existe un proveedor con este nombre.");

            var proveedorCreado = await _proveedorAcciones.CrearAsync(proveedor);

            await _auditoriaAcciones.RegistrarAsync(
                proveedor.CreadoPorUsuarioId,
                "CreacionProveedor",
                $"Proveedor {proveedor.Nombre} creado"
            );

            _logger.LogInformation($"Proveedor {proveedor.Nombre} creado");
            return proveedorCreado;
        }

        public async Task<ProveedorServicio> ActualizarAsync(int id, ProveedorServicio proveedor)
        {
            var existente = await _proveedorAcciones.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new InvalidOperationException("Proveedor no encontrado.");

            existente.Nombre = proveedor.Nombre;
            existente.ReglaValidacionContrato = proveedor.ReglaValidacionContrato;

            await _proveedorAcciones.ActualizarAsync(existente);

            _logger.LogInformation($"Proveedor {id} actualizado");
            return existente;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var proveedor = await _proveedorAcciones.ObtenerPorIdAsync(id);
            if (proveedor == null)
                return false;

            await _proveedorAcciones.EliminarAsync(proveedor);

            _logger.LogInformation($"Proveedor {id} eliminado");
            return true;
        }
    }
}
