using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class AuditoriaServicio : IAuditoriaServicio
    {
        private readonly AuditoriaAcciones _auditoriaAcciones;

        public AuditoriaServicio(AuditoriaAcciones auditoriaAcciones)
        {
            _auditoriaAcciones = auditoriaAcciones;
        }

        public async Task<List<RegistroAuditoria>> ObtenerPorFechasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string? tipoOperacion = null)
        {
            var registros = await _auditoriaAcciones.ObtenerPorFechasAsync(fechaInicio, fechaFin);

            if (!string.IsNullOrEmpty(tipoOperacion))
            {
                registros = registros.Where(r => r.TipoOperacion == tipoOperacion).ToList();
            }

            return registros;
        }

        public async Task<List<RegistroAuditoria>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            return await _auditoriaAcciones.ObtenerPorUsuarioAsync(usuarioId);
        }

        public async Task RegistrarAsync(int usuarioId, string tipoOperacion, string descripcion, string? detalleJson = null)
        {
            await _auditoriaAcciones.RegistrarAsync(usuarioId, tipoOperacion, descripcion, detalleJson);
        }
    }
}
