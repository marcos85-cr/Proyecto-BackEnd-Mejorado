using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA.Acciones;
using System.Text;

namespace SistemaBancaEnLinea.BW
{
    public class ReportesServicio : IReportesServicio
    {
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly ClienteAcciones _clienteAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly ILogger<ReportesServicio> _logger;

        public ReportesServicio(
            CuentaAcciones cuentaAcciones,
            ClienteAcciones clienteAcciones,
            TransaccionAcciones transaccionAcciones,
            ILogger<ReportesServicio> logger)
        {
            _cuentaAcciones = cuentaAcciones;
            _clienteAcciones = clienteAcciones;
            _transaccionAcciones = transaccionAcciones;
            _logger = logger;
        }

        public async Task<ExtractoCuentaDto> GenerarExtractoCuentaAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin)
        {
            var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId) 
                ?? throw new InvalidOperationException("Cuenta no encontrada.");
            
            var cliente = await _clienteAcciones.ObtenerPorIdAsync(cuenta.ClienteId);
            var transacciones = await _transaccionAcciones.FiltrarHistorialAsync(
                null, cuentaId, fechaInicio, fechaFin, null, null);

            var movimientos = transacciones
                .OrderBy(t => t.FechaCreacion)
                .Select(t => new MovimientoDto(
                    t.FechaCreacion,
                    t.Tipo,
                    t.Descripcion,
                    t.CuentaOrigenId == cuentaId ? -(t.Monto + t.Comision) : t.Monto,
                    t.CuentaOrigenId == cuentaId ? t.Comision : 0,
                    t.ComprobanteReferencia,
                    t.Estado
                ))
                .ToList();

            var totalDebitos = transacciones
                .Where(t => t.CuentaOrigenId == cuentaId)
                .Sum(t => t.Monto + t.Comision);

            var totalCreditos = transacciones
                .Where(t => t.CuentaDestinoId == cuentaId)
                .Sum(t => t.Monto);

            var saldoInicial = cuenta.Saldo + totalDebitos - totalCreditos;

            return new ExtractoCuentaDto(
                new CuentaListaDto(
                    cuenta.Id,
                    cuenta.Numero,
                    cuenta.Tipo,
                    cuenta.Moneda,
                    cuenta.Saldo,
                    cuenta.Estado,
                    cuenta.FechaApertura,
                    cliente?.UsuarioAsociado?.Nombre
                ),
                new PeriodoDto(fechaInicio, fechaFin),
                new SaldoResumenDto(saldoInicial, cuenta.Saldo),
                movimientos,
                new ResumenExtractoDto(
                    transacciones.Count,
                    totalDebitos,
                    totalCreditos
                )
            );
        }

        public async Task<byte[]> GenerarExtractoPdfAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin)
        {
            var extracto = await GenerarExtractoCuentaAsync(cuentaId, fechaInicio, fechaFin);

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Encabezado
            document.Add(new Paragraph("SISTEMA BANCA EN LÍNEA")
                .SetFont(fontBold)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Estado de Cuenta")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Información de cuenta
            var infoCuenta = new Table(2).UseAllAvailableWidth();
            infoCuenta.AddCell(CrearCeldaInfo("Número de Cuenta:", fontBold));
            infoCuenta.AddCell(CrearCeldaInfo(extracto.Cuenta.Numero, fontNormal));
            infoCuenta.AddCell(CrearCeldaInfo("Titular:", fontBold));
            infoCuenta.AddCell(CrearCeldaInfo(extracto.Cuenta.Titular ?? "N/A", fontNormal));
            infoCuenta.AddCell(CrearCeldaInfo("Tipo:", fontBold));
            infoCuenta.AddCell(CrearCeldaInfo(extracto.Cuenta.Tipo, fontNormal));
            infoCuenta.AddCell(CrearCeldaInfo("Moneda:", fontBold));
            infoCuenta.AddCell(CrearCeldaInfo(extracto.Cuenta.Moneda, fontNormal));
            infoCuenta.AddCell(CrearCeldaInfo("Período:", fontBold));
            infoCuenta.AddCell(CrearCeldaInfo($"{extracto.Periodo.Desde:dd/MM/yyyy} - {extracto.Periodo.Hasta:dd/MM/yyyy}", fontNormal));
            document.Add(infoCuenta);

            document.Add(new Paragraph().SetMarginBottom(10));

            // Resumen de saldos
            var saldoTable = new Table(2).UseAllAvailableWidth();
            saldoTable.AddCell(CrearCeldaResaltada("Saldo Inicial:", fontBold));
            saldoTable.AddCell(CrearCeldaResaltada($"{extracto.Cuenta.Moneda} {extracto.Saldo.Inicial:N2}", fontNormal));
            saldoTable.AddCell(CrearCeldaResaltada("Saldo Final:", fontBold));
            saldoTable.AddCell(CrearCeldaResaltada($"{extracto.Cuenta.Moneda} {extracto.Saldo.Final:N2}", fontNormal));
            document.Add(saldoTable);

            document.Add(new Paragraph().SetMarginBottom(10));

            // Tabla de movimientos
            document.Add(new Paragraph("DETALLE DE MOVIMIENTOS")
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetMarginTop(15)
                .SetMarginBottom(10));

            var movTable = new Table(new float[] { 2, 2, 4, 2, 2, 2 }).UseAllAvailableWidth();
            
            // Encabezados
            movTable.AddHeaderCell(CrearCeldaEncabezado("Fecha", fontBold));
            movTable.AddHeaderCell(CrearCeldaEncabezado("Tipo", fontBold));
            movTable.AddHeaderCell(CrearCeldaEncabezado("Descripción", fontBold));
            movTable.AddHeaderCell(CrearCeldaEncabezado("Monto", fontBold));
            movTable.AddHeaderCell(CrearCeldaEncabezado("Referencia", fontBold));
            movTable.AddHeaderCell(CrearCeldaEncabezado("Estado", fontBold));

            foreach (var mov in extracto.Movimientos)
            {
                movTable.AddCell(CrearCeldaDato(mov.Fecha.ToString("dd/MM/yy"), fontNormal));
                movTable.AddCell(CrearCeldaDato(mov.Tipo, fontNormal));
                movTable.AddCell(CrearCeldaDato(mov.Descripcion ?? "-", fontNormal));
                
                var montoCell = CrearCeldaDato($"{mov.Monto:N2}", fontNormal);
                if (mov.Monto < 0)
                    montoCell.SetFontColor(new DeviceRgb(220, 53, 69)); // Rojo para débitos
                else
                    montoCell.SetFontColor(new DeviceRgb(40, 167, 69)); // Verde para créditos
                movTable.AddCell(montoCell);
                
                movTable.AddCell(CrearCeldaDato(mov.Referencia ?? "-", fontNormal));
                movTable.AddCell(CrearCeldaDato(mov.Estado, fontNormal));
            }

            document.Add(movTable);

            // Resumen
            document.Add(new Paragraph().SetMarginTop(15));
            var resumenTable = new Table(2).UseAllAvailableWidth();
            resumenTable.AddCell(CrearCeldaInfo("Total Transacciones:", fontBold));
            resumenTable.AddCell(CrearCeldaInfo(extracto.Resumen.TotalTransacciones.ToString(), fontNormal));
            resumenTable.AddCell(CrearCeldaInfo("Total Débitos:", fontBold));
            resumenTable.AddCell(CrearCeldaInfo($"{extracto.Cuenta.Moneda} {extracto.Resumen.TotalDebitos:N2}", fontNormal));
            resumenTable.AddCell(CrearCeldaInfo("Total Créditos:", fontBold));
            resumenTable.AddCell(CrearCeldaInfo($"{extracto.Cuenta.Moneda} {extracto.Resumen.TotalCreditos:N2}", fontNormal));
            document.Add(resumenTable);

            // Pie de página
            document.Add(new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFont(fontNormal)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30));

            document.Close();
            return ms.ToArray();
        }

        public async Task<byte[]> GenerarExtractoCsvAsync(int cuentaId, DateTime fechaInicio, DateTime fechaFin)
        {
            var extracto = await GenerarExtractoCuentaAsync(cuentaId, fechaInicio, fechaFin);

            var sb = new StringBuilder();
            
            // Encabezado
            sb.AppendLine($"Estado de Cuenta - {extracto.Cuenta.Numero}");
            sb.AppendLine($"Titular,{extracto.Cuenta.Titular}");
            sb.AppendLine($"Período,{extracto.Periodo.Desde:dd/MM/yyyy} - {extracto.Periodo.Hasta:dd/MM/yyyy}");
            sb.AppendLine($"Moneda,{extracto.Cuenta.Moneda}");
            sb.AppendLine($"Saldo Inicial,{extracto.Saldo.Inicial}");
            sb.AppendLine($"Saldo Final,{extracto.Saldo.Final}");
            sb.AppendLine();

            // Encabezados de movimientos
            sb.AppendLine("Fecha,Tipo,Descripción,Monto,Comisión,Referencia,Estado");

            // Datos
            foreach (var mov in extracto.Movimientos)
            {
                sb.AppendLine($"{mov.Fecha:dd/MM/yyyy HH:mm},{mov.Tipo},\"{mov.Descripcion ?? ""}\",{mov.Monto},{mov.Comision},{mov.Referencia ?? ""},{mov.Estado}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total Transacciones,{extracto.Resumen.TotalTransacciones}");
            sb.AppendLine($"Total Débitos,{extracto.Resumen.TotalDebitos}");
            sb.AppendLine($"Total Créditos,{extracto.Resumen.TotalCreditos}");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<ResumenClienteDto> GenerarResumenClienteAsync(int clienteId)
        {
            var cliente = await _clienteAcciones.ObtenerPorIdAsync(clienteId)
                ?? throw new InvalidOperationException("Cliente no encontrado.");

            var cuentas = await _cuentaAcciones.ObtenerPorClienteAsync(clienteId);
            var transacciones = await _transaccionAcciones.ObtenerPorClienteAsync(clienteId);

            var hoy = DateTime.UtcNow;
            var ultimoMes = hoy.AddMonths(-1);

            return new ResumenClienteDto(
                new ClienteInfoDto(
                    cliente.Id,
                    cliente.UsuarioAsociado?.Nombre ?? "",
                    cliente.UsuarioAsociado?.Identificacion ?? "",
                    cliente.UsuarioAsociado?.Email ?? "",
                    cliente.UsuarioAsociado?.Telefono,
                    cliente.FechaRegistro
                ),
                new ResumenCuentasDto(
                    cuentas.Count,
                    cuentas.Count(c => c.Estado == "Activa"),
                    cuentas.Where(c => c.Moneda == "CRC").Sum(c => c.Saldo),
                    cuentas.Where(c => c.Moneda == "USD").Sum(c => c.Saldo),
                    cuentas.Select(c => new CuentaListaDto(
                        c.Id, c.Numero, c.Tipo, c.Moneda, c.Saldo, c.Estado, c.FechaApertura
                    )).ToList()
                ),
                new ResumenActividadDto(
                    transacciones.Count,
                    transacciones.Count(t => t.FechaCreacion >= ultimoMes),
                    transacciones.Where(t => t.FechaCreacion >= ultimoMes && t.Estado == "Exitosa").Sum(t => t.Monto),
                    transacciones.OrderByDescending(t => t.FechaCreacion).FirstOrDefault()?.FechaCreacion
                )
            );
        }

        public async Task<byte[]> GenerarResumenClientePdfAsync(int clienteId)
        {
            var resumen = await GenerarResumenClienteAsync(clienteId);

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Encabezado
            document.Add(new Paragraph("SISTEMA BANCA EN LÍNEA")
                .SetFont(fontBold)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Resumen de Cliente")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Información del cliente
            document.Add(new Paragraph("INFORMACIÓN DEL CLIENTE")
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetMarginBottom(10));

            var infoTable = new Table(2).UseAllAvailableWidth();
            infoTable.AddCell(CrearCeldaInfo("Nombre:", fontBold));
            infoTable.AddCell(CrearCeldaInfo(resumen.Cliente.Nombre, fontNormal));
            infoTable.AddCell(CrearCeldaInfo("Identificación:", fontBold));
            infoTable.AddCell(CrearCeldaInfo(resumen.Cliente.Identificacion, fontNormal));
            infoTable.AddCell(CrearCeldaInfo("Correo:", fontBold));
            infoTable.AddCell(CrearCeldaInfo(resumen.Cliente.Correo, fontNormal));
            infoTable.AddCell(CrearCeldaInfo("Teléfono:", fontBold));
            infoTable.AddCell(CrearCeldaInfo(resumen.Cliente.Telefono ?? "N/A", fontNormal));
            infoTable.AddCell(CrearCeldaInfo("Fecha Registro:", fontBold));
            infoTable.AddCell(CrearCeldaInfo(resumen.Cliente.FechaRegistro.ToString("dd/MM/yyyy"), fontNormal));
            document.Add(infoTable);

            // Resumen de cuentas
            document.Add(new Paragraph("RESUMEN DE CUENTAS")
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10));

            var cuentasResumen = new Table(2).UseAllAvailableWidth();
            cuentasResumen.AddCell(CrearCeldaInfo("Total Cuentas:", fontBold));
            cuentasResumen.AddCell(CrearCeldaInfo(resumen.Cuentas.Total.ToString(), fontNormal));
            cuentasResumen.AddCell(CrearCeldaInfo("Cuentas Activas:", fontBold));
            cuentasResumen.AddCell(CrearCeldaInfo(resumen.Cuentas.Activas.ToString(), fontNormal));
            cuentasResumen.AddCell(CrearCeldaInfo("Saldo Total CRC:", fontBold));
            cuentasResumen.AddCell(CrearCeldaInfo($"₡ {resumen.Cuentas.SaldoTotalCRC:N2}", fontNormal));
            cuentasResumen.AddCell(CrearCeldaInfo("Saldo Total USD:", fontBold));
            cuentasResumen.AddCell(CrearCeldaInfo($"$ {resumen.Cuentas.SaldoTotalUSD:N2}", fontNormal));
            document.Add(cuentasResumen);

            // Detalle de cuentas
            if (resumen.Cuentas.Detalle.Any())
            {
                document.Add(new Paragraph("Detalle de Cuentas:")
                    .SetFont(fontBold)
                    .SetFontSize(10)
                    .SetMarginTop(10));

                var cuentasTable = new Table(new float[] { 3, 2, 1, 2, 2 }).UseAllAvailableWidth();
                cuentasTable.AddHeaderCell(CrearCeldaEncabezado("Número", fontBold));
                cuentasTable.AddHeaderCell(CrearCeldaEncabezado("Tipo", fontBold));
                cuentasTable.AddHeaderCell(CrearCeldaEncabezado("Moneda", fontBold));
                cuentasTable.AddHeaderCell(CrearCeldaEncabezado("Saldo", fontBold));
                cuentasTable.AddHeaderCell(CrearCeldaEncabezado("Estado", fontBold));

                foreach (var cuenta in resumen.Cuentas.Detalle)
                {
                    cuentasTable.AddCell(CrearCeldaDato(cuenta.Numero, fontNormal));
                    cuentasTable.AddCell(CrearCeldaDato(cuenta.Tipo, fontNormal));
                    cuentasTable.AddCell(CrearCeldaDato(cuenta.Moneda, fontNormal));
                    cuentasTable.AddCell(CrearCeldaDato($"{cuenta.Saldo:N2}", fontNormal));
                    cuentasTable.AddCell(CrearCeldaDato(cuenta.Estado, fontNormal));
                }

                document.Add(cuentasTable);
            }

            // Actividad
            document.Add(new Paragraph("ACTIVIDAD RECIENTE")
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10));

            var actividadTable = new Table(2).UseAllAvailableWidth();
            actividadTable.AddCell(CrearCeldaInfo("Total Transacciones:", fontBold));
            actividadTable.AddCell(CrearCeldaInfo(resumen.Actividad.TotalTransacciones.ToString(), fontNormal));
            actividadTable.AddCell(CrearCeldaInfo("Transacciones Último Mes:", fontBold));
            actividadTable.AddCell(CrearCeldaInfo(resumen.Actividad.TransaccionesUltimoMes.ToString(), fontNormal));
            actividadTable.AddCell(CrearCeldaInfo("Monto Transferido (Mes):", fontBold));
            actividadTable.AddCell(CrearCeldaInfo($"{resumen.Actividad.MontoTransferidoMes:N2}", fontNormal));
            actividadTable.AddCell(CrearCeldaInfo("Última Transacción:", fontBold));
            actividadTable.AddCell(CrearCeldaInfo(resumen.Actividad.UltimaTransaccion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", fontNormal));
            document.Add(actividadTable);

            // Pie de página
            document.Add(new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFont(fontNormal)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30));

            document.Close();
            return ms.ToArray();
        }

        // ==================== HELPERS PARA PDF ====================

        private static Cell CrearCeldaInfo(string texto, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(3);
        }

        private static Cell CrearCeldaResaltada(string texto, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(font).SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                .SetPadding(5);
        }

        private static Cell CrearCeldaEncabezado(string texto, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(font).SetFontSize(9))
                .SetBackgroundColor(new DeviceRgb(41, 128, 185))
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);
        }

        private static Cell CrearCeldaDato(string texto, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFont(font).SetFontSize(8))
                .SetPadding(3);
        }
    }
}
