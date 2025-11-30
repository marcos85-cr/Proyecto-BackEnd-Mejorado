namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    // ==================== REQUEST ====================
    
    /// <summary>
    /// Request para crear una cuenta
    /// </summary>
    public record CuentaRequest(
        string Tipo,         // Ahorro, Corriente
        string Moneda,       // CRC, USD
        decimal SaldoInicial = 0
    );

    /// <summary>
    /// Request para crear un cliente (datos personales están en Usuario)
    /// </summary>
    public record ClienteRequest(
        string? Direccion,                 // Atributo único del cliente
        DateTime? FechaNacimiento,         // Atributo único del cliente
        int UsuarioId,                     // Usuario obligatorio (contiene datos personales)
        int? GestorId,                     // Para asignar gestor al cliente
        List<CuentaRequest>? Cuentas       // Cuentas a crear junto con el cliente
    );

    /// <summary>
    /// Request para actualizar un cliente
    /// </summary>
    public record ClienteActualizarRequest(
        string? Direccion,                 // Atributo único del cliente
        DateTime? FechaNacimiento,         // Atributo único del cliente
        int? GestorId,                     // Para cambiar/asignar gestor
        List<CuentaRequest>? Cuentas       // Cuentas a agregar (solo crea, no actualiza)
    );

    // ==================== RESPONSE DTOs ====================
    
    /// <summary>
    /// DTO para listado de clientes (datos personales vienen del Usuario)
    /// </summary>
    public record ClienteListaDto(
        int Id,
        int UsuarioId,
        string Identificacion,
        string NombreCompleto,
        string? Telefono,
        string Email,
        string? Direccion,
        DateTime? FechaNacimiento,
        string Estado,
        DateTime FechaRegistro,
        int CuentasActivas,
        int? GestorId,
        string? GestorNombre
    );

    /// <summary>
    /// DTO para cuenta de cliente
    /// </summary>
    public record CuentaClienteDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado
    );

    /// <summary>
    /// DTO para detalle de cliente
    /// </summary>
    public record ClienteDetalleDto(
        int Id,
        int UsuarioId,
        string Identificacion,
        string NombreCompleto,
        string? Telefono,
        string Email,
        string? Direccion,
        DateTime? FechaNacimiento,
        string Estado,
        DateTime FechaRegistro,
        DateTime? UltimaOperacion,
        int CuentasActivas,
        decimal SaldoTotal,
        UsuarioVinculadoDto? Usuario,
        GestorAsignadoDto? Gestor,
        List<CuentaClienteDto> Cuentas
    );

    /// <summary>
    /// DTO para cuenta creada
    /// </summary>
    public record CuentaCreadaDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado
    );

    /// <summary>
    /// DTO para creación de cliente
    /// </summary>
    public record ClienteCreacionDto(
        int Id,
        int UsuarioId,
        string Identificacion,
        string NombreCompleto,
        string? Telefono,
        string Email,
        string? Direccion,
        DateTime? FechaNacimiento,
        string Estado,
        DateTime FechaRegistro,
        int? GestorId,
        List<CuentaCreadaDto> Cuentas
    );

    /// <summary>
    /// DTO para actualización de cliente
    /// </summary>
    public record ClienteActualizacionDto(
        int Id,
        string? Direccion,
        DateTime? FechaNacimiento,
        int? GestorId,
        string? GestorNombre
    );

    /// <summary>
    /// DTO para usuario vinculado
    /// </summary>
    public record UsuarioVinculadoDto(
        int Id,
        string Email,
        string Nombre,
        string? Identificacion,
        string? Telefono,
        string Rol,
        bool EstaBloqueado
    );

    /// <summary>
    /// DTO para gestor asignado
    /// </summary>
    public record GestorAsignadoDto(
        int Id,
        string Nombre,
        string Email
    );

    /// <summary>
    /// DTO para perfil del cliente (mi-perfil)
    /// </summary>
    public record ClientePerfilDto(
        int Id,
        int UsuarioId,
        string Identificacion,
        string NombreCompleto,
        string? Telefono,
        string Email,
        string? Direccion,
        DateTime? FechaNacimiento,
        string Estado,
        DateTime FechaRegistro,
        DateTime? UltimaOperacion,
        int CuentasActivas,
        decimal SaldoTotal
    );
}
