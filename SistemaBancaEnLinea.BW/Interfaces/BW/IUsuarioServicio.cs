using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IUsuarioServicio
    {
        Task<Usuario> RegistrarUsuarioAsync(string email, string password, string rol);
        Task<ResultadoLogin> IniciarSesionAsync(string email, string password);
        Task<bool> DesbloquearUsuarioAsync(int usuarioId);
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario?> ObtenerPorIdAsync(int id);
        Task<Usuario?> ObtenerPorEmailAsync(string email);
    }

    public class ResultadoLogin
    {
        public bool Exitoso { get; set; }
        public string? Token { get; set; }
        public string? Error { get; set; }
        public Usuario? Usuario { get; set; }
    }
}