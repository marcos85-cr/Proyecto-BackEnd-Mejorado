namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    public record CrearProveedorRequest(string Nombre, string ReglaValidacionContrato);

    public record ProveedorDto(int Id, string Nombre, string ReglaValidacionContrato);

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
