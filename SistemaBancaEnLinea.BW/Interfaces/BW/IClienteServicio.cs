using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IClienteServicio
    {
        // Operaciones CRUD con DTOs
        Task<ResultadoOperacion<Cliente>> CrearClienteAsync(ClienteRequest request);
        Task<Cliente?> ObtenerClienteAsync(int id);
        Task<Cliente?> ObtenerPorUsuarioAsync(int usuarioId);
        Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);
        Task<ResultadoOperacion<Cliente>> ActualizarClienteAsync(int id, ClienteActualizarRequest request);
        Task<ResultadoOperacion<bool>> EliminarClienteAsync(int id);
        Task<bool> ExisteIdentificacionAsync(string identificacion);
        Task<List<Cliente>> ObtenerTodosAsync();

        // Gestor de Clientes
        Task<List<Cliente>> ObtenerClientesPorGestorAsync(int gestorId);
        Task<ResultadoOperacion<bool>> AsignarClienteAGestorAsync(int clienteId, int gestorId);
        Task<ResultadoOperacion<bool>> DesasignarClienteDeGestorAsync(int clienteId);

        // Vinculación Usuario-Cliente
        Task<ResultadoOperacion<bool>> VincularUsuarioAsync(int clienteId, int usuarioId);
        Task<ResultadoOperacion<bool>> DesvincularUsuarioAsync(int clienteId);
    }
}