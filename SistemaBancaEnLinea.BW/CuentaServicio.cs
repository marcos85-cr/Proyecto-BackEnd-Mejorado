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
        /// Obtiene todas las cuentas del cliente
        /// </summary>
        public async Task<List<Cuenta>> ObtenerMisCuentasAsync(int clienteId)
        {
            try
            {
                return await _cuentaAcciones.ObtenerPorClienteAsync(clienteId);
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
                    FechaApertura = DateTime.UtcNow
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
        /// RF-B3: Bloquea una cuenta
        /// </summary>
        public async Task BloquearCuentaAsync(int id)
        {
            try
            {
                var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(id);
                if (cuenta == null)
                    throw new InvalidOperationException("La cuenta no existe.");

                if (!CuentasReglas.PuedeBloquearse(cuenta))
                    throw new InvalidOperationException("La cuenta no puede ser bloqueada.");

                cuenta.Estado = "Bloqueada";
                await _cuentaAcciones.ActualizarAsync(cuenta);

                await _auditoriaAcciones.RegistrarAsync(
                    0,
                    "BloqueoCuenta",
                    $"Cuenta {cuenta.Numero} bloqueada"
                );

                _logger.LogInformation($"Cuenta {id} bloqueada");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error bloqueando cuenta {id}: {ex.Message}");
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

                cuenta.Estado = "Cerrada";
                await _cuentaAcciones.ActualizarAsync(cuenta);

                await _auditoriaAcciones.RegistrarAsync(
                    0,
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