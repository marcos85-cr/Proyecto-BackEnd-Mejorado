using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IProveedorServicioServicio
    {
        Task<List<ProveedorServicio>> ObtenerTodosAsync();
        Task<ProveedorServicio?> ObtenerPorIdAsync(int id);
        Task<ProveedorServicio> CrearAsync(ProveedorServicio proveedor);
        Task<ProveedorServicio> ActualizarAsync(int id, ProveedorServicio proveedor);
        Task<bool> EliminarAsync(int id);
    }
}