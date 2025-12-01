namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    public record CrearCuentaGestorRequest(string Tipo, string Moneda, decimal SaldoInicial = 0);

    public record RechazarOperacionRequest(string Razon);

    public record GestorDashboardDto(
        int MyClients,
        int ActiveAccounts,
        int TodayOperations,
        int PendingApprovals,
        decimal TotalVolume);

    public record OperacionPendienteDto(
        int Id,
        int ClienteId,
        string ClienteNombre,
        string Tipo,
        string? Descripcion,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime Fecha,
        string? CuentaOrigenNumero,
        string? CuentaDestinoNumero,
        bool RequiereAprobacion,
        bool EsUrgente);

    public record ClienteGestorDto(
        int Id,
        string Nombre,
        string Email,
        string Identificacion,
        string Telefono,
        int CuentasActivas,
        DateTime UltimaOperacion,
        string Estado,
        decimal VolumenTotal);

    public record ClientesStatsDto(int TotalClients, int TotalAccounts, decimal TotalVolume);

    public record ClientesGestorResponseDto(IEnumerable<ClienteGestorDto> Data, ClientesStatsDto Stats);

    public record CuentaSimpleDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        DateTime? FechaApertura);

    public record ClienteDetalleGestorDto(
        int Id,
        string Identificacion,
        string Nombre,
        string Telefono,
        string Email,
        string Estado,
        DateTime FechaRegistro,
        DateTime? UltimaOperacion,
        int CuentasActivas,
        decimal VolumenTotal,
        int TotalTransacciones,
        IEnumerable<CuentaSimpleDto> Cuentas);

    public record CuentaGestorDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        DateTime FechaApertura,
        int ClienteId,
        string ClienteNombre);

    public record TransaccionGestorDto(
        int Id,
        string Tipo,
        string? Descripcion,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime Fecha,
        DateTime? FechaEjecucion,
        string? CuentaOrigenNumero,
        string? CuentaDestinoNumero,
        string? ComprobanteReferencia);

    public record CuentaCreadaGestorDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        int ClienteId,
        string ClienteNombre,
        DateTime? FechaApertura);

    public record OperacionDto(
        int Id,
        int ClienteId,
        string ClienteNombre,
        string Tipo,
        string? Descripcion,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime Fecha,
        string? CuentaOrigenNumero,
        string? CuentaDestinoNumero,
        bool RequiereAprobacion,
        bool EsUrgente);

    public record OperacionesResumenDto(int Pending, int Approved, int Rejected);

    public record OperacionesResponseDto(IEnumerable<OperacionDto> Data, OperacionesResumenDto Summary);

    public record OperacionDetalleDto(
        int Id,
        int ClienteId,
        string ClienteNombre,
        string Tipo,
        string? Descripcion,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime Fecha,
        DateTime? FechaEjecucion,
        string? CuentaOrigenNumero,
        string? CuentaDestinoNumero,
        string? BeneficiarioAlias,
        string? ComprobanteReferencia,
        decimal SaldoAnterior,
        decimal SaldoPosterior,
        bool RequiereAprobacion,
        bool EsUrgente);

    public record OperacionResultadoDto(int Id, string Estado, DateTime? FechaEjecucion);
}
