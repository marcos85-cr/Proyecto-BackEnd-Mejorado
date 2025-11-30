namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    // ==================== CUENTA ====================
    
    public record CuentaListaDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        DateTime? FechaApertura,
        string? Titular = null
    );

    public record CuentaDetalleDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        DateTime? FechaApertura,
        int ClienteId,
        string Titular
    );

    public record CrearCuentaRequest(
        string Tipo,
        string Moneda,
        decimal SaldoInicial = 0
    );

    // ==================== BENEFICIARIO ====================
    
    public record BeneficiarioListaDto(
        int Id,
        string Alias,
        string Banco,
        string Moneda,
        string NumeroCuenta,
        string? Pais,
        string Estado,
        DateTime FechaCreacion
    );

    public record BeneficiarioDetalleDto(
        int Id,
        string Alias,
        string Banco,
        string Moneda,
        string NumeroCuenta,
        string? Pais,
        string Estado,
        DateTime FechaCreacion,
        bool TieneOperacionesPendientes
    );

    public record CrearBeneficiarioRequest(
        string Alias,
        string Banco,
        string Moneda,
        string NumeroCuentaDestino,
        string? Pais = null
    );

    public record ActualizarBeneficiarioRequest(string NuevoAlias);

    // ==================== TRANSFERENCIA ====================
    
    public record TransferenciaListaDto(
        int Id,
        string Tipo,
        string Estado,
        decimal Monto,
        string Moneda,
        decimal Comision,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion,
        string? ComprobanteReferencia,
        string? Descripcion,
        string CuentaOrigen,
        string? CuentaDestino,
        string? BeneficiarioAlias
    );

    public record TransferenciaDetalleDto(
        int Id,
        string Tipo,
        string Estado,
        decimal Monto,
        string Moneda,
        decimal Comision,
        decimal SaldoAnterior,
        decimal SaldoPosterior,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion,
        string? ComprobanteReferencia,
        string? Descripcion,
        CuentaListaDto CuentaOrigen,
        CuentaListaDto? CuentaDestino,
        BeneficiarioListaDto? Beneficiario,
        ProgramacionDto? Programacion
    );

    public record TransferPreCheckDto(
        bool PuedeEjecutar,
        decimal SaldoAntes,
        decimal Monto,
        decimal Comision,
        decimal MontoTotal,
        decimal SaldoDespues,
        decimal LimiteDisponible,
        bool RequiereAprobacion,
        string Mensaje,
        List<string> Errores
    );

    public record TransferenciaRequest(
        int CuentaOrigenId,
        int? CuentaDestinoId,
        int? BeneficiarioId,
        decimal Monto,
        string Moneda,
        string? Descripcion,
        bool Programada = false,
        DateTime? FechaProgramada = null
    );

    public record ProgramacionDto(
        DateTime FechaProgramada,
        DateTime FechaLimiteCancelacion,
        string EstadoJob
    );

    // ==================== PAGO DE SERVICIOS ====================
    
    public record ProveedorServicioDto(
        int Id,
        string Nombre,
        string Categoria,
        string? Descripcion,
        string ReglaValidacionContrato
    );

    public record PagoServicioRequest(
        int ProveedorServicioId,
        int CuentaOrigenId,
        string NumeroContrato,
        decimal Monto,
        string? Descripcion,
        bool Programado = false,
        DateTime? FechaProgramada = null
    );

    public record ValidacionContratoResponse(
        bool EsValido,
        string Mensaje,
        string? NombreProveedor = null
    );

    // ==================== REPORTES ====================
    
    public record ExtractoCuentaRequest(
        int CuentaId,
        DateTime? FechaInicio,
        DateTime? FechaFin,
        string Formato = "json"  // json, pdf, csv
    );

    public record ExtractoCuentaDto(
        CuentaListaDto Cuenta,
        PeriodoDto Periodo,
        SaldoResumenDto Saldo,
        List<MovimientoDto> Movimientos,
        ResumenExtractoDto Resumen
    );

    public record PeriodoDto(DateTime Desde, DateTime Hasta);

    public record SaldoResumenDto(decimal Inicial, decimal Final);

    public record MovimientoDto(
        DateTime Fecha,
        string Tipo,
        string? Descripcion,
        decimal Monto,
        decimal Comision,
        string? Referencia,
        string Estado
    );

    public record ResumenExtractoDto(
        int TotalTransacciones,
        decimal TotalDebitos,
        decimal TotalCreditos
    );

    public record ResumenClienteDto(
        ClienteInfoDto Cliente,
        ResumenCuentasDto Cuentas,
        ResumenActividadDto Actividad
    );

    public record ClienteInfoDto(
        int Id,
        string Nombre,
        string Identificacion,
        string Correo,
        string? Telefono,
        DateTime FechaRegistro
    );

    public record ResumenCuentasDto(
        int Total,
        int Activas,
        decimal SaldoTotalCRC,
        decimal SaldoTotalUSD,
        List<CuentaListaDto> Detalle
    );

    public record ResumenActividadDto(
        int TotalTransacciones,
        int TransaccionesUltimoMes,
        decimal MontoTransferidoMes,
        DateTime? UltimaTransaccion
    );
}
