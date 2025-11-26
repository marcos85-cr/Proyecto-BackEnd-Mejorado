using SistemaBancaEnLinea.BC.Entidades;

namespace SistemaBancaEnLinea.BW.Servicios
{
    public interface IUsuarioServicio
    {
        Task<Usuario> RegistrarUsuarioAsync(string email, string password, string rol);
        Task<(bool Exitoso, string? Token, string? Error)> IniciarSesionAsync(string email, string password);
        Task<bool> DesbloquearUsuarioAsync(int usuarioId);
    }
}