using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class ProgramacionAcciones
    {
        private readonly BancaContext _context;

        public ProgramacionAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<Programacion?> ObtenerPorIdAsync(int transaccionId)
        {
            return await _context.Programaciones
                .Include(p => p.Transaccion)
                .FirstOrDefaultAsync(p => p.TransaccionId == transaccionId);
        }

        public async Task<List<Programacion>> ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.Programaciones
                .Include(p => p.Transaccion)
                .Where(p => p.Transaccion.ClienteId == clienteId)
                .OrderByDescending(p => p.FechaProgramada)
                .ToListAsync();
        }

        public async Task<List<Programacion>> ObtenerPendientesAsync()
        {
            return await _context.Programaciones
                .Include(p => p.Transaccion)
                .Where(p => p.EstadoJob == "Pendiente" && p.FechaProgramada <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<Programacion> CrearAsync(Programacion programacion)
        {
            _context.Programaciones.Add(programacion);
            await _context.SaveChangesAsync();
            return programacion;
        }

        public async Task ActualizarAsync(Programacion programacion)
        {
            _context.Programaciones.Update(programacion);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> PuedeCancelarseAsync(int transaccionId)
        {
            var programacion = await ObtenerPorIdAsync(transaccionId);
            if (programacion == null) return false;

            return programacion.EstadoJob == "Pendiente" &&
                   DateTime.UtcNow < programacion.FechaLimiteCancelacion;
        }
    }
}