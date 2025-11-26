using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class TransaccionAcciones
    {
        private readonly BancaContext _context;

        public TransaccionAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<Transaccion?> ObtenerPorIdAsync(int id)
        {
            return await _context.Transacciones
                .Include(t => t.CuentaOrigen)
                .Include(t => t.CuentaDestino)
                .Include(t => t.Beneficiario)
                .Include(t => t.ProveedorServicio)
                .Include(t => t.Cliente)
                .Include(t => t.Programacion)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaccion?> ObtenerPorIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.Transacciones
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
        }

        public async Task<bool> ExisteIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.Transacciones
                .AnyAsync(t => t.IdempotencyKey == idempotencyKey);
        }

        public async Task<Transaccion> CrearAsync(Transaccion transaccion)
        {
            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();
            return transaccion;
        }

        public async Task ActualizarAsync(Transaccion transaccion)
        {
            _context.Transacciones.Update(transaccion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Transaccion>> ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.Transacciones
                .Where(t => t.ClienteId == clienteId)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();
        }

        public async Task<List<Transaccion>> FiltrarHistorialAsync(
            int? clienteId,
            int? cuentaId,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? tipo,
            string? estado)
        {
            var query = _context.Transacciones.AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(t => t.ClienteId == clienteId.Value);

            if (cuentaId.HasValue)
                query = query.Where(t => t.CuentaOrigenId == cuentaId.Value ||
                                         t.CuentaDestinoId == cuentaId.Value);

            if (fechaInicio.HasValue)
                query = query.Where(t => t.FechaCreacion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(t => t.FechaCreacion <= fechaFin.Value);

            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(t => t.Tipo == tipo);

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(t => t.Estado == estado);

            return await query
                .Include(t => t.CuentaOrigen)
                .Include(t => t.CuentaDestino)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();
        }

        // Para calcular límite diario
        public async Task<decimal> ObtenerMontoTransferidoHoyAsync(int clienteId)
        {
            var hoy = DateTime.UtcNow.Date;
            return await _context.Transacciones
                .Where(t => t.ClienteId == clienteId &&
                           t.FechaCreacion.Date == hoy &&
                           t.Estado == "Exitosa")
                .SumAsync(t => t.Monto);
        }
    }
}