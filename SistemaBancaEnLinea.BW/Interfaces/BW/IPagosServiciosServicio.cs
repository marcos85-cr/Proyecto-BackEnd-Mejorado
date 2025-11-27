using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IPagosServiciosServicio
    {
        Task<List<ProveedorServicio>> ObtenerProveedoresAsync();
        Task<ProveedorServicio?> ObtenerProveedorAsync(int id);
        Task<Transaccion> RealizarPagoAsync(PagoServicioRequest request);
        Task<Transaccion> ProgramarPagoAsync(PagoServicioRequest request);
        Task<List<Transaccion>> ObtenerHistorialPagosAsync(int clienteId);
        Task<bool> ValidarNumeroContratoAsync(int proveedorId, string numeroContrato);
    }

    public class PagoServicioRequest
    {
        public int ClienteId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int ProveedorServicioId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}