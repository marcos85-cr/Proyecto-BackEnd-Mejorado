using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IClienteServicio
    {
        Task<Cliente> CrearClienteAsync(Cliente cliente);
        Task<Cliente?> ObtenerClienteAsync(int id);
        Task<Cliente?> ObtenerPorUsuarioAsync(int usuarioId);
        Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);
        Task<Cliente> ActualizarClienteAsync(Cliente cliente);
        Task<bool> ExisteIdentificacionAsync(string identificacion);
    }
}