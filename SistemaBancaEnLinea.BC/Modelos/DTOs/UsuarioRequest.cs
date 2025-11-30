using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    /// <summary>
    /// Request para crear/actualizar usuario
    /// </summary>
    public record UsuarioRequest(
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        string Email,

        string? Password,

        string? ConfirmPassword,

        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        string? Nombre,

        [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres")]
        string? Identificacion,

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        string? Telefono,

        [Required(ErrorMessage = "El rol es requerido")]
        string Role = "Cliente"
    );

    /// <summary>
    /// Request para cambiar contraseña
    /// </summary>
    public record CambioContrasenaRequest(
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        string ContrasenaActual,

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        string NuevaContrasena
    );

    /// <summary>
    /// Request de login
    /// </summary>
    public record LoginRequest(
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        string Email,

        [Required(ErrorMessage = "La contraseña es requerida")]
        string Password
    );

    /// <summary>
    /// Request de registro
    /// </summary>
    public record RegistroRequest(
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        string Email,

        [Required(ErrorMessage = "La contraseña es requerida")]
        string Password,

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        string ConfirmPassword,

        [Required(ErrorMessage = "El nombre es requerido")]
        string Nombre,

        [Required(ErrorMessage = "La identificación es requerida")]
        string Identificacion,

        [Required(ErrorMessage = "El teléfono es requerido")]
        string Telefono,

        string? Rol = "Cliente"
    );

    /// <summary>
    /// Request para verificar email
    /// </summary>
    public record CheckEmailRequest(
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        string Email
    );
}
