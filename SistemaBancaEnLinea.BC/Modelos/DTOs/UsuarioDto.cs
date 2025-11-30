namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    /// <summary>
    /// DTO para lista de usuarios
    /// </summary>
    public record UsuarioListaDto(
        int Id,
        string Email,
        string Role,
        string Nombre,
        string? Identificacion,
        string? Telefono,
        bool Bloqueado,
        int IntentosFallidos,
        DateTime FechaCreacion,
        int CuentasActivas = 0
    );

    /// <summary>
    /// DTO para detalle de usuario
    /// </summary>
    public record UsuarioDetalleDto(
        string Id,
        string Email,
        string Role,
        string Nombre,
        string? Identificacion,
        string? Telefono,
        bool Bloqueado,
        int IntentosFallidos,
        DateTime FechaCreacion,
        DateTime? FechaBloqueo
    );

    /// <summary>
    /// DTO para creación de usuario
    /// </summary>
    public record UsuarioCreacionDto(
        string Id,
        string Email,
        string Role,
        string? Nombre
    );

    /// <summary>
    /// DTO para actualización de usuario
    /// </summary>
    public record UsuarioActualizacionDto(
        string Id,
        string Email,
        string Role,
        string? Nombre,
        string? Identificacion,
        string? Telefono
    );

    /// <summary>
    /// DTO para bloqueo de usuario
    /// </summary>
    public record UsuarioBloqueoDto(
        string Id,
        bool Bloqueado,
        DateTime? FechaBloqueo
    );

    /// <summary>
    /// DTO para disponibilidad de email
    /// </summary>
    public record EmailDisponibilidadDto(bool Available);

    /// <summary>
    /// DTO para cambio de contraseña
    /// </summary>
    public record CambioContrasenaDto(
        string Id,
        string Email,
        DateTime FechaCambio
    );

    /// <summary>
    /// DTO para respuesta de login
    /// </summary>
    public record LoginDto(
        string Token
    );

    /// <summary>
    /// DTO de usuario para login
    /// </summary>
    public record UsuarioLoginDto(
        string Id,
        string Email,
        string Role,
        string Nombre,
        string? Identificacion,
        string? Telefono,
        bool Bloqueado,
        int IntentosFallidos
    );

    /// <summary>
    /// DTO para respuesta de registro
    /// </summary>
    public record RegistroDto(
        string Id,
        string Email,
        string Role,
        string Nombre,
        string? Identificacion,
        string? Telefono
    );
}
