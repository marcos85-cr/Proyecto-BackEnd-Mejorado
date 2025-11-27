using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;

namespace SistemaBancaEnLinea.BW
{
    public class ClienteServicio : IClienteServicio
    {
        private readonly ClienteAcciones _clienteAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly BancaContext _context;

        public ClienteServicio(
            ClienteAcciones clienteAcciones,
            AuditoriaAcciones auditoriaAcciones,
            BancaContext context)
        {
            _clienteAcciones = clienteAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _context = context;
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

            // Actualizar la relación Usuario -> Cliente
            if (cliente.UsuarioAsociado != null)
            {
                cliente.UsuarioAsociado.ClienteId = clienteCreado.Id;
                cliente.UsuarioAsociado.Nombre = cliente.NombreCompleto;
                cliente.UsuarioAsociado.Identificacion = cliente.Identificacion;
                cliente.UsuarioAsociado.Telefono = cliente.Telefono;
                await _context.SaveChangesAsync();
            }

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
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.Beneficiarios)
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuarioId);
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

        public async Task<List<Cliente>> ObtenerTodosAsync()
        {
            return await _clienteAcciones.ObtenerTodosAsync();
        }

        // ========== NUEVOS MÉTODOS PARA GESTOR ==========

        /// <summary>
        /// Obtiene todos los clientes asignados a un gestor específico
        /// </summary>
        public async Task<List<Cliente>> ObtenerClientesPorGestorAsync(int gestorId)
        {
            return await _context.Clientes
                .Where(c => c.GestorAsignadoId == gestorId)
                .Include(c => c.Cuentas)
                .Include(c => c.UsuarioAsociado)
                .OrderBy(c => c.NombreCompleto)
                .ToListAsync();
        }

        /// <summary>
        /// Asigna un cliente a un gestor
        /// </summary>
        public async Task<bool> AsignarClienteAGestorAsync(int clienteId, int gestorId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return false;

            var gestor = await _context.Usuarios.FindAsync(gestorId);
            if (gestor == null || gestor.Rol != "Gestor")
                throw new InvalidOperationException("El usuario no es un gestor válido.");

            cliente.GestorAsignadoId = gestorId;
            await _context.SaveChangesAsync();

            await _auditoriaAcciones.RegistrarAsync(
                gestorId,
                "AsignacionCliente",
                $"Cliente {cliente.NombreCompleto} asignado al gestor"
            );

            return true;
        }

        /// <summary>
        /// Desasigna un cliente de su gestor actual
        /// </summary>
        public async Task<bool> DesasignarClienteDeGestorAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return false;

            var gestorAnterior = cliente.GestorAsignadoId;
            cliente.GestorAsignadoId = null;
            await _context.SaveChangesAsync();

            if (gestorAnterior.HasValue)
            {
                await _auditoriaAcciones.RegistrarAsync(
                    gestorAnterior.Value,
                    "DesasignacionCliente",
                    $"Cliente {cliente.NombreCompleto} desasignado del gestor"
                );
            }

            return true;
        }
    }
}