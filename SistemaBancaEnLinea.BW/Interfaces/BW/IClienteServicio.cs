using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IClienteServicio
    {
        // Operaciones CRUD básicas
        Task<Cliente> CrearClienteAsync(Cliente cliente);
        Task<Cliente?> ObtenerClienteAsync(int id);
        Task<Cliente?> ObtenerPorUsuarioAsync(int usuarioId);
        Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);
        Task<Cliente> ActualizarClienteAsync(Cliente cliente);
        Task<bool> ExisteIdentificacionAsync(string identificacion);
        Task<List<Cliente>> ObtenerTodosAsync();

        // Gestor de Clientes
        Task<List<Cliente>> ObtenerClientesPorGestorAsync(int gestorId);
        Task<bool> AsignarClienteAGestorAsync(int clienteId, int gestorId);
        Task<bool> DesasignarClienteDeGestorAsync(int clienteId);
    }
}