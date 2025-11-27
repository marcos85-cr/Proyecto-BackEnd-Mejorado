using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BW.Interfaces.BW;

namespace SistemaBancaEnLinea.API
{
    /// <summary>
    /// RF-D3, RF-E3: Servicio en segundo plano que ejecuta transferencias y pagos programados
    /// </summary>
    public class ProgramacionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProgramacionBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(1); // Verificar cada minuto

        public ProgramacionBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ProgramacionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProgramacionBackgroundService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarProgramacionesPendientesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error en ProgramacionBackgroundService: {ex.Message}");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("ProgramacionBackgroundService detenido");
        }

        private async Task ProcesarProgramacionesPendientesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var programacionServicio = scope.ServiceProvider.GetRequiredService<IProgramacionServicio>();

            try
            {
                await programacionServicio.EjecutarProgramacionesPendientesAsync();
                _logger.LogDebug("Verificación de programaciones completada");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error procesando programaciones: {ex.Message}");
            }
        }
    }
}