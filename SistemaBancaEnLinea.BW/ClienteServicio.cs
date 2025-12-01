using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using static SistemaBancaEnLinea.BC.ReglasDeNegocio.ConstantesGenerales;

namespace SistemaBancaEnLinea.BW
{
    public class ClienteServicio : IClienteServicio
    {
        private readonly ClienteAcciones _clienteAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly BancaContext _context;
        private readonly ILogger<ClienteServicio> _logger;

        public ClienteServicio(
            ClienteAcciones clienteAcciones,
            AuditoriaAcciones auditoriaAcciones,
            BancaContext context,
            ILogger<ClienteServicio> logger)
        {
            _clienteAcciones = clienteAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo cliente con vinculación obligatoria a usuario
        /// </summary>
        public async Task<ResultadoOperacion<Cliente>> CrearClienteAsync(ClienteRequest request)
        {
            // Validar request
            var (esValido, error) = ClientesReglas.ValidarClienteRequest(request);
            if (!esValido)
                return ResultadoOperacion<Cliente>.Fallo(error!);

            // Validar usuario (obligatorio)
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null)
                return ResultadoOperacion<Cliente>.Fallo("El usuario especificado no existe.");

            if (usuario.ClienteId.HasValue)
                return ResultadoOperacion<Cliente>.Fallo("El usuario ya está vinculado a otro cliente.");

            if (usuario.Rol != "Cliente")
                return ResultadoOperacion<Cliente>.Fallo("Solo usuarios con rol 'Cliente' pueden vincularse.");

            // Validar gestor si se proporciona
            if (request.GestorId.HasValue)
            {
                var gestor = await _context.Usuarios.FindAsync(request.GestorId.Value);
                if (gestor == null)
                    return ResultadoOperacion<Cliente>.Fallo("El gestor especificado no existe.");

                if (gestor.Rol != "Gestor")
                    return ResultadoOperacion<Cliente>.Fallo("El usuario especificado no es un gestor.");
            }

            // Validar cuentas antes de iniciar la transacción
            var cuentasValidas = new List<CuentaRequest>();
            if (request.Cuentas != null && request.Cuentas.Count > 0)
            {
                foreach (var cuentaRequest in request.Cuentas)
                {
                    if (!EsTipoCuentaValido(cuentaRequest.Tipo))
                        return ResultadoOperacion<Cliente>.Fallo($"Tipo de cuenta inválido: {cuentaRequest.Tipo}. Use: Ahorro o Corriente.");
                    
                    if (!EsMonedaValida(cuentaRequest.Moneda))
                        return ResultadoOperacion<Cliente>.Fallo($"Moneda inválida: {cuentaRequest.Moneda}. Use: CRC o USD.");

                    cuentasValidas.Add(cuentaRequest);
                }
            }

            // Transacción manual con rollback robusto para creación de cliente
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Re-obtener usuario dentro de la transacción
                var usuarioTx = await _context.Usuarios.FindAsync(request.UsuarioId);
                if (usuarioTx == null)
                    return ResultadoOperacion<Cliente>.Fallo("El usuario especificado no existe.");

                // 1. Crear cliente
                var cliente = new Cliente
                {
                    Direccion = request.Direccion,
                    FechaNacimiento = request.FechaNacimiento,
                    Estado = ESTADO_CLIENTE_ACTIVO,
                    FechaRegistro = DateTime.UtcNow,
                    GestorAsignadoId = request.GestorId
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                // 2. Vincular usuario al cliente
                usuarioTx.ClienteId = cliente.Id;
                await _context.SaveChangesAsync();

                // 3. Crear cuentas
                var cuentasCreadas = new List<Cuenta>();
                foreach (var cuentaRequest in cuentasValidas)
                {
                    var cuenta = new Cuenta
                    {
                        Numero = GenerarNumeroCuenta(),
                        Tipo = NormalizarTipoCuenta(cuentaRequest.Tipo),
                        Moneda = cuentaRequest.Moneda.ToUpperInvariant(),
                        Saldo = cuentaRequest.SaldoInicial,
                        Estado = ESTADO_CUENTA_ACTIVA,
                        FechaApertura = DateTime.UtcNow,
                        ClienteId = cliente.Id
                    };

                    _context.Cuentas.Add(cuenta);
                    cuentasCreadas.Add(cuenta);
                }

                if (cuentasCreadas.Count > 0)
                    await _context.SaveChangesAsync();

                // Confirmar la transacción solo si todo fue exitoso
                await transaction.CommitAsync();

                // Auditoría (fuera de la transacción para no afectar rollback si falla)
                try
                {
                    await _auditoriaAcciones.RegistrarAsync(
                        request.UsuarioId,
                        "CreacionCliente",
                        $"Cliente creado para usuario {usuarioTx.Nombre} con {cuentasCreadas.Count} cuentas. Gestor: {request.GestorId}"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning($"Error registrando auditoría pero cliente fue creado: {auditEx.Message}");
                }

                // Cargar relaciones para respuesta
                await _context.Entry(cliente).Reference(c => c.GestorAsignado).LoadAsync();
                await _context.Entry(cliente).Reference(c => c.UsuarioAsociado).LoadAsync();
                cliente.Cuentas = cuentasCreadas;

                return ResultadoOperacion<Cliente>.Exito(cliente);
            }
            catch (Exception ex)
            {
                // Intentar rollback con manejo de errores
                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning($"Transacción de cliente revertida debido a error: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Error crítico haciendo rollback de cliente: {rollbackEx.Message}");
                }

                // Capturar el error interno para más detalle
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return ResultadoOperacion<Cliente>.Fallo($"Error al crear cliente: {innerMessage}");
            }
        }

        private static string GenerarNumeroCuenta()
        {
            // Formato: CR + 10 dígitos aleatorios = 12 caracteres (límite de la columna)
            var random = new Random();
            var digitos = string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(0, 10)));
            return $"CR{digitos}";
        }

        public async Task<Cliente?> ObtenerClienteAsync(int id)
        {
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.UsuarioAsociado)
                .Include(c => c.GestorAsignado)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente?> ObtenerPorUsuarioAsync(int usuarioId)
        {
            // Primero buscar el usuario para obtener su ClienteId
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);
            
            if (usuario == null || !usuario.ClienteId.HasValue)
                return null;
            
            // Luego obtener el cliente con todas sus relaciones
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.Beneficiarios)
                .Include(c => c.GestorAsignado)
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.Id == usuario.ClienteId.Value);
        }

        public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
        {
            return await _clienteAcciones.ObtenerPorIdentificacionAsync(identificacion);
        }

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        public async Task<ResultadoOperacion<Cliente>> ActualizarClienteAsync(int id, ClienteActualizarRequest request)
        {
            var cliente = await ObtenerClienteAsync(id);
            if (cliente == null)
                return ResultadoOperacion<Cliente>.Fallo("Cliente no encontrado.");

            // Validar request
            var (esValido, error) = ClientesReglas.ValidarActualizarRequest(request);
            if (!esValido)
                return ResultadoOperacion<Cliente>.Fallo(error!);

            // Validar gestor si se proporciona
            if (request.GestorId.HasValue && request.GestorId != cliente.GestorAsignadoId)
            {
                var gestor = await _context.Usuarios.FindAsync(request.GestorId.Value);
                if (gestor == null)
                    return ResultadoOperacion<Cliente>.Fallo("El gestor especificado no existe.");

                if (gestor.Rol != "Gestor")
                    return ResultadoOperacion<Cliente>.Fallo("El usuario especificado no es un gestor.");
            }

            // Validar cuentas a crear antes de la transacción
            if (request.Cuentas != null && request.Cuentas.Any())
            {
                foreach (var cuentaReq in request.Cuentas)
                {
                    if (!EsTipoCuentaValido(cuentaReq.Tipo))
                        return ResultadoOperacion<Cliente>.Fallo($"Tipo de cuenta inválido: {cuentaReq.Tipo}. Valores permitidos: {string.Join(", ", TIPOS_CUENTA)}");

                    if (!EsMonedaValida(cuentaReq.Moneda))
                        return ResultadoOperacion<Cliente>.Fallo($"Moneda inválida: {cuentaReq.Moneda}. Valores permitidos: {MONEDA_COLONES}, {MONEDA_DOLARES}");
                }
            }

            // Transacción manual con rollback robusto para actualización de cliente
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Re-obtener cliente dentro de la transacción
                var clienteTx = await _context.Clientes
                    .Include(c => c.UsuarioAsociado)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clienteTx == null)
                    return ResultadoOperacion<Cliente>.Fallo("Cliente no encontrado.");

                // Actualizar campos únicos del cliente
                if (request.Direccion != null)
                    clienteTx.Direccion = request.Direccion;

                if (request.FechaNacimiento.HasValue)
                    clienteTx.FechaNacimiento = request.FechaNacimiento;

                // Cambiar gestor si se proporciona
                if (request.GestorId.HasValue && request.GestorId != clienteTx.GestorAsignadoId)
                    clienteTx.GestorAsignadoId = request.GestorId;

                await _context.SaveChangesAsync();

                // Crear nuevas cuentas si se proporcionan (solo crea, no actualiza existentes)
                if (request.Cuentas != null && request.Cuentas.Any())
                {
                    foreach (var cuentaReq in request.Cuentas)
                    {
                        var nuevaCuenta = new Cuenta
                        {
                            Numero = GenerarNumeroCuenta(),
                            Tipo = NormalizarTipoCuenta(cuentaReq.Tipo),
                            Moneda = cuentaReq.Moneda.ToUpperInvariant(),
                            Saldo = cuentaReq.SaldoInicial,
                            Estado = ESTADO_CUENTA_ACTIVA,
                            ClienteId = clienteTx.Id,
                            FechaApertura = DateTime.UtcNow
                        };

                        _context.Cuentas.Add(nuevaCuenta);
                    }

                    await _context.SaveChangesAsync();
                }

                // Confirmar la transacción solo si todo fue exitoso
                await transaction.CommitAsync();

                // Auditoría (fuera de la transacción para no afectar rollback si falla)
                try
                {
                    await _auditoriaAcciones.RegistrarAsync(
                        clienteTx.UsuarioAsociado?.Id ?? 0,
                        "ActualizacionCliente",
                        $"Cliente {clienteTx.Id} actualizado"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning($"Error registrando auditoría pero cliente fue actualizado: {auditEx.Message}");
                }

                // Recargar con relaciones
                await _context.Entry(clienteTx).Reference(c => c.GestorAsignado).LoadAsync();
                await _context.Entry(clienteTx).Collection(c => c.Cuentas!).LoadAsync();

                return ResultadoOperacion<Cliente>.Exito(clienteTx);
            }
            catch (Exception ex)
            {
                // Intentar rollback con manejo de errores
                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning($"Transacción de actualización de cliente revertida debido a error: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Error crítico haciendo rollback de actualización de cliente: {rollbackEx.Message}");
                }

                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return ResultadoOperacion<Cliente>.Fallo($"Error al actualizar cliente: {innerMessage}");
            }
        }

        /// <summary>
        /// Elimina un cliente (solo si no tiene cuentas activas)
        /// </summary>
        public async Task<ResultadoOperacion<bool>> EliminarClienteAsync(int id)
        {
            var cliente = await ObtenerClienteAsync(id);
            if (cliente == null)
                return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

            // Validar que no tenga cuentas activas
            if (cliente.Cuentas?.Any(c => c.Estado == "Activa") == true)
                return ResultadoOperacion<bool>.Fallo("No se puede eliminar un cliente con cuentas activas.");

            // Transacción manual con rollback robusto para eliminación de cliente
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var clienteTx = await _context.Clientes
                    .Include(c => c.UsuarioAsociado)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clienteTx == null)
                    return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

                // Desvincular usuario si existe
                if (clienteTx.UsuarioAsociado != null)
                {
                    clienteTx.UsuarioAsociado.ClienteId = null;
                    await _context.SaveChangesAsync();
                }

                _context.Clientes.Remove(clienteTx);
                await _context.SaveChangesAsync();

                // Confirmar la transacción solo si todo fue exitoso
                await transaction.CommitAsync();

                // Auditoría (fuera de la transacción para no afectar rollback si falla)
                try
                {
                    await _auditoriaAcciones.RegistrarAsync(
                        0,
                        "EliminacionCliente",
                        $"Cliente {id} eliminado"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning($"Error registrando auditoría pero cliente fue eliminado: {auditEx.Message}");
                }

                return ResultadoOperacion<bool>.Exito(true);
            }
            catch (Exception ex)
            {
                // Intentar rollback con manejo de errores
                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning($"Transacción de eliminación de cliente revertida debido a error: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Error crítico haciendo rollback de eliminación de cliente: {rollbackEx.Message}");
                }

                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return ResultadoOperacion<bool>.Fallo($"Error al eliminar cliente: {innerMessage}");
            }
        }

        public async Task<bool> ExisteIdentificacionAsync(string identificacion)
        {
            return await _clienteAcciones.ExisteIdentificacionAsync(identificacion);
        }

        public async Task<List<Cliente>> ObtenerTodosAsync()
        {
            return await _context.Clientes
                .Include(c => c.Cuentas)
                .Include(c => c.UsuarioAsociado)
                .Include(c => c.GestorAsignado)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<List<Cliente>> ObtenerClientesPorGestorAsync(int gestorId)
        {
            return await _context.Clientes
                .Where(c => c.GestorAsignadoId == gestorId)
                .Include(c => c.Cuentas)
                .Include(c => c.UsuarioAsociado)
                .OrderBy(c => c.UsuarioAsociado != null ? c.UsuarioAsociado.Nombre : "")
                .ToListAsync();
        }

        public async Task<ResultadoOperacion<bool>> AsignarClienteAGestorAsync(int clienteId, int gestorId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

            var gestor = await _context.Usuarios.FindAsync(gestorId);
            if (gestor == null || gestor.Rol != "Gestor")
                return ResultadoOperacion<bool>.Fallo("El usuario no es un gestor válido.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var clienteTx = await _context.Clientes.FindAsync(clienteId);
                    if (clienteTx == null)
                        return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

                    clienteTx.GestorAsignadoId = gestorId;
                    await _context.SaveChangesAsync();

                    await _auditoriaAcciones.RegistrarAsync(
                        gestorId,
                        "AsignacionCliente",
                        $"Cliente {clienteId} asignado al gestor"
                    );

                    await transaction.CommitAsync();
                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al asignar gestor: {innerMessage}");
                }
            });
        }

        public async Task<ResultadoOperacion<bool>> DesasignarClienteDeGestorAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

            var gestorAnterior = cliente.GestorAsignadoId;

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var clienteTx = await _context.Clientes.FindAsync(clienteId);
                    if (clienteTx == null)
                        return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

                    clienteTx.GestorAsignadoId = null;
                    await _context.SaveChangesAsync();

                    if (gestorAnterior.HasValue)
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            gestorAnterior.Value,
                            "DesasignacionCliente",
                            $"Cliente {clienteId} desasignado del gestor"
                        );
                    }

                    await transaction.CommitAsync();
                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al desasignar gestor: {innerMessage}");
                }
            });
        }

        public async Task<ResultadoOperacion<bool>> VincularUsuarioAsync(int clienteId, int usuarioId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return ResultadoOperacion<bool>.Fallo("Cliente no encontrado.");

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

            if (usuario.ClienteId.HasValue)
                return ResultadoOperacion<bool>.Fallo("El usuario ya está vinculado a otro cliente.");

            if (usuario.Rol != "Cliente")
                return ResultadoOperacion<bool>.Fallo("Solo usuarios con rol 'Cliente' pueden vincularse.");

            // Verificar que el cliente no tenga otro usuario vinculado
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.ClienteId == clienteId);
            if (usuarioExistente != null)
                return ResultadoOperacion<bool>.Fallo("El cliente ya tiene un usuario vinculado.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioTx = await _context.Usuarios.FindAsync(usuarioId);
                    if (usuarioTx == null)
                        return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

                    usuarioTx.ClienteId = clienteId;
                    await _context.SaveChangesAsync();

                    await _auditoriaAcciones.RegistrarAsync(
                        usuarioId,
                        "VinculacionUsuarioCliente",
                        $"Usuario {usuarioTx.Email} vinculado a cliente {clienteId}"
                    );

                    await transaction.CommitAsync();
                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al vincular usuario: {innerMessage}");
                }
            });
        }

        public async Task<ResultadoOperacion<bool>> DesvincularUsuarioAsync(int clienteId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.ClienteId == clienteId);
            if (usuario == null)
                return ResultadoOperacion<bool>.Fallo("El cliente no tiene usuario vinculado.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioTx = await _context.Usuarios.FirstOrDefaultAsync(u => u.ClienteId == clienteId);
                    if (usuarioTx == null)
                        return ResultadoOperacion<bool>.Fallo("El cliente no tiene usuario vinculado.");

                    var email = usuarioTx.Email;
                    usuarioTx.ClienteId = null;
                    await _context.SaveChangesAsync();

                    await _auditoriaAcciones.RegistrarAsync(
                        usuarioTx.Id,
                        "DesvinculacionUsuarioCliente",
                        $"Usuario {email} desvinculado del cliente"
                    );

                    await transaction.CommitAsync();
                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al desvincular usuario: {innerMessage}");
                }
            });
        }
    }
}