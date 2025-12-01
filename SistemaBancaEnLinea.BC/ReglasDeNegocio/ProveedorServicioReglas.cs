using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Reglas para validación de proveedores de servicio
    /// </summary>
    public static class ProveedorServicioReglas
    {
        public static string ExtraerTipo(string nombre)
        {
            if (nombre.Contains("Electricidad") || nombre.Contains("ICE") || nombre.Contains("CNFL"))
                return "Electricidad";
            if (nombre.Contains("Agua") || nombre.Contains("AyA"))
                return "Agua";
            if (nombre.Contains("Teléfono") || nombre.Contains("Telefonía") || nombre.Contains("Kolbi") || nombre.Contains("Movistar"))
                return "Telefonía";
            if (nombre.Contains("Internet") || nombre.Contains("Cable"))
                return "Internet";
            if (nombre.Contains("Seguro"))
                return "Seguro";
            if (nombre.Contains("Municipalidad"))
                return "Municipalidades";
            if (nombre.Contains("Judicial") || nombre.Contains("Cobro"))
                return "Cobro Judicial";
            return "Otros";
        }

        public static string ObtenerIcono(string tipo) =>
            tipo switch
            {
                "Electricidad" => "flash-outline",
                "Agua" => "water-outline",
                "Telefonía" => "call-outline",
                "Internet" => "wifi-outline",
                "Cable" => "tv-outline",
                "Seguro" => "shield-checkmark-outline",
                "Municipalidades" => "business-outline",
                "Cobro Judicial" => "document-text-outline",
                _ => "apps-outline"
            };

        public static bool ValidarReferencia(string numeroReferencia, string reglaValidacion)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(numeroReferencia, reglaValidacion);
            }
            catch
            {
                return false;
            }
        }
    }
}
