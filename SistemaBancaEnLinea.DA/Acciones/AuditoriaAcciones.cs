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
                // Si no hay usuarioId válido, buscar el admin por defecto
                if (usuarioId <= 0)
                {
                    var adminUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Rol == "Administrador");
                    usuarioId = adminUser?.Id ?? 1;
                }

                // Verificar que el usuario existe
                var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
                if (!usuarioExiste)
                {
                    var adminUser = await _context.Usuarios.FirstOrDefaultAsync();
                    usuarioId = adminUser?.Id ?? throw new Exception("No hay usuarios en el sistema");
                }

                var registro = new RegistroAuditoria
                {
                    UsuarioId = usuarioId,
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
                // Log pero no fallar - la auditoría no debe bloquear operaciones
                Console.WriteLine($"Error registrando auditoría: {ex.Message}");
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