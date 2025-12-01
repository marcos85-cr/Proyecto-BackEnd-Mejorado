using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Reglas de negocio para la gestión de usuarios
    /// Incluye validaciones de creación, actualización y bloqueo de usuarios
    /// </summary>
    public static class UsuarioReglas
    {
        #region Constantes

        public const int LONGITUD_MINIMA_NOMBRE = 2;
        public const int LONGITUD_MAXIMA_NOMBRE = 100;
        public const int LONGITUD_MINIMA_IDENTIFICACION = 5;
        public const int LONGITUD_MAXIMA_IDENTIFICACION = 20;
        public const int LONGITUD_MINIMA_TELEFONO = 8;
        public const int LONGITUD_MAXIMA_TELEFONO = 15;

        #endregion

        #region Resultado de Validación

        public class ResultadoValidacion
        {
            public bool EsValido { get; set; }
            public string Mensaje { get; set; } = string.Empty;

            public static ResultadoValidacion Exito() => new() { EsValido = true };
            public static ResultadoValidacion Error(string mensaje) => new() { EsValido = false, Mensaje = mensaje };
        }

        #endregion

        #region Validaciones de Email

        /// <summary>
        /// Valida el formato del email
        /// </summary>
        public static ResultadoValidacion ValidarEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ResultadoValidacion.Error("El email es requerido.");

            if (!ValidacionesComunes.ValidarEmail(email))
                return ResultadoValidacion.Error("El formato del email no es válido.");

            return ResultadoValidacion.Exito();
        }

        /// <summary>
        /// Valida si el email puede ser actualizado (no duplicado)
        /// </summary>
        public static ResultadoValidacion ValidarCambioEmail(string nuevoEmail, string emailActual, bool emailExiste)
        {
            if (string.IsNullOrWhiteSpace(nuevoEmail))
                return ResultadoValidacion.Exito(); // No se está cambiando

            if (nuevoEmail == emailActual)
                return ResultadoValidacion.Exito(); // Es el mismo email

            var validacionFormato = ValidarEmail(nuevoEmail);
            if (!validacionFormato.EsValido)
                return validacionFormato;

            if (emailExiste)
                return ResultadoValidacion.Error("El email ya está registrado.");

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Nombre

        /// <summary>
        /// Valida el nombre del usuario
        /// </summary>
        public static ResultadoValidacion ValidarNombre(string? nombre, bool esRequerido = false)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                if (esRequerido)
                    return ResultadoValidacion.Error("El nombre es requerido.");
                return ResultadoValidacion.Exito();
            }

            if (nombre.Length < LONGITUD_MINIMA_NOMBRE)
                return ResultadoValidacion.Error($"El nombre debe tener al menos {LONGITUD_MINIMA_NOMBRE} caracteres.");

            if (nombre.Length > LONGITUD_MAXIMA_NOMBRE)
                return ResultadoValidacion.Error($"El nombre no puede exceder {LONGITUD_MAXIMA_NOMBRE} caracteres.");

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Identificación

        /// <summary>
        /// Valida la identificación del usuario
        /// </summary>
        public static ResultadoValidacion ValidarIdentificacion(string? identificacion, bool esRequerido = false)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                if (esRequerido)
                    return ResultadoValidacion.Error("La identificación es requerida.");
                return ResultadoValidacion.Exito();
            }

            if (identificacion.Length < LONGITUD_MINIMA_IDENTIFICACION)
                return ResultadoValidacion.Error($"La identificación debe tener al menos {LONGITUD_MINIMA_IDENTIFICACION} caracteres.");

            if (identificacion.Length > LONGITUD_MAXIMA_IDENTIFICACION)
                return ResultadoValidacion.Error($"La identificación no puede exceder {LONGITUD_MAXIMA_IDENTIFICACION} caracteres.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(identificacion, @"^[a-zA-Z0-9\-]+$"))
                return ResultadoValidacion.Error("La identificación solo puede contener letras, números y guiones.");

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Teléfono

        /// <summary>
        /// Valida el teléfono del usuario
        /// </summary>
        public static ResultadoValidacion ValidarTelefono(string? telefono, bool esRequerido = false)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                if (esRequerido)
                    return ResultadoValidacion.Error("El teléfono es requerido.");
                return ResultadoValidacion.Exito();
            }

            var telefonoLimpio = telefono.Replace(" ", "").Replace("-", "").Replace("+", "");

            if (telefonoLimpio.Length < LONGITUD_MINIMA_TELEFONO)
                return ResultadoValidacion.Error($"El teléfono debe tener al menos {LONGITUD_MINIMA_TELEFONO} dígitos.");

            if (telefonoLimpio.Length > LONGITUD_MAXIMA_TELEFONO)
                return ResultadoValidacion.Error($"El teléfono no puede exceder {LONGITUD_MAXIMA_TELEFONO} dígitos.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^\d+$"))
                return ResultadoValidacion.Error("El teléfono solo puede contener números.");

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Rol

        /// <summary>
        /// Valida que el rol sea válido
        /// </summary>
        public static ResultadoValidacion ValidarRol(string? rol, bool esRequerido = true)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                if (esRequerido)
                    return ResultadoValidacion.Error("El rol es requerido.");
                return ResultadoValidacion.Exito();
            }

            if (!AutenticacionReglas.ValidarRol(rol))
                return ResultadoValidacion.Error($"El rol '{rol}' no es válido. Roles permitidos: {string.Join(", ", AutenticacionReglas.ROLES_VALIDOS)}");

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Contraseña

        /// <summary>
        /// Valida el formato de la contraseña
        /// </summary>
        public static ResultadoValidacion ValidarPassword(string? password, bool esRequerido = true)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                if (esRequerido)
                    return ResultadoValidacion.Error("La contraseña es requerida.");
                return ResultadoValidacion.Exito();
            }

            if (!AutenticacionReglas.ValidarFormatoPassword(password))
                return ResultadoValidacion.Error($"La contraseña debe tener al menos {AutenticacionReglas.LONGITUD_MINIMA_PASSWORD} caracteres, incluir mayúscula, número y símbolo especial.");

            return ResultadoValidacion.Exito();
        }

        /// <summary>
        /// Alias de ValidarPassword para uso en español
        /// </summary>
        public static ResultadoValidacion ValidarContrasena(string? contrasena, bool esRequerido = true) =>
            ValidarPassword(contrasena, esRequerido);

        #endregion

        #region Validaciones de Creación de Usuario

        /// <summary>
        /// Valida todos los campos requeridos para crear un usuario
        /// </summary>
        public static ResultadoValidacion ValidarCreacionUsuario(string email, string password, string rol, string? nombre = null, string? identificacion = null, string? telefono = null)
        {
            var validacionEmail = ValidarEmail(email);
            if (!validacionEmail.EsValido)
                return validacionEmail;

            var validacionPassword = ValidarPassword(password);
            if (!validacionPassword.EsValido)
                return validacionPassword;

            var validacionRol = ValidarRol(rol);
            if (!validacionRol.EsValido)
                return validacionRol;

            var validacionNombre = ValidarNombre(nombre);
            if (!validacionNombre.EsValido)
                return validacionNombre;

            var validacionIdentificacion = ValidarIdentificacion(identificacion);
            if (!validacionIdentificacion.EsValido)
                return validacionIdentificacion;

            var validacionTelefono = ValidarTelefono(telefono);
            if (!validacionTelefono.EsValido)
                return validacionTelefono;

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Validaciones de Actualización de Usuario

        /// <summary>
        /// Valida los campos para actualizar un usuario
        /// </summary>
        public static ResultadoValidacion ValidarActualizacionUsuario(
            string? nombre,
            string? telefono,
            string? identificacion,
            string? nuevoEmail,
            string emailActual,
            bool nuevoEmailExiste,
            string? nuevoRol)
        {
            var validacionNombre = ValidarNombre(nombre);
            if (!validacionNombre.EsValido)
                return validacionNombre;

            var validacionTelefono = ValidarTelefono(telefono);
            if (!validacionTelefono.EsValido)
                return validacionTelefono;

            var validacionIdentificacion = ValidarIdentificacion(identificacion);
            if (!validacionIdentificacion.EsValido)
                return validacionIdentificacion;

            var validacionEmail = ValidarCambioEmail(nuevoEmail ?? string.Empty, emailActual, nuevoEmailExiste);
            if (!validacionEmail.EsValido)
                return validacionEmail;

            var validacionRol = ValidarRol(nuevoRol, esRequerido: false);
            if (!validacionRol.EsValido)
                return validacionRol;

            return ResultadoValidacion.Exito();
        }

        #endregion

        #region Lógica de Bloqueo/Desbloqueo

        /// <summary>
        /// Determina si un usuario puede ser bloqueado
        /// </summary>
        public static ResultadoValidacion PuedeBloquearUsuario(Usuario usuario, int idUsuarioAdmin)
        {
            if (usuario == null)
                return ResultadoValidacion.Error("Usuario no encontrado.");

            if (usuario.Id == idUsuarioAdmin)
                return ResultadoValidacion.Error("No puede bloquearse a sí mismo.");

            if (usuario.Rol == "Administrador")
                return ResultadoValidacion.Error("No se puede bloquear a un administrador.");

            if (usuario.EstaBloqueado)
                return ResultadoValidacion.Error("El usuario ya está bloqueado.");

            return ResultadoValidacion.Exito();
        }

        /// <summary>
        /// Determina si un usuario puede ser desbloqueado
        /// </summary>
        public static ResultadoValidacion PuedeDesbloquearUsuario(Usuario usuario)
        {
            if (usuario == null)
                return ResultadoValidacion.Error("Usuario no encontrado.");

            if (!usuario.EstaBloqueado)
                return ResultadoValidacion.Error("El usuario no está bloqueado.");

            return ResultadoValidacion.Exito();
        }

        /// <summary>
        /// Aplica el bloqueo a un usuario
        /// </summary>
        public static void AplicarBloqueo(Usuario usuario)
        {
            usuario.EstaBloqueado = true;
            usuario.FechaBloqueo = DateTime.UtcNow;
        }

        /// <summary>
        /// Aplica el desbloqueo a un usuario
        /// </summary>
        public static void AplicarDesbloqueo(Usuario usuario)
        {
            usuario.EstaBloqueado = false;
            usuario.FechaBloqueo = null;
            usuario.IntentosFallidos = 0;
        }

        #endregion

        #region Aplicar Actualizaciones

        /// <summary>
        /// Aplica las actualizaciones a un usuario (solo los campos que tienen valor).
        /// Nota: Nombre, Telefono e Identificacion se manejan desde Cliente.
        /// </summary>
        public static void AplicarActualizaciones(
            Usuario usuario,
            string? email,
            string? rol)
        {
            if (!string.IsNullOrWhiteSpace(email) && email != usuario.Email)
                usuario.Email = email;

            if (!string.IsNullOrWhiteSpace(rol) && rol != usuario.Rol)
                usuario.Rol = rol;
        }

        #endregion
    }
}
