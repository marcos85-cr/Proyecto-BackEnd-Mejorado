using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IReportesServicio
    {
        // Métodos existentes
        Task<ExtractoCuentaDto> GenerarExtractoCuentaAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);
        Task<byte[]> GenerarExtractoPdfAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);
        Task<byte[]> GenerarExtractoCsvAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin);
        Task<ResumenClienteDto> GenerarResumenClienteAsync(int clienteId);
        Task<byte[]> GenerarResumenClientePdfAsync(int clienteId);

        // Métodos con validación de acceso
        Task<(byte[]? archivo, string? numeroCuenta)> GenerarExtractoConAccesoAsync(
            int cuentaId, DateTime? startDate, DateTime? endDate, string format, int usuarioId, string rol);

        Task<byte[]?> GenerarResumenParaUsuarioAsync(int usuarioId, string format);

        Task<ResumenClienteDto> GenerarResumenParaUsuarioJsonAsync(int usuarioId);

        Task<object> GenerarReporteTransaccionesAsync(
            DateTime inicio, DateTime fin, string? tipo, string? estado, int? clienteId, int usuarioId, string rol);

        Task<object> GenerarEstadisticasDashboardAsync();

        Task<object> GenerarVolumenDiarioAsync(DateTime inicio, DateTime fin, int usuarioId, string rol);

        Task<object> GenerarClientesMasActivosAsync(DateTime inicio, DateTime fin, int top, int usuarioId, string rol);

        Task<object> GenerarTotalesPorPeriodoAsync(DateTime inicio, DateTime fin, int usuarioId, string rol);

        // RF-G1: Exportación de reportes para Administrador
        Task<byte[]> ExportarTotalesPorPeriodoPdfAsync(DateTime inicio, DateTime fin);
        Task<byte[]> ExportarTotalesPorPeriodoExcelAsync(DateTime inicio, DateTime fin);
        Task<byte[]> ExportarTopClientesPdfAsync(DateTime inicio, DateTime fin, int top);
        Task<byte[]> ExportarTopClientesExcelAsync(DateTime inicio, DateTime fin, int top);
        Task<byte[]> ExportarVolumenDiarioPdfAsync(DateTime inicio, DateTime fin);
        Task<byte[]> ExportarVolumenDiarioExcelAsync(DateTime inicio, DateTime fin);

        // RF-G2: Exportación de auditoría para Administrador
        Task<byte[]> ExportarAuditoriaPdfAsync(DateTime inicio, DateTime fin, string? tipoOperacion);
        Task<byte[]> ExportarAuditoriaExcelAsync(DateTime inicio, DateTime fin, string? tipoOperacion);
    }
}
