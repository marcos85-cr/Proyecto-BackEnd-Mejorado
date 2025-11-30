using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    /// <summary>
    /// Resultado genérico de operación con datos y estado
    /// Usado para retornar resultados desde BW hacia API sin excepciones
    /// </summary>
    public record ResultadoOperacion<T>(
        bool Exitoso,
        T? Datos = default,
        string? Error = null
    )
    {
        /// <summary>
        /// Crea un resultado exitoso con datos
        /// </summary>
        public static ResultadoOperacion<T> Exito(T datos) =>
            new(true, datos, null);

        /// <summary>
        /// Crea un resultado fallido con mensaje de error
        /// </summary>
        public static ResultadoOperacion<T> Fallo(string error) =>
            new(false, default, error);
    }

    /// <summary>
    /// Resultado de operación de login
    /// </summary>
    public record ResultadoLogin(
        bool Exitoso,
        string? Token = null,
        string? Error = null,
        Usuario? Usuario = null
    )
    {
        /// <summary>
        /// Crea un resultado exitoso con token y usuario
        /// </summary>
        public static ResultadoLogin Exito(string token, Usuario usuario) =>
            new(true, token, null, usuario);

        /// <summary>
        /// Crea un resultado fallido con mensaje de error
        /// </summary>
        public static ResultadoLogin Fallido(string error) =>
            new(false, null, error, null);
    }

    /// <summary>
    /// Resultado de validación de reglas de negocio
    /// </summary>
    public record ResultadoValidacion(bool EsValido, string Mensaje = "")
    {
        /// <summary>
        /// Validación exitosa
        /// </summary>
        public static ResultadoValidacion Exito() => new(true);

        /// <summary>
        /// Validación fallida con mensaje
        /// </summary>
        public static ResultadoValidacion Error(string mensaje) => new(false, mensaje);
    }
}
