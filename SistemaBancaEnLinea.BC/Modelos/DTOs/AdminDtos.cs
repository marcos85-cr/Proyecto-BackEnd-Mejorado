namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    public record AdminDashboardDto(
        int TotalUsuarios,
        int UsuariosActivos,
        int UsuariosBloqueados,
        int TotalClientes,
        int TotalCuentas,
        int CuentasActivas,
        int TotalProveedores,
        int OperacionesHoy,
        decimal VolumenTotal);

    public record CrearProveedorRequest(string Nombre, string ReglaValidacionContrato, string? FormatoContrato = null);

    public record ProveedorDto(int Id, string Nombre, string ReglaValidacionContrato, string? FormatoContrato);

    public record AuditoriaDto(
        int Id,
        DateTime FechaHora,
        string TipoOperacion,
        string Descripcion,
        int UsuarioId,
        string? UsuarioEmail,
        string? DetalleJson);

    public record AuditoriaResumenDto(
        int Id,
        DateTime FechaHora,
        string TipoOperacion,
        string Descripcion);
}
