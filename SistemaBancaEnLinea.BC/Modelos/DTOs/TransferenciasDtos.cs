namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    public record PreCheckTransferenciaRequest(
        int CuentaOrigenId,
        int? CuentaDestinoId,
        int? BeneficiarioId,
        decimal Monto);

    public record EjecutarTransferenciaRequest(
        int CuentaOrigenId,
        int? CuentaDestinoId,
        int? BeneficiarioId,
        decimal Monto,
        string Moneda = "CRC",
        string? Descripcion = null,
        bool Programada = false,
        DateTime? FechaProgramada = null);

    public record RechazarTransferenciaRequest(string Razon);

    public record PreCheckResultDto(
        bool PuedeEjecutar,
        decimal SaldoAntes,
        decimal Monto,
        decimal Comision,
        decimal MontoTotal,
        decimal SaldoDespues,
        bool RequiereAprobacion,
        decimal LimiteDisponible,
        string? Mensaje);

    public record TransferenciaEjecutadaDto(
        int TransaccionId,
        string Estado,
        string? ComprobanteReferencia,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion);

    public record TransferenciaTransaccionDetalleDto(
        int Id,
        string Tipo,
        string Estado,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string? Descripcion,
        string? ComprobanteReferencia,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion,
        string? CuentaOrigenNumero,
        string? CuentaDestinoNumero,
        string? BeneficiarioAlias,
        decimal SaldoAnterior,
        decimal SaldoPosterior);

    public record TransferenciaTransaccionListaDto(
        int Id,
        string Tipo,
        string Estado,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string? Descripcion,
        string? ComprobanteReferencia,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion);

    public record TransferenciaHistorialDto(
        int Id,
        string Tipo,
        string Estado,
        decimal Monto,
        string Moneda,
        DateTime FechaCreacion);

    public record TransferenciaEstadoDto(int Id, string Estado);
}
