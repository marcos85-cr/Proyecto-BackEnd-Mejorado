using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using static SistemaBancaEnLinea.BC.ReglasDeNegocio.ConstantesGenerales;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-A3: Gestión de datos de cliente
    /// </summary>
    public static class ClientesReglas
    {
        // ==================== VALIDACIONES ====================

        public static (bool esValido, string? error) ValidarClienteRequest(ClienteRequest request)
        {
            if (request.UsuarioId <= 0)
                return (false, "El UsuarioId es obligatorio para crear un cliente.");

            return (true, null);
        }

        public static (bool esValido, string? error) ValidarActualizarRequest(ClienteActualizarRequest request)
        {
            // No hay validaciones obligatorias, todos los campos son opcionales
            return (true, null);
        }

        // ==================== MAPEOS A DTOs ====================

        public static ClienteListaDto MapearAListaDto(Cliente cliente)
        {
            var usuario = cliente.UsuarioAsociado;
            return new ClienteListaDto(
                cliente.Id,
                usuario?.Id ?? 0,
                usuario?.Identificacion ?? "",
                usuario?.Nombre ?? "",
                usuario?.Telefono,
                usuario?.Email ?? "",
                cliente.Direccion,
                cliente.FechaNacimiento,
                cliente.Estado,
                cliente.FechaRegistro,
                cliente.Cuentas?.Count(c => c.Estado == ESTADO_CUENTA_ACTIVA) ?? 0,
                cliente.GestorAsignadoId,
                cliente.GestorAsignado?.Nombre ?? cliente.GestorAsignado?.Email
            );
        }

        public static IEnumerable<ClienteListaDto> MapearAListaDto(IEnumerable<Cliente> clientes)
        {
            return clientes.Select(MapearAListaDto);
        }

        public static ClienteDetalleDto MapearADetalleDto(Cliente cliente, List<Cuenta> cuentas)
        {
            var cuentasActivas = cuentas.Count(c => c.Estado == ESTADO_CUENTA_ACTIVA);
            var saldoTotal = cuentas.Where(c => c.Estado == ESTADO_CUENTA_ACTIVA).Sum(c => c.Saldo);
            var usuario = cliente.UsuarioAsociado;
            
            return new ClienteDetalleDto(
                cliente.Id,
                usuario?.Id ?? 0,
                usuario?.Identificacion ?? "",
                usuario?.Nombre ?? "",
                usuario?.Telefono,
                usuario?.Email ?? "",
                cliente.Direccion,
                cliente.FechaNacimiento,
                cliente.Estado,
                cliente.FechaRegistro,
                cliente.UltimaOperacion,
                cuentasActivas,
                saldoTotal,
                usuario != null 
                    ? new UsuarioVinculadoDto(
                        usuario.Id,
                        usuario.Email,
                        usuario.Nombre,
                        usuario.Identificacion,
                        usuario.Telefono,
                        usuario.Rol,
                        usuario.EstaBloqueado)
                    : null,
                cliente.GestorAsignado != null
                    ? new GestorAsignadoDto(
                        cliente.GestorAsignado.Id,
                        cliente.GestorAsignado.Nombre,
                        cliente.GestorAsignado.Email)
                    : null,
                cuentas.Select(c => new CuentaClienteDto(
                    c.Id,
                    c.Numero,
                    c.Tipo,
                    c.Moneda,
                    c.Saldo,
                    c.Estado
                )).ToList()
            );
        }

        public static ClienteCreacionDto MapearACreacionDto(Cliente cliente)
        {
            var usuario = cliente.UsuarioAsociado;
            return new ClienteCreacionDto(
                cliente.Id,
                usuario?.Id ?? 0,
                usuario?.Identificacion ?? "",
                usuario?.Nombre ?? "",
                usuario?.Telefono,
                usuario?.Email ?? "",
                cliente.Direccion,
                cliente.FechaNacimiento,
                cliente.Estado,
                cliente.FechaRegistro,
                cliente.GestorAsignadoId,
                cliente.Cuentas?.Select(c => new CuentaCreadaDto(
                    c.Id,
                    c.Numero,
                    c.Tipo,
                    c.Moneda,
                    c.Saldo,
                    c.Estado
                )).ToList() ?? new List<CuentaCreadaDto>()
            );
        }

        public static ClienteActualizacionDto MapearAActualizacionDto(Cliente cliente)
        {
            return new ClienteActualizacionDto(
                cliente.Id,
                cliente.Direccion,
                cliente.FechaNacimiento,
                cliente.GestorAsignadoId,
                cliente.GestorAsignado?.Nombre ?? cliente.GestorAsignado?.Email
            );
        }

        public static ClientePerfilDto MapearAPerfilDto(Cliente cliente, int cuentasActivas, decimal saldoTotal)
        {
            var usuario = cliente.UsuarioAsociado;
            return new ClientePerfilDto(
                cliente.Id,
                usuario?.Id ?? 0,
                usuario?.Identificacion ?? "",
                usuario?.Nombre ?? "",
                usuario?.Telefono,
                usuario?.Email ?? "",
                cliente.Direccion,
                cliente.FechaNacimiento,
                cliente.Estado,
                cliente.FechaRegistro,
                cliente.UltimaOperacion,
                cuentasActivas,
                saldoTotal
            );
        }
    }
}