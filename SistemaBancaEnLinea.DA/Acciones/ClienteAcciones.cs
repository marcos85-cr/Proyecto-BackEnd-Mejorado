using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class ClienteAcciones
    {
        private readonly BancaContext _context;

        public ClienteAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<Cliente?> ObtenerPorIdAsync(int id)
        {
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.Beneficiarios)
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
        {
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Identificacion == identificacion);
        }

        public async Task<bool> ExisteIdentificacionAsync(string identificacion)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.Identificacion == identificacion);
        }

        public async Task<Cliente> CrearAsync(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task ActualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Cliente>> ObtenerTodosAsync()
        {
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .ToListAsync();
        }

        public async Task<List<Cliente>> ObtenerPorGestorAsync(int gestorId)
        {
            return await _context.Clientes
                .Where(c => c.GestorAsignadoId == gestorId)
                .Include(c => c.Cuentas)
                .ToListAsync();
        }

        public async Task<bool> TieneCuentasAsync(int clienteId)
        {
            return await _context.Cuentas
                .AnyAsync(c => c.ClienteId == clienteId);
        }

        public async Task<bool> TieneBeneficiariosAsync(int clienteId)
        {
            return await _context.Beneficiarios
                .AnyAsync(b => b.ClienteId == clienteId);
        }

        public async Task<bool> TieneTransaccionesAsync(int clienteId)
        {
            return await _context.Transacciones
                .AnyAsync(t => t.ClienteId == clienteId);
        }

        public async Task EliminarAsync(Cliente cliente)
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
        }
    }
}