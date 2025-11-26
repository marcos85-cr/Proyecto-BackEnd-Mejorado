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
    }
}