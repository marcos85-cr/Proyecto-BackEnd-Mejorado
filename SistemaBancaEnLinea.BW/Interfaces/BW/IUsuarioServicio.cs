using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    /// <summary>
    /// Interfaz del servicio de usuarios
    /// Define operaciones CRUD con validaciones integradas
    /// </summary>
    public interface IUsuarioServicio
    {
        #region Autenticación

        /// <summary>
        /// Inicia sesión y genera token JWT
        /// </summary>
        Task<ResultadoLogin> IniciarSesionAsync(string email, string password);

        #endregion

        #region Consultas

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        Task<List<Usuario>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        Task<Usuario?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene usuarios filtrados por rol
        /// </summary>
        Task<List<Usuario>> ObtenerPorRolAsync(string rol);

        /// <summary>
        /// Verifica si un email ya está registrado
        /// </summary>
        Task<bool> ExisteEmailAsync(string email);

        #endregion

        #region Operaciones CRUD con Validaciones Integradas

        /// <summary>
        /// Crea un nuevo usuario con todas las validaciones de negocio
        /// </summary>
        Task<ResultadoOperacion<Usuario>> CrearUsuarioAsync(UsuarioRequest request);

        /// <summary>
        /// Actualiza un usuario existente con todas las validaciones de negocio
        /// </summary>
        Task<ResultadoOperacion<Usuario>> ActualizarUsuarioAsync(int id, UsuarioRequest request);

        /// <summary>
        /// Bloquea o desbloquea un usuario con validaciones de permisos
        /// </summary>
        Task<ResultadoOperacion<Usuario>> ToggleBloqueoUsuarioAsync(int usuarioId, int adminId);

        /// <summary>
        /// Cambia la contraseña con validación de contraseña actual
        /// </summary>
        Task<ResultadoOperacion<bool>> CambiarContrasenaAsync(int usuarioId, int solicitanteId, string rolSolicitante, string contrasenaActual, string nuevaContrasena);

        /// <summary>
        /// Elimina un usuario validando que no tenga referencias en otras tablas
        /// </summary>
        Task<ResultadoOperacion<bool>> EliminarUsuarioAsync(int usuarioId, int adminId);

        #endregion
    }
}
