namespace SistemaBancaEnLinea.BC.Modelos.DTOs
{
    /// <summary>
    /// DTO base para respuestas de la API
    /// </summary>
    public record ApiResponse(bool Success, string Message = "")
    {
        public static ApiResponse Ok(string message = "") => new(true, message);
        public static ApiResponse Fail(string message) => new(false, message);
    }

    /// <summary>
    /// DTO genérico para respuestas con datos
    /// </summary>
    public record ApiResponse<T>(bool Success, string Message, T? Data) where T : class
    {
        public static ApiResponse<T> Ok(T data, string message = "") => new(true, message, data);
        public static ApiResponse<T> Fail(string message) => new(false, message, default);
    }
}
