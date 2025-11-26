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

            var clienteCreado = await _clienteAcciones.CrearAsync(cliente);

            // Registrar en auditoría
            await _auditoriaAcciones.RegistrarAsync(
                cliente.UsuarioAsociado?.Id ?? 0,
                "RegistroUsuario",
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

        public async Task<Cliente> ActualizarClienteAsync(Cliente cliente)
        {
            await _clienteAcciones.ActualizarAsync(cliente);
            return cliente;
        }
    }
}