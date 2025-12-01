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
        string? Titular = null,
        int? ClienteId = null,
        decimal LimiteDiario = 500000m,
        decimal? SaldoDisponible = null
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

    /// <summary>
    /// DTO completo de cuenta con todas sus relaciones
    /// </summary>
    public record CuentaCompletaDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado,
        DateTime? FechaApertura,
        CuentaRelacionClienteDto? Cliente,
        CuentaRelacionUsuarioDto? Usuario,
        CuentaRelacionGestorDto? Gestor
    );

    public record CuentaRelacionClienteDto(
        int Id,
        string? Direccion,
        DateTime? FechaNacimiento,
        string Estado,
        DateTime FechaRegistro
    );

    public record CuentaRelacionUsuarioDto(
        int Id,
        string Nombre,
        string Email,
        string? Telefono,
        string? Identificacion,
        string Rol
    );

    public record CuentaRelacionGestorDto(
        int Id,
        string Nombre,
        string Email
    );

    public record CrearCuentaRequest(
        string Tipo,
        string Moneda,
        decimal SaldoInicial = 0
    );

    public record CuentaCreacionDto(
        int Id,
        string Numero,
        string Tipo,
        string Moneda,
        decimal Saldo,
        string Estado
    );

    public record CuentaBalanceDto(
        decimal Saldo,
        decimal Disponible,
        string Moneda
    );

    public record CuentaEstadoDto(
        int Id,
        string Numero,
        string Estado,
        string Mensaje
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
        string ReglaValidacionContrato
    );

    public record ValidacionContratoResponse(
        bool EsValido,
        string Mensaje,
        string? NombreProveedor = null
    );

    public record ValidarContratoRequest(
        int ProveedorId,
        string NumeroContrato);

    public record RealizarPagoRequest(
        int CuentaOrigenId,
        int ProveedorServicioId,
        string NumeroContrato,
        decimal Monto,
        string? Descripcion);

    public record ProgramarPagoRequest(
        int CuentaOrigenId,
        int ProveedorServicioId,
        string NumeroContrato,
        decimal Monto,
        DateTime FechaProgramada,
        string? Descripcion);

    public record PagoRealizadoDto(
        int TransaccionId,
        string ComprobanteReferencia,
        decimal Monto,
        decimal Comision,
        decimal MontoTotal,
        string Estado,
        DateTime? FechaEjecucion,
        string? Proveedor,
        string NumeroContrato);

    public record PagoProgramadoDto(
        int TransaccionId,
        string Estado,
        DateTime FechaProgramada,
        decimal Monto,
        decimal Comision,
        string? Proveedor);

    public record PagoDetalleDto(
        int Id,
        string? Proveedor,
        string NumeroContrato,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion,
        string ComprobanteReferencia,
        string? Descripcion);

    public record PagoListaDto(
        int Id,
        string? Proveedor,
        string NumeroContrato,
        decimal Monto,
        string Moneda,
        decimal Comision,
        string Estado,
        DateTime FechaCreacion,
        DateTime? FechaEjecucion,
        string ComprobanteReferencia);

    public record PagoResumenDto(
        int Id,
        string? Proveedor,
        decimal Monto,
        string Estado,
        DateTime FechaCreacion);

    public record BeneficiarioCreacionDto(
        int Id,
        string Alias,
        string Banco,
        string NumeroCuenta,
        string Estado);

    public record BeneficiarioConfirmacionDto(
        int Id,
        string Alias,
        string Estado);

    public record BeneficiarioActualizacionDto(
        int Id,
        string Alias,
        string Estado);

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

    // ==================== PROGRAMACIÓN ====================

    public record ProgramacionListaDto(
        int TransaccionId,
        string? Tipo,
        decimal? Monto,
        string? Moneda,
        string? Descripcion,
        DateTime FechaProgramada,
        DateTime FechaLimiteCancelacion,
        string EstadoJob,
        bool PuedeCancelarse);

    public record ProgramacionResumenDto(
        int TransaccionId,
        string? Tipo,
        decimal? Monto,
        string? Moneda,
        DateTime FechaProgramada,
        string EstadoJob);

    public record ProgramacionDetalleDto(
        int Id,
        int TransaccionId,
        string? Tipo,
        decimal? Monto,
        string? Moneda,
        string? Descripcion,
        DateTime FechaProgramada,
        DateTime FechaLimiteCancelacion,
        string EstadoJob,
        bool PuedeCancelarse,
        string? CuentaOrigen,
        string? CuentaDestino);

    // ==================== VALIDACIÓN ====================

    public record ValidarCedulaRequest(string Cedula);

    public record ValidacionCedulaDto(
        bool EsValida,
        string? Tipo,
        string? CedulaFormateada,
        string Mensaje);

    public record IdentificacionDisponibilidadDto(
        bool Disponible,
        string Mensaje);

    // ==================== PROVEEDORES DE SERVICIO ====================
 
    public record ActualizarProveedorRequest(
        string? Nombre,
        string? ReglaValidacion);

    public record ValidarReferenciaRequest(string NumeroReferencia);

    public record ProveedorListaDto(
        string Id,
        string Nombre,
        string Tipo,
        string Icon,
        string ReglaValidacion,
        bool Activo,
        string CreadoPor);

    public record ProveedorDetalleDto(
        string Id,
        string Nombre,
        string Tipo,
        string Icon,
        string ReglaValidacion,
        bool Activo);

    public record ProveedorCreacionDto(
        string Id,
        string Nombre,
        string ReglaValidacion);

    public record ValidacionReferenciaDto(
        bool Valida,
        decimal? Monto,
        string? Nombre,
        string Mensaje);
}
