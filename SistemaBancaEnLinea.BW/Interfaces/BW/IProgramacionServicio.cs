using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IProgramacionServicio
    {
        Task<List<Programacion>> ObtenerProgramacionesClienteAsync(int clienteId);
        Task<Programacion?> ObtenerProgramacionAsync(int transaccionId);
        Task<bool> CancelarProgramacionAsync(int transaccionId, int clienteId);
        Task EjecutarProgramacionesPendientesAsync();
    }
}