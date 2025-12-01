using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Reglas para mapeo y validación de proveedores de servicio
    /// </summary>
    public static class ProveedorServicioReglas
    {
        #region ========== MAPEO DTOs ==========

        public static ProveedorListaDto MapearAListaDto(ProveedorServicio p) =>
            new(p.Id.ToString(), p.Nombre, ExtraerTipo(p.Nombre), 
                ObtenerIcono(ExtraerTipo(p.Nombre)), p.ReglaValidacionContrato,
                true, p.CreadoPor?.Nombre ?? "Sistema");

        public static IEnumerable<ProveedorListaDto> MapearAListaDto(IEnumerable<ProveedorServicio> proveedores) =>
            proveedores.Select(MapearAListaDto);

        public static ProveedorDetalleDto MapearADetalleDto(ProveedorServicio p) =>
            new(p.Id.ToString(), p.Nombre, ExtraerTipo(p.Nombre),
                ObtenerIcono(ExtraerTipo(p.Nombre)), p.ReglaValidacionContrato, true);

        public static ProveedorCreacionDto MapearACreacionDto(ProveedorServicio p) =>
            new(p.Id.ToString(), p.Nombre, p.ReglaValidacionContrato);

        public static ValidacionReferenciaDto CrearValidacionDto(bool valida, decimal? monto = null, string? nombre = null) =>
            new(valida, monto, nombre,
                valida ? "Referencia válida" : "Número de referencia no válido");

        #endregion

        #region ========== UTILIDADES ==========

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

        #endregion
    }
}
