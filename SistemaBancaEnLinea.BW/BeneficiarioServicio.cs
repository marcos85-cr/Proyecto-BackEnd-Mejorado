using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class BeneficiarioServicio : IBeneficiarioServicio
    {
        private readonly BancaContext _context;
        private readonly BeneficiarioAcciones _beneficiarioAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ILogger<BeneficiarioServicio> _logger;

        public BeneficiarioServicio(
            BancaContext context,
            BeneficiarioAcciones beneficiarioAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ILogger<BeneficiarioServicio> logger)
        {
            _context = context;
            _beneficiarioAcciones = beneficiarioAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los beneficiarios del cliente
        /// </summary>
        public async Task<List<Beneficiario>> ObtenerMisBeneficiariosAsync(int clienteId)
        {
            try
            {
                return await _beneficiarioAcciones.ObtenerPorClienteAsync(clienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo beneficiarios del cliente {clienteId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un beneficiario específico
        /// </summary>
        public async Task<Beneficiario?> ObtenerBeneficiarioAsync(int id)
        {
            try
            {
                return await _beneficiarioAcciones.ObtenerPorIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo beneficiario {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// RF-C1: Crea un nuevo beneficiario
        /// </summary>
        public async Task<Beneficiario> CrearBeneficiarioAsync(Beneficiario beneficiario)
        {
            // Validar alias
            if (!BeneficiariosReglas.ValidarAlias(beneficiario.Alias))
                throw new InvalidOperationException(
                    $"El alias debe tener entre {BeneficiariosReglas.LONGITUD_MINIMA_ALIAS} " +
                    $"y {BeneficiariosReglas.LONGITUD_MAXIMA_ALIAS} caracteres.");

            // Validar número de cuenta
            if (!BeneficiariosReglas.ValidarNumeroCuenta(beneficiario.NumeroCuentaDestino))
                throw new InvalidOperationException(
                    $"El número de cuenta debe tener entre {BeneficiariosReglas.LONGITUD_MINIMA_CUENTA} " +
                    $"y {BeneficiariosReglas.LONGITUD_MAXIMA_CUENTA} dígitos.");

            // RF-C1: Validar alias único por cliente
            var aliasExiste = await _beneficiarioAcciones.ExisteAliasParaClienteAsync(
                beneficiario.ClienteId, beneficiario.Alias);
            if (aliasExiste)
                throw new InvalidOperationException("Ya existe un beneficiario con este alias.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    beneficiario.Estado = "Inactivo";
                    beneficiario.FechaCreacion = DateTime.UtcNow;

                    var beneficiarioCreado = await _beneficiarioAcciones.CrearAsync(beneficiario);

                    await transaction.CommitAsync();

                    try
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            beneficiario.ClienteId,
                            "CreacionBeneficiario",
                            $"Beneficiario {beneficiario.Alias} creado. Estado: Inactivo"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning($"Error registrando auditoría pero creación fue exitosa: {auditEx.Message}");
                    }

                    _logger.LogInformation($"Beneficiario {beneficiario.Alias} creado");
                    return beneficiarioCreado;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error creando beneficiario: {ex.Message}");
                    throw new InvalidOperationException($"Error creando beneficiario: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Actualiza el alias de un beneficiario
        /// RF-C2: No se puede editar si tiene operaciones pendientes
        /// </summary>
        public async Task<Beneficiario> ActualizarBeneficiarioAsync(int id, string nuevoAlias)
        {
            var beneficiario = await _beneficiarioAcciones.ObtenerPorIdAsync(id);
            if (beneficiario == null)
                throw new InvalidOperationException("El beneficiario no existe.");

            if (!BeneficiariosReglas.PuedeActualizarse(beneficiario))
                throw new InvalidOperationException(
                    "Solo se pueden actualizar beneficiarios en estado Inactivo.");

            // RF-C2: Validar que no tenga operaciones pendientes
            if (await _beneficiarioAcciones.TieneOperacionesPendientesAsync(id))
                throw new InvalidOperationException(
                    "No se puede editar el beneficiario porque tiene operaciones pendientes.");

            if (!BeneficiariosReglas.ValidarAlias(nuevoAlias))
                throw new InvalidOperationException("El alias no es válido.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    beneficiario.Alias = nuevoAlias;
                    await _beneficiarioAcciones.ActualizarAsync(beneficiario);

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Beneficiario {id} actualizado");
                    return beneficiario;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error actualizando beneficiario {id}: {ex.Message}");
                    throw new InvalidOperationException($"Error actualizando beneficiario: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Elimina un beneficiario
        /// RF-C2: No se puede eliminar si tiene operaciones pendientes
        /// </summary>
        public async Task EliminarBeneficiarioAsync(int id)
        {
            var beneficiario = await _beneficiarioAcciones.ObtenerPorIdAsync(id);
            if (beneficiario == null)
                throw new InvalidOperationException("El beneficiario no existe.");

            // RF-C2: Validar que no tenga operaciones pendientes
            if (await _beneficiarioAcciones.TieneOperacionesPendientesAsync(id))
                throw new InvalidOperationException(
                    "No se puede eliminar el beneficiario porque tiene operaciones pendientes.");

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await _beneficiarioAcciones.EliminarAsync(beneficiario);

                    await transaction.CommitAsync();

                    try
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            beneficiario.ClienteId,
                            "EliminacionBeneficiario",
                            $"Beneficiario {beneficiario.Alias} eliminado"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning($"Error registrando auditoría pero eliminación fue exitosa: {auditEx.Message}");
                    }

                    _logger.LogInformation($"Beneficiario {id} eliminado");
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error eliminando beneficiario {id}: {ex.Message}");
                    throw new InvalidOperationException($"Error eliminando beneficiario: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// RF-C1: Confirma un beneficiario
        /// </summary>
        public async Task<Beneficiario> ConfirmarBeneficiarioAsync(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var beneficiario = await _beneficiarioAcciones.ObtenerPorIdAsync(id);
                    if (beneficiario == null)
                        throw new InvalidOperationException("El beneficiario no existe.");

                    beneficiario.Estado = "Confirmado";
                    await _beneficiarioAcciones.ActualizarAsync(beneficiario);

                    await transaction.CommitAsync();

                    try
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            beneficiario.ClienteId,
                            "ConfirmacionBeneficiario",
                            $"Beneficiario {beneficiario.Alias} confirmado"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning($"Error registrando auditoría pero confirmación fue exitosa: {auditEx.Message}");
                    }

                    _logger.LogInformation($"Beneficiario {id} confirmado");
                    return beneficiario;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error confirmando beneficiario {id}: {ex.Message}");
                    throw new InvalidOperationException($"Error confirmando beneficiario: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Verifica si el beneficiario tiene operaciones pendientes
        /// </summary>
        public async Task<bool> TieneOperacionesPendientesAsync(int beneficiarioId)
        {
            return await _beneficiarioAcciones.TieneOperacionesPendientesAsync(beneficiarioId);
        }
    }
}