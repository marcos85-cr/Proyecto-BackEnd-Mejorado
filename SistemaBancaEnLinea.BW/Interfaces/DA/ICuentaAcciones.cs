using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.DA
{
    public interface ICuentaAcciones
    {
        Task<Cuenta?> ObtenerPorIdAsync(int id);
        Task<Cuenta?> ObtenerPorNumeroAsync(string numero);
        Task<bool> ExisteNumeroAsync(string numero);
        Task<Cuenta> CrearAsync(Cuenta cuenta);
        Task ActualizarAsync(Cuenta cuenta);
        Task<List<Cuenta>> ObtenerPorClienteAsync(int clienteId);
        Task<int> ContarCuentasPorTipoYMonedaAsync(int clienteId, string tipo, string moneda);
        Task<List<Cuenta>> FiltrarCuentasAsync(int? clienteId, string? tipo, string? moneda, string? estado);
    }
}