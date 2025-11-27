using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class ProveedorServicioAcciones
    {
        private readonly BancaContext _context;

        public ProveedorServicioAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<ProveedorServicio?> ObtenerPorIdAsync(int id)
        {
            return await _context.ProveedoresServicios
                .Include(p => p.CreadoPor)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<ProveedorServicio>> ObtenerTodosAsync()
        {
            return await _context.ProveedoresServicios
                .Include(p => p.CreadoPor)
                .ToListAsync();
        }

        public async Task<ProveedorServicio> CrearAsync(ProveedorServicio proveedor)
        {
            _context.ProveedoresServicios.Add(proveedor);
            await _context.SaveChangesAsync();
            return proveedor;
        }

        public async Task ActualizarAsync(ProveedorServicio proveedor)
        {
            _context.ProveedoresServicios.Update(proveedor);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(ProveedorServicio proveedor)
        {
            _context.ProveedoresServicios.Remove(proveedor);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _context.ProveedoresServicios
                .AnyAsync(p => p.Nombre == nombre);
        }
    }
}