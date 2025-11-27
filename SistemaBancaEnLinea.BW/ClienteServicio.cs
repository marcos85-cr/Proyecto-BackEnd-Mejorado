using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.BW
{
    public class ClienteServicio : IClienteServicio
    {
        private readonly ClienteAcciones _clienteAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;

        public ClienteServicio(ClienteAcciones clienteAcciones, AuditoriaAcciones auditoriaAcciones)
        {
            _clienteAcciones = clienteAcciones;
            _auditoriaAcciones = auditoriaAcciones;
        }

        public async Task<Cliente> CrearClienteAsync(Cliente cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Identificacion))
                throw new InvalidOperationException("La identificación es requerida.");

            if (string.IsNullOrWhiteSpace(cliente.NombreCompleto))
                throw new InvalidOperationException("El nombre completo es requerido.");

            // Verificar que la identificación sea única
            var clienteExistente = await _clienteAcciones.ObtenerPorIdentificacionAsync(cliente.Identificacion);
            if (clienteExistente != null)
                throw new InvalidOperationException("Ya existe un cliente con esta identificación.");

            cliente.FechaRegistro = DateTime.UtcNow;
            cliente.Estado = "Activo";

            var clienteCreado = await _clienteAcciones.CrearAsync(cliente);

            // Registrar en auditoría
            await _auditoriaAcciones.RegistrarAsync(
                cliente.UsuarioAsociado?.Id ?? 0,
                "RegistroCliente",
                $"Se registró nuevo cliente: {cliente.NombreCompleto}"
            );

            return clienteCreado;
        }

        public async Task<Cliente?> ObtenerClienteAsync(int id)
        {
            return await _clienteAcciones.ObtenerPorIdAsync(id);
        }

        public async Task<Cliente?> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var clientes = await _clienteAcciones.ObtenerTodosAsync();
            return clientes.FirstOrDefault(c => c.UsuarioAsociado?.Id == usuarioId);
        }

        public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
        {
            return await _clienteAcciones.ObtenerPorIdentificacionAsync(identificacion);
        }

        public async Task<Cliente> ActualizarClienteAsync(Cliente cliente)
        {
            var existente = await _clienteAcciones.ObtenerPorIdAsync(cliente.Id);
            if (existente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            existente.NombreCompleto = cliente.NombreCompleto;
            existente.Telefono = cliente.Telefono;
            existente.Correo = cliente.Correo;

            await _clienteAcciones.ActualizarAsync(existente);

            await _auditoriaAcciones.RegistrarAsync(
                existente.UsuarioAsociado?.Id ?? 0,
                "ActualizacionCliente",
                $"Cliente {existente.NombreCompleto} actualizado"
            );

            return existente;
        }

        public async Task<bool> ExisteIdentificacionAsync(string identificacion)
        {
            return await _clienteAcciones.ExisteIdentificacionAsync(identificacion);
        }
    }
}