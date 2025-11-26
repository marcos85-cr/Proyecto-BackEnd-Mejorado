using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.DA
{
    public interface IUsuarioAcciones
    {
        Task<Usuario?> ObtenerPorIdAsync(int id);
        Task<Usuario?> ObtenerPorEmailAsync(string email);
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario> CrearAsync(Usuario usuario);
        Task ActualizarAsync(Usuario usuario);
        Task<List<Usuario>> ObtenerTodosAsync();
        Task<List<Usuario>> ObtenerPorRolAsync(string rol);
    }
}