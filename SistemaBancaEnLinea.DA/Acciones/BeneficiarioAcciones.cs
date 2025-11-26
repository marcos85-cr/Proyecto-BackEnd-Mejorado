using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class BeneficiarioAcciones
    {
        private readonly BancaContext _context;

        public BeneficiarioAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<Beneficiario?> ObtenerPorIdAsync(int id)
        {
            return await _context.Beneficiarios
                .Include(b => b.Cliente)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> ExisteAliasParaClienteAsync(int clienteId, string alias)
        {
            return await _context.Beneficiarios
                .AnyAsync(b => b.ClienteId == clienteId && b.Alias == alias);
        }

        public async Task<Beneficiario> CrearAsync(Beneficiario beneficiario)
        {
            _context.Beneficiarios.Add(beneficiario);
            await _context.SaveChangesAsync();
            return beneficiario;
        }

        public async Task ActualizarAsync(Beneficiario beneficiario)
        {
            _context.Beneficiarios.Update(beneficiario);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Beneficiario beneficiario)
        {
            _context.Beneficiarios.Remove(beneficiario);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Beneficiario>> ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.Beneficiarios
                .Where(b => b.ClienteId == clienteId)
                .ToListAsync();
        }

        public async Task<List<Beneficiario>> FiltrarAsync(int clienteId, string? alias, string? banco, string? pais)
        {
            var query = _context.Beneficiarios
                .Where(b => b.ClienteId == clienteId);

            if (!string.IsNullOrEmpty(alias))
                query = query.Where(b => b.Alias.Contains(alias));

            if (!string.IsNullOrEmpty(banco))
                query = query.Where(b => b.Banco.Contains(banco));

            if (!string.IsNullOrEmpty(pais))
                query = query.Where(b => b.Pais.Contains(pais));

            return await query.ToListAsync();
        }
    }
}