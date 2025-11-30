using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IReportesServicio
    {
        /// <summary>
        /// Genera extracto de cuenta en formato DTO
        /// </summary>
        Task<ExtractoCuentaDto> GenerarExtractoCuentaAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Genera extracto de cuenta en PDF (bytes)
        /// </summary>
        Task<byte[]> GenerarExtractoPdfAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Genera extracto de cuenta en CSV (bytes)
        /// </summary>
        Task<byte[]> GenerarExtractoCsvAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Genera resumen de cliente
        /// </summary>
        Task<ResumenClienteDto> GenerarResumenClienteAsync(int clienteId);

        /// <summary>
        /// Genera resumen de cliente en PDF
        /// </summary>
        Task<byte[]> GenerarResumenClientePdfAsync(int clienteId);
    }
}
