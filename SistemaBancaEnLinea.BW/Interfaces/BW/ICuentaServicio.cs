using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface ICuentaServicio
    {
        Task<List<Cuenta>> ObtenerMisCuentasAsync(int clienteId);
        Task<Cuenta?> ObtenerCuentaAsync(int id);
        Task<Cuenta?> ObtenerCuentaConRelacionesAsync(int id);
        Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta);
        Task<Cuenta> CrearCuentaAsync(int clienteId, string tipo, string moneda, decimal saldoInicial);
        Task BloquearCuentaAsync(int id);
        Task CerrarCuentaAsync(int id);
        Task<decimal> ObtenerSaldoAsync(int cuentaId);
        Task<List<Cuenta>> ObtenerTodasConRelacionesAsync();
        Task<bool> TieneTransaccionesAsync(int cuentaId);
        Task EliminarCuentaAsync(int cuentaId);
    }
}