namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Reglas de validación para cédulas costarricenses
    /// </summary>
    public static class ValidacionCedulaReglas
    {
        /// <summary>
        /// Valida una cédula de identidad costarricense
        /// Formato: N-NNNN-NNNN (9 dígitos)
        /// </summary>
        public static bool ValidarCedulaFisica(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return false;

            // Remover guiones
            var cedulaLimpia = cedula.Replace("-", "").Trim();

            // Debe tener exactamente 9 dígitos
            if (cedulaLimpia.Length != 9)
                return false;

            // Todos deben ser dígitos
            if (!cedulaLimpia.All(char.IsDigit))
                return false;

            // El primer dígito debe ser válido (1-9)
            if (cedulaLimpia[0] == '0')
                return false;

            // Validar formato N-NNNN-NNNN
            if (cedula.Contains("-"))
            {
                var partes = cedula.Split('-');
                if (partes.Length != 3)
                    return false;

                if (partes[0].Length != 1 || partes[1].Length != 4 || partes[2].Length != 4)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Valida una cédula jurídica costarricense
        /// Formato: N-NNN-NNNNNN (10 dígitos)
        /// </summary>
        public static bool ValidarCedulaJuridica(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return false;

            var cedulaLimpia = cedula.Replace("-", "").Trim();

            // Debe tener exactamente 10 dígitos
            if (cedulaLimpia.Length != 10)
                return false;

            // Todos deben ser dígitos
            if (!cedulaLimpia.All(char.IsDigit))
                return false;

            // El primer dígito debe ser 3 (cédula jurídica)
            if (cedulaLimpia[0] != '3')
                return false;

            // Validar formato N-NNN-NNNNNN
            if (cedula.Contains("-"))
            {
                var partes = cedula.Split('-');
                if (partes.Length != 3)
                    return false;

                if (partes[0].Length != 1 || partes[1].Length != 3 || partes[2].Length != 6)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Valida DIMEX (Documento de Identidad Migratorio)
        /// Formato: NNNNNNNNNNN o NNN-NNNNNN-N (11-12 dígitos)
        /// </summary>
        public static bool ValidarDIMEX(string dimex)
        {
            if (string.IsNullOrWhiteSpace(dimex))
                return false;

            var dimexLimpio = dimex.Replace("-", "").Trim();

            // Debe tener entre 11 y 12 dígitos
            if (dimexLimpio.Length < 11 || dimexLimpio.Length > 12)
                return false;

            // Todos deben ser dígitos
            return dimexLimpio.All(char.IsDigit);
        }

        /// <summary>
        /// Valida NITE (Número de Identificación Tributario Especial)
        /// Formato: NN-NNNN-NNNN (10 dígitos)
        /// </summary>
        public static bool ValidarNITE(string nite)
        {
            if (string.IsNullOrWhiteSpace(nite))
                return false;

            var niteLimpio = nite.Replace("-", "").Trim();

            // Debe tener exactamente 10 dígitos
            if (niteLimpio.Length != 10)
                return false;

            // Todos deben ser dígitos
            return niteLimpio.All(char.IsDigit);
        }

        /// <summary>
        /// Valida cualquier tipo de identificación costarricense
        /// </summary>
        public static bool ValidarIdentificacion(string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return false;

            return ValidarCedulaFisica(identificacion) ||
                   ValidarCedulaJuridica(identificacion) ||
                   ValidarDIMEX(identificacion) ||
                   ValidarNITE(identificacion);
        }

        /// <summary>
        /// Determina el tipo de identificación
        /// </summary>
        public static string ObtenerTipoIdentificacion(string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return "Desconocido";

            var limpia = identificacion.Replace("-", "").Trim();

            if (ValidarCedulaFisica(identificacion))
                return "Cédula Física";

            if (ValidarCedulaJuridica(identificacion))
                return "Cédula Jurídica";

            if (ValidarDIMEX(identificacion))
                return "DIMEX";

            if (ValidarNITE(identificacion))
                return "NITE";

            return "Desconocido";
        }

        /// <summary>
        /// Formatea una cédula con guiones
        /// </summary>
        public static string FormatearCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return string.Empty;

            var limpia = cedula.Replace("-", "").Trim();

            if (limpia.Length == 9) // Cédula física
                return $"{limpia.Substring(0, 1)}-{limpia.Substring(1, 4)}-{limpia.Substring(5, 4)}";

            if (limpia.Length == 10) // Cédula jurídica
                return $"{limpia.Substring(0, 1)}-{limpia.Substring(1, 3)}-{limpia.Substring(4, 6)}";

            if (limpia.Length == 11 || limpia.Length == 12) // DIMEX
                return limpia; // DIMEX no tiene formato estándar de guiones

            return cedula;
        }

        #region ========== MAPEO DTOs ==========

        public static Modelos.DTOs.ValidacionCedulaDto CrearValidacionDto(string cedula)
        {
            var esValida = ValidarIdentificacion(cedula);
            var tipo = ObtenerTipoIdentificacion(cedula);
            var formateada = FormatearCedula(cedula);
            var mensaje = esValida ? $"Identificación válida ({tipo})" : "Identificación no válida";
            return new Modelos.DTOs.ValidacionCedulaDto(esValida, tipo, formateada, mensaje);
        }

        public static Modelos.DTOs.IdentificacionDisponibilidadDto CrearDisponibilidadDto(bool existe) =>
            new(!existe, existe ? "Esta identificación ya está registrada" : "Identificación disponible");

        #endregion
    }
}