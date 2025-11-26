using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW.CU
{
    /// <summary>
    /// Caso de Uso: Gestión de Cuentas (RF-B1, RF-B2, RF-B3)
    /// </summary>
    public class GestionCuentasCU
    {
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly ClienteAcciones _clienteAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;

        public GestionCuentasCU(
            CuentaAcciones cuentaAcciones,
            ClienteAcciones clienteAcciones,
            AuditoriaAcciones auditoriaAcciones)
        {
            _cuentaAcciones = cuentaAcciones;
            _clienteAcciones = clienteAcciones;
            _auditoriaAcciones = auditoriaAcciones;
        }

        /// <summary>
        /// RF-B1: Apertura de cuentas
        /// </summary>
        public async Task<Cuenta> AbrirCuentaAsync(int clienteId, string tipo, string moneda, decimal saldoInicial, int usuarioCreadorId)
        {
            // Validar que el cliente existe
            var cliente = await _clienteAcciones.ObtenerPorIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("El cliente no existe.");

            // Validar saldo inicial >= 0
            if (saldoInicial < 0)
                throw new InvalidOperationException("El saldo inicial debe ser mayor o igual a 0.");

            // RF-B1: Validar máximo 3 cuentas del mismo tipo en la misma moneda
            var cantidadCuentas = await _cuentaAcciones.ContarCuentasPorTipoYMonedaAsync(clienteId, tipo, moneda);
            if (cantidadCuentas >= 3)
                throw new InvalidOperationException($"El cliente ya tiene 3 cuentas de tipo {tipo} en {moneda}.");

            // Generar número de cuenta único de 12 dígitos
            var numeroCuenta = await GenerarNumeroCuentaUnicoAsync();

            var cuenta = new Cuenta
            {
                Numero = numeroCuenta,
                Tipo = tipo,
                Moneda = moneda,
                Saldo = saldoInicial,
                Estado = "Activa",
                ClienteId = clienteId
            };

            var cuentaCreada = await _cuentaAcciones.CrearAsync(cuenta);

            // Registrar en auditoría
            await _auditoriaAcciones.RegistrarAsync(
                usuarioCreadorId,
                "AperturaCuenta",
                $"Se abrió cuenta {numeroCuenta} para cliente {clienteId}"
            );

            return cuentaCreada;
        }

        /// <summary>
        /// RF-B3: Bloquear cuenta (Solo administradores)
        /// </summary>
        public async Task BloquearCuentaAsync(int cuentaId, int usuarioId)
        {
            var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId);
            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            cuenta.Estado = "Bloqueada";
            await _cuentaAcciones.ActualizarAsync(cuenta);

            await _auditoriaAcciones.RegistrarAsync(
                usuarioId,
                "BloqueoCuenta",
                $"Se bloqueó la cuenta {cuenta.Numero}"
            );
        }

        /// <summary>
        /// RF-B3: Cerrar cuenta (Solo si saldo = 0 y sin operaciones pendientes)
        /// </summary>
        public async Task CerrarCuentaAsync(int cuentaId, int usuarioId)
        {
            var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId);
            if (cuenta == null)
                throw new InvalidOperationException("La cuenta no existe.");

            if (cuenta.Saldo != 0)
                throw new InvalidOperationException("No se puede cerrar una cuenta con saldo diferente a 0.");

            // TODO: Validar que no tenga operaciones pendientes

            cuenta.Estado = "Cerrada";
            await _cuentaAcciones.ActualizarAsync(cuenta);

            await _auditoriaAcciones.RegistrarAsync(
                usuarioId,
                "CierreCuenta",
                $"Se cerró la cuenta {cuenta.Numero}"
            );
        }

        private async Task<string> GenerarNumeroCuentaUnicoAsync()
        {
            string numero;
            do
            {
                numero = GenerarNumeroAleatorio(12);
            } while (await _cuentaAcciones.ExisteNumeroAsync(numero));

            return numero;
        }

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