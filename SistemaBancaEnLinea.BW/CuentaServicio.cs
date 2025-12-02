using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class CuentaServicio : ICuentaServicio
    {
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly ClienteAcciones _clienteAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ILogger<CuentaServicio> _logger;

        public CuentaServicio(
            CuentaAcciones cuentaAcciones,
            ClienteAcciones clienteAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ILogger<CuentaServicio> logger)
        {
            _cuentaAcciones = cuentaAcciones;
            _clienteAcciones = clienteAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las cuentas del cliente con información del cliente
        /// </summary>
        public async Task<List<Cuenta>> ObtenerMisCuentasAsync(int clienteId)
        {
            try
            {
                var cuentas = await _cuentaAcciones.ObtenerPorClienteAsync(clienteId);

                // Cargar información del cliente para cada cuenta
                var cliente = await _clienteAcciones.ObtenerPorIdAsync(clienteId);
                foreach (var cuenta in cuentas)
                {
                    cuenta.Cliente = cliente!;
                }

                return cuentas;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo cuentas del cliente {clienteId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una cuenta específica
        /// </summary>
        public async Task<Cuenta?> ObtenerCuentaAsync(int id)
        {
            try
            {
                return await _cuentaAcciones.ObtenerPorIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo cuenta {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una cuenta con relaciones completas (Cliente, Usuario, Gestor)
        /// </summary>
        public async Task<Cuenta?> ObtenerCuentaConRelacionesAsync(int id)
        {
            try
            {
                return await _cuentaAcciones.ObtenerPorIdConRelacionesAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cuenta con relaciones {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene una cuenta por su número
        /// </summary>
        public async Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta)
        {
            try
            {
                return await _cuentaAcciones.ObtenerPorNumeroAsync(numeroCuenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cuenta por número {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        /// <summary>
        /// RF-B1: Crea una nueva cuenta para el cliente
        /// </summary>
        public async Task<Cuenta> CrearCuentaAsync(int clienteId, string tipo, string moneda, decimal saldoInicial)
        {
            try
            {
                // Validar cliente
                var cliente = await _clienteAcciones.ObtenerPorIdAsync(clienteId);
                if (cliente == null)
                    throw new InvalidOperationException("El cliente no existe.");

                // Validar tipo de cuenta
                if (!CuentasReglas.ValidarTipoCuenta(tipo))
                    throw new InvalidOperationException("El tipo de cuenta no es válido.");

                // Validar moneda
                if (!CuentasReglas.ValidarMoneda(moneda))
                    throw new InvalidOperationException("La moneda no es válida.");

                // Validar saldo inicial
                if (saldoInicial < 0)
                    throw new InvalidOperationException("El saldo inicial no puede ser negativo.");

                // RF-B1: Validar máximo 3 cuentas del mismo tipo y moneda
                var cantidadCuentas = await _cuentaAcciones.ContarCuentasPorTipoYMonedaAsync(clienteId, tipo, moneda);
                if (cantidadCuentas >= CuentasReglas.MAXIMO_CUENTAS_MISMO_TIPO_MONEDA)
                    throw new InvalidOperationException(
                        $"Ya tiene {cantidadCuentas} cuentas de tipo {tipo} en moneda {moneda}.");

                // Generar número de cuenta único
                var numeroCuenta = await GenerarNumeroCuentaUnicoAsync();

                var cuenta = new Cuenta
                {
                    Numero = numeroCuenta,
                    Tipo = tipo,
                    Moneda = moneda,
                    Saldo = saldoInicial,
                    Estado = "Activa",
                    ClienteId = clienteId,
                    FechaApertura = DateTime.UtcNow,
                    Cliente = cliente
                };

                var cuentaCreada = await _cuentaAcciones.CrearAsync(cuenta);

                // Registrar auditoría
                await _auditoriaAcciones.RegistrarAsync(
                    clienteId,
                    "AperturaCuenta",
                    $"Cuenta {numeroCuenta} abierta. Tipo: {tipo}, Moneda: {moneda}, Saldo: {saldoInicial}"
                );

                _logger.LogInformation($"Cuenta {numeroCuenta} creada para cliente {clienteId}");
                return cuentaCreada;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando cuenta: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// RF-B3: Toggle bloqueo/desbloqueo de cuenta
        /// </summary>
        public async Task BloquearCuentaAsync(int id)
        {
            try
            {
                var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(id);
                if (cuenta == null)
                    throw new InvalidOperationException("La cuenta no existe.");

                if (cuenta.Estado == "Cerrada" || cuenta.Estado == "Inactiva")
                    throw new InvalidOperationException($"No se puede modificar una cuenta {cuenta.Estado.ToLower()}.");

                var nuevoEstado = cuenta.Estado == "Bloqueada" ? "Activa" : "Bloqueada";
                var accion = nuevoEstado == "Bloqueada" ? "bloqueada" : "desbloqueada";

                cuenta.Estado = nuevoEstado;
                await _cuentaAcciones.ActualizarAsync(cuenta);

                await _auditoriaAcciones.RegistrarAsync(
                    cuenta.ClienteId,
                    nuevoEstado == "Bloqueada" ? "BloqueoCuenta" : "DesbloqueoCuenta",
                    $"Cuenta {cuenta.Numero} {accion}"
                );

                _logger.LogInformation("Cuenta {Id} {Accion}", id, accion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bloqueando/desbloqueando cuenta {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// RF-B3: Cierra una cuenta
        /// </summary>
        public async Task CerrarCuentaAsync(int id)
        {
            try
            {
                var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(id);
                if (cuenta == null)
                    throw new InvalidOperationException("La cuenta no existe.");

                if (!CuentasReglas.PuedeCerrarse(cuenta))
                    throw new InvalidOperationException("Solo se pueden cerrar cuentas con saldo 0.");

                // RF-B3: Validar que no tenga operaciones pendientes
                if (await _cuentaAcciones.TieneOperacionesPendientesAsync(id))
                    throw new InvalidOperationException("No se puede cerrar una cuenta con operaciones pendientes.");

                cuenta.Estado = "Cerrada";
                await _cuentaAcciones.ActualizarAsync(cuenta);

                await _auditoriaAcciones.RegistrarAsync(
                    cuenta.ClienteId,
                    "CierreCuenta",
                    $"Cuenta {cuenta.Numero} cerrada"
                );

                _logger.LogInformation($"Cuenta {id} cerrada");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cerrando cuenta {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el saldo de una cuenta
        /// </summary>
        public async Task<decimal> ObtenerSaldoAsync(int cuentaId)
        {
            try
            {
                var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId);
                if (cuenta == null)
                    throw new InvalidOperationException("La cuenta no existe.");

                return cuenta.Saldo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo saldo de cuenta {cuentaId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las cuentas con sus relaciones completas (Cliente, Usuario, Gestor)
        /// </summary>
        public async Task<List<Cuenta>> ObtenerTodasConRelacionesAsync()
        {
            try
            {
                return await _cuentaAcciones.ObtenerTodasConRelacionesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todas las cuentas con relaciones");
                throw;
            }
        }

        public async Task<bool> TieneTransaccionesAsync(int cuentaId)
        {
            return await _cuentaAcciones.TieneTransaccionesAsync(cuentaId);
        }

        public async Task EliminarCuentaAsync(int cuentaId)
        {
            var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId);
            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            if (cuenta.Estado == "Inactiva")
                throw new InvalidOperationException("La cuenta ya está inactiva.");

            if (await _cuentaAcciones.TieneTransaccionesAsync(cuentaId))
                throw new InvalidOperationException("No se puede eliminar una cuenta con transacciones asociadas.");

            if (cuenta.Saldo > 0)
                throw new InvalidOperationException("No se puede eliminar una cuenta con saldo positivo.");

            cuenta.Estado = "Inactiva";
            await _cuentaAcciones.ActualizarAsync(cuenta);

            await _auditoriaAcciones.RegistrarAsync(
                cuenta.ClienteId,
                "EliminacionCuenta",
                $"Cuenta {cuenta.Numero} marcada como inactiva"
            );

            _logger.LogInformation("Cuenta {Numero} eliminada (inactiva)", cuenta.Numero);
        }

        /// <summary>
        /// Genera un número de cuenta único
        /// </summary>
        private async Task<string> GenerarNumeroCuentaUnicoAsync()
        {
            string numero;
            int intentos = 0;
            const int INTENTOS_MAXIMOS = 10;

            do
            {
                numero = GenerarNumeroAleatorio(CuentasReglas.LONGITUD_NUMERO_CUENTA);
                intentos++;

                if (intentos > INTENTOS_MAXIMOS)
                    throw new InvalidOperationException("No se pudo generar un número de cuenta único.");

            } while (await _cuentaAcciones.ExisteNumeroAsync(numero));

            return numero;
        }

        /// <summary>
        /// Genera un número aleatorio de la longitud especificada
        /// </summary>
        private string GenerarNumeroAleatorio(int longitud)
        {
            var random = new Random();
            var numero = "";
            for (int i = 0; i < longitud; i++)
            {
                numero += random.Next(0, 10).ToString();
            }
            return numero;
        }
    }
}