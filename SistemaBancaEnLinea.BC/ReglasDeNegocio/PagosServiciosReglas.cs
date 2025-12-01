using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-E1: Creación de proveedores de servicio
    /// RF-E2: Validación de contratos
    /// RF-E3: Programación de pagos de servicios
    /// </summary>
    public static class PagosServiciosReglas
    {
        // RF-E1: Límites de proveedor
        public const int LONGITUD_MINIMA_NOMBRE_PROVEEDOR = 3;
        public const int LONGITUD_MAXIMA_NOMBRE_PROVEEDOR = 200;

        // RF-E2: Validación de contrato
        public const int LONGITUD_MINIMA_CONTRATO = 5;
        public const int LONGITUD_MAXIMA_CONTRATO = 50;

        // RF-E3: Comisión de pago de servicio
        public const decimal COMISION_PAGO_SERVICIO = 1000; // 1000 CRC

        public static bool ValidarNombreProveedor(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return false;

            return nombre.Length >= LONGITUD_MINIMA_NOMBRE_PROVEEDOR &&
                   nombre.Length <= LONGITUD_MAXIMA_NOMBRE_PROVEEDOR;
        }

        public static bool ValidarNumeroContrato(string numeroContrato, string reglaValidacion)
        {
            if (string.IsNullOrWhiteSpace(numeroContrato))
                return false;

            if (numeroContrato.Length < LONGITUD_MINIMA_CONTRATO ||
                numeroContrato.Length > LONGITUD_MAXIMA_CONTRATO)
                return false;

            try
            {
                // Validar contra la expresión regular del proveedor
                return System.Text.RegularExpressions.Regex.IsMatch(numeroContrato, reglaValidacion);
            }
            catch
            {
                return false;
            }
        }

        public static decimal ObtenerComision()
        {
            return COMISION_PAGO_SERVICIO;
        }

        #region ========== MAPEO DTOs ==========

        public static ProveedorServicioDto MapearProveedorADto(ProveedorServicio p) =>
            new(p.Id, p.Nombre, p.ReglaValidacionContrato);

        public static IEnumerable<ProveedorServicioDto> MapearProveedoresADto(IEnumerable<ProveedorServicio> proveedores) =>
            proveedores.Select(MapearProveedorADto);

        public static ValidacionContratoResponse CrearRespuestaValidacion(bool esValido, string? nombreProveedor = null) =>
            new(esValido,
                esValido ? "Número de contrato válido." : "El número de contrato no cumple con el formato requerido.",
                nombreProveedor);

        public static PagoRealizadoDto MapearAPagoRealizado(Transaccion t) =>
            new(t.Id, t.ComprobanteReferencia ?? "", t.Monto, t.Comision, 
                t.Monto + t.Comision, t.Estado, t.FechaEjecucion,
                t.ProveedorServicio?.Nombre, t.NumeroContrato ?? "");

        public static PagoProgramadoDto MapearAPagoProgramado(Transaccion t, DateTime fechaProgramada) =>
            new(t.Id, t.Estado, fechaProgramada, t.Monto, t.Comision,
                t.ProveedorServicio?.Nombre);

        public static PagoDetalleDto MapearAPagoDetalle(Transaccion t) =>
            new(t.Id, t.ProveedorServicio?.Nombre, t.NumeroContrato ?? "",
                t.Monto, t.Moneda, t.Comision, t.Estado, t.FechaCreacion,
                t.FechaEjecucion, t.ComprobanteReferencia ?? "", t.Descripcion);

        public static PagoListaDto MapearAPagoLista(Transaccion t) =>
            new(t.Id, t.ProveedorServicio?.Nombre, t.NumeroContrato ?? "",
                t.Monto, t.Moneda, t.Comision, t.Estado, t.FechaCreacion,
                t.FechaEjecucion, t.ComprobanteReferencia ?? "");

        public static IEnumerable<PagoListaDto> MapearAPagoLista(IEnumerable<Transaccion> transacciones) =>
            transacciones.Select(MapearAPagoLista).OrderByDescending(p => p.FechaCreacion);

        public static PagoResumenDto MapearAPagoResumen(Transaccion t) =>
            new(t.Id, t.ProveedorServicio?.Nombre, t.Monto, t.Estado, t.FechaCreacion);

        public static IEnumerable<PagoResumenDto> MapearAPagoResumen(IEnumerable<Transaccion> transacciones) =>
            transacciones.Select(MapearAPagoResumen);

        #endregion
    }
}