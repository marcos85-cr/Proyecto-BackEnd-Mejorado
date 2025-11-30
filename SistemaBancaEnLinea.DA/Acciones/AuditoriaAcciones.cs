using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class AuditoriaAcciones
    {
        private readonly BancaContext _context;

        public AuditoriaAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(int usuarioId, string tipoOperacion, string descripcion, string? detalleJson = null)
        {
            try
            {
                var registro = new RegistroAuditoria
                {
                    UsuarioId = usuarioId != 0 ? usuarioId : 1,
                    TipoOperacion = tipoOperacion,
                    Descripcion = descripcion,
                    DetalleJson = detalleJson,
                    FechaHora = DateTime.UtcNow
                };

                _context.RegistrosAuditoria.Add(registro);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Manejo de errores (puede ser logging, rethrow, etc.)
                throw new Exception("Error registrando auditoría", ex);
            }

        }

        public async Task<List<RegistroAuditoria>> ObtenerPorFechasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.RegistrosAuditoria
                .Where(ra => ra.FechaHora >= fechaInicio && ra.FechaHora <= fechaFin)
                .Include(ra => ra.Usuario)
                .OrderByDescending(ra => ra.FechaHora)
                .ToListAsync();
        }

        public async Task<List<RegistroAuditoria>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            return await _context.RegistrosAuditoria
                .Where(ra => ra.UsuarioId == usuarioId)
                .OrderByDescending(ra => ra.FechaHora)
                .ToListAsync();
        }

        public async Task<List<RegistroAuditoria>> ObtenerPorTipoOperacionAsync(string tipoOperacion)
        {
            return await _context.RegistrosAuditoria
                .Where(ra => ra.TipoOperacion == tipoOperacion)
                .Include(ra => ra.Usuario)
                .OrderByDescending(ra => ra.FechaHora)
                .ToListAsync();
        }
    }
}