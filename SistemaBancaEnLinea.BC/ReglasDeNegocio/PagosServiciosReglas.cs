using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-E1: Creación de proveedores de servicio
    /// RF-E2: Validación de contratos
    /// RF-E3: Programación de pagos de servicios
    /// </summary>
    public static class PagosServiciosReglas
    {
        public const int LONGITUD_MINIMA_NOMBRE_PROVEEDOR = 3;
        public const int LONGITUD_MAXIMA_NOMBRE_PROVEEDOR = 200;
        public const int LONGITUD_MINIMA_CONTRATO = 5;
        public const int LONGITUD_MAXIMA_CONTRATO = 50;
    
        public static bool ValidarNombreProveedor(string nombre) =>
            !string.IsNullOrWhiteSpace(nombre) &&
            nombre.Length >= LONGITUD_MINIMA_NOMBRE_PROVEEDOR &&
            nombre.Length <= LONGITUD_MAXIMA_NOMBRE_PROVEEDOR;

        public static bool ValidarNumeroContrato(string numeroContrato, string reglaValidacion)
        {
            if (string.IsNullOrWhiteSpace(numeroContrato))
                return false;

            if (numeroContrato.Length < LONGITUD_MINIMA_CONTRATO ||
                numeroContrato.Length > LONGITUD_MAXIMA_CONTRATO)
                return false;

            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(numeroContrato, reglaValidacion);
            }
            catch
            {
                return false;
            }
        }

        public static decimal ObtenerComision() => 0;
    }
}