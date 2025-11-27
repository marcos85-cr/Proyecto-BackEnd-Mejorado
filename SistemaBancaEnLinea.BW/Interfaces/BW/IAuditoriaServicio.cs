using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IAuditoriaServicio
    {
        Task<List<RegistroAuditoria>> ObtenerPorFechasAsync(DateTime fechaInicio, DateTime fechaFin, string? tipoOperacion = null);
        Task<List<RegistroAuditoria>> ObtenerPorUsuarioAsync(int usuarioId);
        Task RegistrarAsync(int usuarioId, string tipoOperacion, string descripcion, string? detalleJson = null);
    }
}
