using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-A3: Gestión de datos de cliente
    /// </summary>
    public static class ClientesReglas
    {
        public static (bool esValido, string? error) ValidarClienteRequest(ClienteRequest request)
        {
            if (request.UsuarioId <= 0)
                return (false, "El UsuarioId es obligatorio para crear un cliente.");

            return (true, null);
        }

        public static (bool esValido, string? error) ValidarActualizarRequest(ClienteActualizarRequest request) =>
            (true, null); // Todos los campos son opcionales
    }
}