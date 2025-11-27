using SistemaBancaEnLinea.BC.Modelos;
using static SistemaBancaEnLinea.BW.TransferenciasServicio;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface ITransferenciasServicio
    {
        // Operaciones básicas
        Task<TransferPrecheck> PreCheckTransferenciaAsync(TransferRequest request);
        Task<Transaccion> EjecutarTransferenciaAsync(TransferRequest request);
        Task<List<Transaccion>> ObtenerMisTransaccionesAsync(int clienteId);
        Task<Transaccion?> ObtenerTransaccionAsync(int id);
        Task<byte[]> DescargarComprobanteAsync(int transaccionId);
        Task<Transaccion> AprobarTransaccionAsync(int transaccionId, int aprobadorId);
        Task<Transaccion> RechazarTransaccionAsync(int transaccionId, int aprobadorId, string razon);

        // Métodos para Gestor
        Task<List<Transaccion>> ObtenerOperacionesPorGestorAsync(int gestorId, DateTime? fechaInicio, DateTime? fechaFin);
        Task<List<Transaccion>> ObtenerOperacionesPendientesPorGestorAsync(int gestorId);
        Task<List<Transaccion>> ObtenerTransaccionesFiltradasAsync(int clienteId, DateTime? fechaInicio, DateTime? fechaFin, string? tipo, string? estado);

        // Métodos para Administrador
        Task<List<Transaccion>> ObtenerOperacionesPorClientesAsync(List<int> clienteIds);
        Task<List<Transaccion>> ObtenerOperacionesPendientesPorClientesAsync(List<int> clienteIds);
        Task<List<Transaccion>> ObtenerOperacionesDeHoyPorClientesAsync(List<int> clienteIds);
        Task<List<Transaccion>> ObtenerTransaccionesConFiltrosAsync(int clienteId, DateTime? fechaInicio, DateTime? fechaFin, string? tipo, string? estado);

        // Otros métodos 

        Task<List<Transaccion>> ObtenerHistorialCuentaAsync(int cuentaId);
        Task<TransaccionesEstadisticas> ObtenerEstadisticasAsync(int clienteId, DateTime fechaInicio, DateTime fechaFin);
        Task<bool> CancelarTransferenciaProgramadaAsync(int transaccionId, int clienteId);
    }

    public class TransferRequest
    {
        public int ClienteId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; }
        public int? BeneficiarioId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "CRC";
        public string? Descripcion { get; set; }
        public bool Programada { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class TransferPrecheck
    {
        public bool PuedeEjecutar { get; set; }
        public bool RequiereAprobacion { get; set; }
        public decimal SaldoAntes { get; set; }
        public decimal Monto { get; set; }
        public decimal Comision { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal SaldoDespues { get; set; }
        public decimal LimiteDisponible { get; set; }
        public string? Mensaje { get; set; }
        public List<string> Errores { get; set; } = new();
    }
}