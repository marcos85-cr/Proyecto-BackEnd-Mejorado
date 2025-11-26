using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IUsuarioServicio
    {
        Task<Usuario> RegistrarUsuarioAsync(string email, string password, string rol);
        Task<(bool Exitoso, string? Token, string? Error)> IniciarSesionAsync(string email, string password);
        Task<bool> DesbloquearUsuarioAsync(int usuarioId);
    }
}