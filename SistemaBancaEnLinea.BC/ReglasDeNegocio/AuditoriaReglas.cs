
/// <summary>
/// RF-G2: Auditoría y trazabilidad
/// </summary>
public static class AuditoriaReglas
{
    // Tipos de operación auditados
    public static readonly string[] TIPOS_OPERACION_AUDITADOS =
    {
            "RegistroUsuario",
            "InicioSesion",
            "BloqueoUsuario",
            "DesbloqueoUsuario",
            "AperturaCuenta",
            "CierreCuenta",
            "BloqueoCuenta",
            "Transferencia",
            "PagoServicio",
            "CreacionBeneficiario",
            "ConfirmacionBeneficiario",
            "ProgramacionTransferencia",
            "CancelacionTransferencia",
            "CreacionProveedor"
        };

    public static bool EsOperacionAuditada(string tipoOperacion)
    {
        return TIPOS_OPERACION_AUDITADOS.Contains(tipoOperacion);
    }

    public static string GenerarDescripcionAuditoria(string tipoOperacion, Dictionary<string, object> parametros)
    {
        return tipoOperacion switch
        {
            "RegistroUsuario" => $"Usuario registrado: {parametros.GetValueOrDefault("email", "N/A")}",
            "InicioSesion" => $"Inicio de sesión: {parametros.GetValueOrDefault("email", "N/A")}",
            "Transferencia" => $"Transferencia de {parametros.GetValueOrDefault("monto", "N/A")} desde cuenta {parametros.GetValueOrDefault("cuentaOrigen", "N/A")}",
            "PagoServicio" => $"Pago a {parametros.GetValueOrDefault("proveedor", "N/A")} por {parametros.GetValueOrDefault("monto", "N/A")}",
            _ => $"Operación: {tipoOperacion}"
        };
    }
}
}
