using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using System.Text;

namespace SistemaBancaEnLinea.BW
{
    public class ReportesServicio : IReportesServicio
    {
        private readonly BancaContext _context;
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly ClienteAcciones _clienteAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly ILogger<ReportesServicio> _logger;

        public ReportesServicio(
            BancaContext context,
            CuentaAcciones cuentaAcciones,
            ClienteAcciones clienteAcciones,
            TransaccionAcciones transaccionAcciones,
            ILogger<ReportesServicio> logger)
        {
            _context = context;
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
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            var writer = PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.BLACK);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);

            // Encabezado
            var titulo = new Paragraph("SISTEMA BANCA EN LINEA", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10
            };
            document.Add(titulo);

            var subtitulo = new Paragraph("Estado de Cuenta", fontSubtitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(subtitulo);

            // Información de cuenta
            var infoCuenta = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 10 };
            infoCuenta.AddCell(new PdfPCell(new Phrase("Numero de Cuenta:", fontBold)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase(extracto.Cuenta.Numero ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase("Titular:", fontBold)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase(extracto.Cuenta.Titular ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase("Tipo:", fontBold)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase(extracto.Cuenta.Tipo ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase("Moneda:", fontBold)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase(extracto.Cuenta.Moneda ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase("Periodo:", fontBold)) { Border = 0, Padding = 3 });
            infoCuenta.AddCell(new PdfPCell(new Phrase($"{extracto.Periodo.Desde:dd/MM/yyyy} - {extracto.Periodo.Hasta:dd/MM/yyyy}", fontNormal)) { Border = 0, Padding = 3 });
            document.Add(infoCuenta);

            // Resumen de saldos
            var saldoTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 10 };
            var grayBg = new BaseColor(240, 240, 240);
            saldoTable.AddCell(new PdfPCell(new Phrase("Saldo Inicial:", fontBold)) { BackgroundColor = grayBg, Padding = 5 });
            saldoTable.AddCell(new PdfPCell(new Phrase($"{extracto.Cuenta.Moneda ?? ""} {extracto.Saldo.Inicial:N2}", fontNormal)) { BackgroundColor = grayBg, Padding = 5 });
            saldoTable.AddCell(new PdfPCell(new Phrase("Saldo Final:", fontBold)) { BackgroundColor = grayBg, Padding = 5 });
            saldoTable.AddCell(new PdfPCell(new Phrase($"{extracto.Cuenta.Moneda ?? ""} {extracto.Saldo.Final:N2}", fontNormal)) { BackgroundColor = grayBg, Padding = 5 });
            document.Add(saldoTable);

            // Tabla de movimientos
            var movTitle = new Paragraph("DETALLE DE MOVIMIENTOS", fontBold)
            {
                SpacingBefore = 15,
                SpacingAfter = 10
            };
            document.Add(movTitle);

            var movTable = new PdfPTable(6) { WidthPercentage = 100 };
            movTable.SetWidths(new float[] { 2, 2, 4, 2, 2, 2 });

            var headerBg = new BaseColor(41, 128, 185);
            var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);

            movTable.AddCell(new PdfPCell(new Phrase("Fecha", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
            movTable.AddCell(new PdfPCell(new Phrase("Tipo", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
            movTable.AddCell(new PdfPCell(new Phrase("Descripcion", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
            movTable.AddCell(new PdfPCell(new Phrase("Monto", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
            movTable.AddCell(new PdfPCell(new Phrase("Referencia", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
            movTable.AddCell(new PdfPCell(new Phrase("Estado", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });

            var fontSmall = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
            var fontRed = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(220, 53, 69));
            var fontGreen = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(40, 167, 69));

            foreach (var mov in extracto.Movimientos)
            {
                movTable.AddCell(new PdfPCell(new Phrase(mov.Fecha.ToString("dd/MM/yy"), fontSmall)) { Padding = 3 });
                movTable.AddCell(new PdfPCell(new Phrase(mov.Tipo ?? "-", fontSmall)) { Padding = 3 });
                movTable.AddCell(new PdfPCell(new Phrase(mov.Descripcion ?? "-", fontSmall)) { Padding = 3 });

                var montoFont = mov.Monto < 0 ? fontRed : fontGreen;
                movTable.AddCell(new PdfPCell(new Phrase($"{mov.Monto:N2}", montoFont)) { Padding = 3 });

                movTable.AddCell(new PdfPCell(new Phrase(mov.Referencia ?? "-", fontSmall)) { Padding = 3 });
                movTable.AddCell(new PdfPCell(new Phrase(mov.Estado ?? "-", fontSmall)) { Padding = 3 });
            }

            document.Add(movTable);

            // Resumen
            var resumenTable = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 15 };
            resumenTable.AddCell(new PdfPCell(new Phrase("Total Transacciones:", fontBold)) { Border = 0, Padding = 3 });
            resumenTable.AddCell(new PdfPCell(new Phrase(extracto.Resumen.TotalTransacciones.ToString(), fontNormal)) { Border = 0, Padding = 3 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Total Debitos:", fontBold)) { Border = 0, Padding = 3 });
            resumenTable.AddCell(new PdfPCell(new Phrase($"{extracto.Cuenta.Moneda ?? ""} {extracto.Resumen.TotalDebitos:N2}", fontNormal)) { Border = 0, Padding = 3 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Total Creditos:", fontBold)) { Border = 0, Padding = 3 });
            resumenTable.AddCell(new PdfPCell(new Phrase($"{extracto.Cuenta.Moneda ?? ""} {extracto.Resumen.TotalCreditos:N2}", fontNormal)) { Border = 0, Padding = 3 });
            document.Add(resumenTable);

            // Pie de página
            var fontFooter = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
            var footer = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontFooter)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 30
            };
            document.Add(footer);

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

            // Agregar BOM (Byte Order Mark) de UTF-8 para que Excel reconozca la codificación
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var content = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[bom.Length + content.Length];
            bom.CopyTo(result, 0);
            content.CopyTo(result, bom.Length);
            return result;
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
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            var writer = PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.BLACK);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
            var fontSectionTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);

            // Encabezado
            var titulo = new Paragraph("SISTEMA BANCA EN LINEA", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10
            };
            document.Add(titulo);

            var subtitulo = new Paragraph("Resumen de Cliente", fontSubtitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(subtitulo);

            // Información del cliente
            var clienteSectionTitle = new Paragraph("INFORMACION DEL CLIENTE", fontSectionTitle)
            {
                SpacingAfter = 10
            };
            document.Add(clienteSectionTitle);

            var infoTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 10 };
            infoTable.AddCell(new PdfPCell(new Phrase("Nombre:", fontBold)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase(resumen.Cliente.Nombre ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase("Identificacion:", fontBold)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase(resumen.Cliente.Identificacion ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase("Correo:", fontBold)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase(resumen.Cliente.Correo ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase("Telefono:", fontBold)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase(resumen.Cliente.Telefono ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase("Fecha Registro:", fontBold)) { Border = 0, Padding = 3 });
            infoTable.AddCell(new PdfPCell(new Phrase(resumen.Cliente.FechaRegistro.ToString("dd/MM/yyyy"), fontNormal)) { Border = 0, Padding = 3 });
            document.Add(infoTable);

            // Resumen de cuentas
            var cuentasSectionTitle = new Paragraph("RESUMEN DE CUENTAS", fontSectionTitle)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(cuentasSectionTitle);

            var cuentasResumen = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 10 };
            cuentasResumen.AddCell(new PdfPCell(new Phrase("Total Cuentas:", fontBold)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase(resumen.Cuentas.Total.ToString(), fontNormal)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase("Cuentas Activas:", fontBold)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase(resumen.Cuentas.Activas.ToString(), fontNormal)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase("Saldo Total CRC:", fontBold)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase($"CRC {resumen.Cuentas.SaldoTotalCRC:N2}", fontNormal)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase("Saldo Total USD:", fontBold)) { Border = 0, Padding = 3 });
            cuentasResumen.AddCell(new PdfPCell(new Phrase($"USD {resumen.Cuentas.SaldoTotalUSD:N2}", fontNormal)) { Border = 0, Padding = 3 });
            document.Add(cuentasResumen);

            // Detalle de cuentas
            if (resumen.Cuentas.Detalle.Count > 0)
            {
                var detalleTitle = new Paragraph("Detalle de Cuentas:", fontBold)
                {
                    SpacingBefore = 10,
                    SpacingAfter = 5
                };
                document.Add(detalleTitle);

                var cuentasTable = new PdfPTable(5) { WidthPercentage = 100, SpacingAfter = 10 };
                cuentasTable.SetWidths(new float[] { 3, 2, 1, 2, 2 });

                var headerBg = new BaseColor(41, 128, 185);
                var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);

                cuentasTable.AddCell(new PdfPCell(new Phrase("Numero", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                cuentasTable.AddCell(new PdfPCell(new Phrase("Tipo", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                cuentasTable.AddCell(new PdfPCell(new Phrase("Moneda", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                cuentasTable.AddCell(new PdfPCell(new Phrase("Saldo", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                cuentasTable.AddCell(new PdfPCell(new Phrase("Estado", fontHeader)) { BackgroundColor = headerBg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });

                var fontSmall = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);

                foreach (var cuenta in resumen.Cuentas.Detalle)
                {
                    cuentasTable.AddCell(new PdfPCell(new Phrase(cuenta.Numero ?? "-", fontSmall)) { Padding = 3 });
                    cuentasTable.AddCell(new PdfPCell(new Phrase(cuenta.Tipo ?? "-", fontSmall)) { Padding = 3 });
                    cuentasTable.AddCell(new PdfPCell(new Phrase(cuenta.Moneda ?? "-", fontSmall)) { Padding = 3 });
                    cuentasTable.AddCell(new PdfPCell(new Phrase($"{cuenta.Saldo:N2}", fontSmall)) { Padding = 3 });
                    cuentasTable.AddCell(new PdfPCell(new Phrase(cuenta.Estado ?? "-", fontSmall)) { Padding = 3 });
                }

                document.Add(cuentasTable);
            }

            // Actividad
            var actividadSectionTitle = new Paragraph("ACTIVIDAD RECIENTE", fontSectionTitle)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(actividadSectionTitle);

            var actividadTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 10 };
            actividadTable.AddCell(new PdfPCell(new Phrase("Total Transacciones:", fontBold)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase(resumen.Actividad.TotalTransacciones.ToString(), fontNormal)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase("Transacciones Ultimo Mes:", fontBold)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase(resumen.Actividad.TransaccionesUltimoMes.ToString(), fontNormal)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase("Monto Transferido (Mes):", fontBold)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase($"{resumen.Actividad.MontoTransferidoMes:N2}", fontNormal)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase("Ultima Transaccion:", fontBold)) { Border = 0, Padding = 3 });
            actividadTable.AddCell(new PdfPCell(new Phrase(resumen.Actividad.UltimaTransaccion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", fontNormal)) { Border = 0, Padding = 3 });
            document.Add(actividadTable);

            // Pie de página
            var fontFooter = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
            var footer = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontFooter)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 30
            };
            document.Add(footer);

            document.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// Genera extracto de cuenta con validación de acceso
        /// </summary>
        public async Task<(byte[]? archivo, string? numeroCuenta)> GenerarExtractoConAccesoAsync(
            int cuentaId, DateTime? startDate, DateTime? endDate, string format, int usuarioId, string rol)
        {
            var cuenta = await _cuentaAcciones.ObtenerPorIdAsync(cuentaId);
            if (cuenta == null)
                throw new InvalidOperationException("Cuenta no encontrada.");

            // Validar acceso
            if (!await ValidarAccesoACuentaAsync(cuenta.ClienteId, usuarioId, rol))
                throw new UnauthorizedAccessException("No tiene acceso a esta cuenta.");

            var inicio = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var fin = endDate ?? DateTime.UtcNow;

            byte[] archivo = format.ToLower() switch
            {
                "pdf" => await GenerarExtractoPdfAsync(cuentaId, inicio, fin),
                "csv" => await GenerarExtractoCsvAsync(cuentaId, inicio, fin),
                _ => null
            };

            return (archivo, cuenta.Numero);
        }

        /// <summary>
        /// Genera resumen para usuario autenticado (PDF)
        /// </summary>
        public async Task<byte[]?> GenerarResumenParaUsuarioAsync(int usuarioId, string format)
        {
            var cliente = await _context.Clientes
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuarioId);

            if (cliente == null)
                throw new InvalidOperationException("Cliente no identificado.");

            return format.ToLower() == "pdf"
                ? await GenerarResumenClientePdfAsync(cliente.Id)
                : null;
        }

        /// <summary>
        /// Genera resumen para usuario autenticado (JSON)
        /// </summary>
        public async Task<ResumenClienteDto> GenerarResumenParaUsuarioJsonAsync(int usuarioId)
        {
            var cliente = await _context.Clientes
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuarioId);

            if (cliente == null)
                throw new InvalidOperationException("Cliente no identificado.");

            return await GenerarResumenClienteAsync(cliente.Id);
        }

        /// <summary>
        /// Genera reporte de transacciones con filtros y validación de acceso
        /// </summary>
        public async Task<object> GenerarReporteTransaccionesAsync(
            DateTime inicio, DateTime fin, string? tipo, string? estado, int? clienteId, int usuarioId, string rol)
        {
            List<Transaccion> transacciones;

            if (clienteId.HasValue)
            {
                transacciones = await _transaccionAcciones.FiltrarHistorialAsync(
                    clienteId.Value, null, inicio, fin, tipo, estado);
            }
            else
            {
                // Obtener transacciones según el rol
                transacciones = await ObtenerTransaccionesPorRolAsync(usuarioId, rol, inicio, fin);
            }

            // Aplicar filtros adicionales
            if (!string.IsNullOrEmpty(tipo))
                transacciones = transacciones.Where(t => t.Tipo == tipo).ToList();

            if (!string.IsNullOrEmpty(estado))
                transacciones = transacciones.Where(t => t.Estado == estado).ToList();

            var resumen = new
            {
                totalTransacciones = transacciones.Count,
                montoTotal = transacciones.Where(t => t.Estado == "Exitosa").Sum(t => t.Monto),
                comisionesTotal = 0,
                porTipo = transacciones.GroupBy(t => t.Tipo).Select(g => new
                {
                    tipo = g.Key,
                    cantidad = g.Count(),
                    monto = g.Sum(t => t.Monto)
                }),
                porEstado = transacciones.GroupBy(t => t.Estado).Select(g => new
                {
                    estado = g.Key,
                    cantidad = g.Count()
                })
            };

            return new
            {
                success = true,
                data = new
                {
                    periodo = new { desde = inicio, hasta = fin },
                    resumen,
                    transacciones = transacciones.Select(t => new
                    {
                        id = t.Id,
                        fecha = t.FechaCreacion,
                        tipo = t.Tipo,
                        clienteNombre = t.Cliente?.UsuarioAsociado?.Nombre ?? "N/A",
                        monto = t.Monto,
                        moneda = t.Moneda,
                        comision = 0,
                        estado = t.Estado,
                        referencia = t.ComprobanteReferencia
                    }).OrderByDescending(t => t.fecha)
                }
            };
        }

        /// <summary>
        /// Genera estadísticas para dashboard de administrador
        /// </summary>
        public async Task<object> GenerarEstadisticasDashboardAsync()
        {
            var clientes = await _clienteAcciones.ObtenerTodosAsync();

            var hoy = DateTime.UtcNow.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            return new
            {
                clientes = new
                {
                    total = clientes.Count,
                    activos = clientes.Count(c => c.Estado == "Activo"),
                    nuevosEsteMes = clientes.Count(c => c.FechaRegistro >= inicioMes)
                }
            };
        }

        /// <summary>
        /// Genera reporte de volumen de transacciones diarias
        /// </summary>
        public async Task<object> GenerarVolumenDiarioAsync(DateTime inicio, DateTime fin, int usuarioId, string rol)
        {
            var transacciones = await ObtenerTransaccionesPorRolAsync(usuarioId, rol, inicio, fin);

            var volumenDiario = transacciones
                .Where(t => t.Estado == "Exitosa")
                .GroupBy(t => t.FechaCreacion.Date)
                .Select(g => new
                {
                    fecha = g.Key,
                    cantidadTransacciones = g.Count(),
                    montoTotalCRC = g.Where(t => t.Moneda == "CRC").Sum(t => t.Monto),
                    montoTotalUSD = g.Where(t => t.Moneda == "USD").Sum(t => t.Monto),
                    comisionesTotales = g.Sum(t => t.Comision)
                })
                .OrderBy(x => x.fecha)
                .ToList();

            var resumen = new
            {
                periodo = new { desde = inicio, hasta = fin },
                totalDias = volumenDiario.Count,
                promedioDiarioCantidad = volumenDiario.Any() ? volumenDiario.Average(v => v.cantidadTransacciones) : 0,
                promedioDiarioMontoCRC = volumenDiario.Any() ? volumenDiario.Average(v => v.montoTotalCRC) : 0,
                promedioDiarioMontoUSD = volumenDiario.Any() ? volumenDiario.Average(v => v.montoTotalUSD) : 0
            };

            return new
            {
                success = true,
                data = new
                {
                    resumen,
                    volumenDiario
                }
            };
        }

        /// <summary>
        /// Genera reporte de clientes más activos
        /// </summary>
        public async Task<object> GenerarClientesMasActivosAsync(DateTime inicio, DateTime fin, int top, int usuarioId, string rol)
        {
            var transacciones = await ObtenerTransaccionesPorRolAsync(usuarioId, rol, inicio, fin);

            var clientesActivos = transacciones
                .Where(t => t.Cliente != null)
                .GroupBy(t => new
                {
                    ClienteId = t.Cliente!.Id,
                    ClienteNombre = t.Cliente.UsuarioAsociado != null ? t.Cliente.UsuarioAsociado.Nombre : "N/A",
                    ClienteEmail = t.Cliente.UsuarioAsociado != null ? t.Cliente.UsuarioAsociado.Email : "N/A"
                })
                .Select(g => new
                {
                    clienteId = g.Key.ClienteId,
                    clienteNombre = g.Key.ClienteNombre,
                    clienteEmail = g.Key.ClienteEmail,
                    totalTransacciones = g.Count(),
                    transaccionesExitosas = g.Count(t => t.Estado == "Exitosa"),
                    montoTotalCRC = g.Where(t => t.Moneda == "CRC" && t.Estado == "Exitosa").Sum(t => t.Monto),
                    montoTotalUSD = g.Where(t => t.Moneda == "USD" && t.Estado == "Exitosa").Sum(t => t.Monto),
                    ultimaTransaccion = g.Max(t => t.FechaCreacion)
                })
                .OrderByDescending(c => c.totalTransacciones)
                .Take(top)
                .ToList();

            return new
            {
                success = true,
                data = new
                {
                    periodo = new { desde = inicio, hasta = fin },
                    top,
                    clientesMasActivos = clientesActivos
                }
            };
        }

        /// <summary>
        /// Genera reporte de totales por período
        /// </summary>
        public async Task<object> GenerarTotalesPorPeriodoAsync(DateTime inicio, DateTime fin, int usuarioId, string rol)
        {
            var transacciones = await ObtenerTransaccionesPorRolAsync(usuarioId, rol, inicio, fin);

            var totales = new
            {
                periodo = new { desde = inicio, hasta = fin },
                transacciones = new
                {
                    total = transacciones.Count,
                    exitosas = transacciones.Count(t => t.Estado == "Exitosa"),
                    fallidas = transacciones.Count(t => t.Estado == "Fallida"),
                    pendientes = transacciones.Count(t => t.Estado == "Pendiente")
                },
                montos = new
                {
                    totalCRC = transacciones.Where(t => t.Moneda == "CRC" && t.Estado == "Exitosa").Sum(t => t.Monto),
                    totalUSD = transacciones.Where(t => t.Moneda == "USD" && t.Estado == "Exitosa").Sum(t => t.Monto),
                    comisionesTotales = transacciones.Where(t => t.Estado == "Exitosa").Sum(t => t.Comision)
                },
                porTipo = transacciones
                    .Where(t => t.Estado == "Exitosa")
                    .GroupBy(t => t.Tipo)
                    .Select(g => new
                    {
                        tipo = g.Key,
                        cantidad = g.Count(),
                        montoCRC = g.Where(t => t.Moneda == "CRC").Sum(t => t.Monto),
                        montoUSD = g.Where(t => t.Moneda == "USD").Sum(t => t.Monto)
                    })
                    .ToList()
            };

            return totales;
        }

        #region Métodos Privados de Validación y Utilidades

        private async Task<bool> ValidarAccesoACuentaAsync(int cuentaClienteId, int usuarioId, string rol)
        {
            if (rol is "Administrador" or "Gestor")
                return true;

            var cliente = await _context.Clientes
                .Include(c => c.UsuarioAsociado)
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuarioId);

            return cliente != null && cliente.Id == cuentaClienteId;
        }

        private async Task<List<Transaccion>> ObtenerTransaccionesPorRolAsync(int usuarioId, string rol, DateTime inicio, DateTime fin)
        {
            if (rol == "Administrador")
            {
                // Administrador ve TODAS las transacciones del sistema
                return await _context.Transacciones
                    .Include(t => t.Cliente)
                        .ThenInclude(c => c!.UsuarioAsociado)
                    .Include(t => t.CuentaOrigen)
                    .Include(t => t.CuentaDestino)
                    .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin)
                    .OrderByDescending(t => t.FechaCreacion)
                    .ToListAsync();
            }
            else if (rol == "Gestor")
            {
                // Gestor ve transacciones de sus clientes asignados
                var clientesGestor = await _context.Clientes
                    .Where(c => c.GestorAsignado != null && c.GestorAsignado.Id == usuarioId)
                    .Select(c => c.Id)
                    .ToListAsync();

                return await _context.Transacciones
                    .Include(t => t.Cliente)
                        .ThenInclude(c => c!.UsuarioAsociado)
                    .Include(t => t.CuentaOrigen)
                    .Include(t => t.CuentaDestino)
                    .Where(t => clientesGestor.Contains(t.ClienteId) &&
                                t.FechaCreacion >= inicio && t.FechaCreacion <= fin)
                    .OrderByDescending(t => t.FechaCreacion)
                    .ToListAsync();
            }

            return new List<Transaccion>();
        }

        #endregion

        #region RF-G1: Exportación de Reportes Administrativos

        /// <summary>
        /// RF-G1: Exportar totales por período a PDF
        /// </summary>
        public async Task<byte[]> ExportarTotalesPorPeriodoPdfAsync(DateTime inicio, DateTime fin)
        {
            var transacciones = await _context.Transacciones
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin)
                .ToListAsync();

            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            document.Add(new Paragraph("Reporte de Totales por Período", fontTitle) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}", fontSubtitle) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });
            document.Add(new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fontSubtitle) { Alignment = Element.ALIGN_RIGHT, SpacingAfter = 20 });

            // Resumen general
            var totalOperaciones = transacciones.Count;
            var volumenTotal = transacciones.Sum(t => t.Monto);
            var exitosas = transacciones.Count(t => t.Estado == "Exitosa");
            var fallidas = transacciones.Count(t => t.Estado == "Fallida");

            var resumenTable = new PdfPTable(2) { WidthPercentage = 60, SpacingAfter = 20 };
            resumenTable.AddCell(new PdfPCell(new Phrase("Métrica", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Valor", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Total Operaciones", fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase(totalOperaciones.ToString("N0"), fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Volumen Total", fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase($"₡ {volumenTotal:N2}", fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Operaciones Exitosas", fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase(exitosas.ToString("N0"), fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase("Operaciones Fallidas", fontNormal)) { Padding = 5 });
            resumenTable.AddCell(new PdfPCell(new Phrase(fallidas.ToString("N0"), fontNormal)) { Padding = 5 });
            document.Add(resumenTable);

            // Desglose por tipo
            document.Add(new Paragraph("Desglose por Tipo de Operación", fontBold) { SpacingBefore = 10, SpacingAfter = 10 });
            var tiposTable = new PdfPTable(3) { WidthPercentage = 100, SpacingAfter = 20 };
            tiposTable.AddCell(new PdfPCell(new Phrase("Tipo", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            tiposTable.AddCell(new PdfPCell(new Phrase("Cantidad", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            tiposTable.AddCell(new PdfPCell(new Phrase("Monto Total", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });

            var porTipo = transacciones.GroupBy(t => t.Tipo).Select(g => new { Tipo = g.Key, Cantidad = g.Count(), Total = g.Sum(x => x.Monto) });
            foreach (var tipo in porTipo)
            {
                tiposTable.AddCell(new PdfPCell(new Phrase(tipo.Tipo, fontNormal)) { Padding = 5 });
                tiposTable.AddCell(new PdfPCell(new Phrase(tipo.Cantidad.ToString("N0"), fontNormal)) { Padding = 5 });
                tiposTable.AddCell(new PdfPCell(new Phrase($"₡ {tipo.Total:N2}", fontNormal)) { Padding = 5 });
            }
            document.Add(tiposTable);

            document.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// RF-G1: Exportar totales por período a Excel
        /// </summary>
        public async Task<byte[]> ExportarTotalesPorPeriodoExcelAsync(DateTime inicio, DateTime fin)
        {
            var transacciones = await _context.Transacciones
                .Include(t => t.Cliente).ThenInclude(c => c!.UsuarioAsociado)
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            
            // Hoja de Resumen
            var wsResumen = workbook.Worksheets.Add("Resumen");
            wsResumen.Cell(1, 1).Value = "Reporte de Totales por Período";
            wsResumen.Cell(1, 1).Style.Font.Bold = true;
            wsResumen.Cell(1, 1).Style.Font.FontSize = 16;
            wsResumen.Cell(2, 1).Value = $"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
            wsResumen.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";

            wsResumen.Cell(5, 1).Value = "Métrica";
            wsResumen.Cell(5, 2).Value = "Valor";
            wsResumen.Range("A5:B5").Style.Font.Bold = true;
            wsResumen.Range("A5:B5").Style.Fill.BackgroundColor = XLColor.LightGray;
            wsResumen.Range("A5:B5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("A5:B5").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wsResumen.Cell(6, 1).Value = "Total Operaciones";
            wsResumen.Cell(6, 2).Value = transacciones.Count;
            wsResumen.Cell(7, 1).Value = "Volumen Total";
            wsResumen.Cell(7, 2).Value = transacciones.Sum(t => t.Monto);
            wsResumen.Cell(7, 2).Style.NumberFormat.Format = "₡ #,##0.00";
            wsResumen.Cell(8, 1).Value = "Operaciones Exitosas";
            wsResumen.Cell(8, 2).Value = transacciones.Count(t => t.Estado == "Exitosa");
            wsResumen.Cell(9, 1).Value = "Operaciones Fallidas";
            wsResumen.Cell(9, 2).Value = transacciones.Count(t => t.Estado == "Fallida");

            // Aplicar bordes a la tabla de métricas
            wsResumen.Range("A6:B9").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("A6:B9").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Desglose por tipo
            wsResumen.Cell(11, 1).Value = "Tipo";
            wsResumen.Cell(11, 2).Value = "Cantidad";
            wsResumen.Cell(11, 3).Value = "Monto Total";
            wsResumen.Range("A11:C11").Style.Font.Bold = true;
            wsResumen.Range("A11:C11").Style.Fill.BackgroundColor = XLColor.LightGray;
            wsResumen.Range("A11:C11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("A11:C11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var row = 12;
            var startRow = row;
            foreach (var grupo in transacciones.GroupBy(t => t.Tipo))
            {
                wsResumen.Cell(row, 1).Value = grupo.Key;
                wsResumen.Cell(row, 2).Value = grupo.Count();
                wsResumen.Cell(row, 3).Value = grupo.Sum(t => t.Monto);
                wsResumen.Cell(row, 3).Style.NumberFormat.Format = "₡ #,##0.00";
                row++;
            }

            // Aplicar bordes a la tabla de desglose por tipo
            if (row > startRow)
            {
                wsResumen.Range($"A{startRow}:C{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                wsResumen.Range($"A{startRow}:C{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustar ancho de columnas
            wsResumen.Column(1).Width = 30;
            wsResumen.Column(2).Width = 20;
            wsResumen.Column(3).Width = 20;

            // Hoja de Detalle
            var wsDetalle = workbook.Worksheets.Add("Detalle");
            wsDetalle.Cell(1, 1).Value = "ID";
            wsDetalle.Cell(1, 2).Value = "Fecha";
            wsDetalle.Cell(1, 3).Value = "Tipo";
            wsDetalle.Cell(1, 4).Value = "Estado";
            wsDetalle.Cell(1, 5).Value = "Cliente";
            wsDetalle.Cell(1, 6).Value = "Monto";
            wsDetalle.Cell(1, 7).Value = "Moneda";
            wsDetalle.Cell(1, 8).Value = "Descripción";
            wsDetalle.Range("A1:H1").Style.Font.Bold = true;
            wsDetalle.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.LightGray;
            wsDetalle.Range("A1:H1").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsDetalle.Range("A1:H1").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            row = 2;
            var startRowDetalle = row;
            foreach (var t in transacciones)
            {
                wsDetalle.Cell(row, 1).Value = t.Id;
                wsDetalle.Cell(row, 2).Value = t.FechaCreacion;
                wsDetalle.Cell(row, 3).Value = t.Tipo;
                wsDetalle.Cell(row, 4).Value = t.Estado;
                wsDetalle.Cell(row, 5).Value = t.Cliente?.UsuarioAsociado?.Nombre ?? "N/A";
                wsDetalle.Cell(row, 6).Value = t.Monto;
                wsDetalle.Cell(row, 6).Style.NumberFormat.Format = "₡ #,##0.00";
                wsDetalle.Cell(row, 7).Value = t.Moneda;
                wsDetalle.Cell(row, 8).Value = t.Descripcion ?? "";
                row++;
            }

            // Aplicar bordes a toda la tabla de detalle
            if (row > startRowDetalle)
            {
                wsDetalle.Range($"A{startRowDetalle}:H{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                wsDetalle.Range($"A{startRowDetalle}:H{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustar ancho de columnas
            wsDetalle.Column(1).Width = 10;
            wsDetalle.Column(2).Width = 20;
            wsDetalle.Column(3).Width = 15;
            wsDetalle.Column(4).Width = 15;
            wsDetalle.Column(5).Width = 30;
            wsDetalle.Column(6).Width = 20;
            wsDetalle.Column(7).Width = 10;
            wsDetalle.Column(8).Width = 40;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// RF-G1: Exportar Top Clientes a PDF
        /// </summary>
        public async Task<byte[]> ExportarTopClientesPdfAsync(DateTime inicio, DateTime fin, int top)
        {
            var transacciones = await _context.Transacciones
                .Include(t => t.Cliente).ThenInclude(c => c!.UsuarioAsociado)
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin && t.Estado == "Exitosa")
                .ToListAsync();

            var topClientes = transacciones
                .GroupBy(t => new { t.ClienteId, Nombre = t.Cliente?.UsuarioAsociado?.Nombre ?? "N/A" })
                .Select(g => new { g.Key.ClienteId, g.Key.Nombre, Volumen = g.Sum(x => x.Monto), Operaciones = g.Count() })
                .OrderByDescending(x => x.Volumen)
                .Take(top)
                .ToList();

            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            document.Add(new Paragraph($"Top {top} Clientes por Volumen", fontTitle) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}", fontSubtitle) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });
            document.Add(new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fontSubtitle) { Alignment = Element.ALIGN_RIGHT, SpacingAfter = 20 });

            var table = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20 };
            table.SetWidths(new float[] { 1, 3, 2, 2 });

            table.AddCell(new PdfPCell(new Phrase("#", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Cliente", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase("Volumen Total", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Phrase("Operaciones", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER });

            var pos = 1;
            foreach (var cliente in topClientes)
            {
                table.AddCell(new PdfPCell(new Phrase(pos.ToString(), fontNormal)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase(cliente.Nombre, fontNormal)) { Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase($"₡ {cliente.Volumen:N2}", fontNormal)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(cliente.Operaciones.ToString("N0"), fontNormal)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                pos++;
            }

            document.Add(table);
            document.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// RF-G1: Exportar Top Clientes a Excel
        /// </summary>
        public async Task<byte[]> ExportarTopClientesExcelAsync(DateTime inicio, DateTime fin, int top)
        {
            var transacciones = await _context.Transacciones
                .Include(t => t.Cliente).ThenInclude(c => c!.UsuarioAsociado)
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin && t.Estado == "Exitosa")
                .ToListAsync();

            var topClientes = transacciones
                .GroupBy(t => new { t.ClienteId, Nombre = t.Cliente?.UsuarioAsociado?.Nombre ?? "N/A" })
                .Select(g => new { g.Key.ClienteId, g.Key.Nombre, Volumen = g.Sum(x => x.Monto), Operaciones = g.Count() })
                .OrderByDescending(x => x.Volumen)
                .Take(top)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Top Clientes");

            ws.Cell(1, 1).Value = $"Top {top} Clientes por Volumen";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Value = $"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
            ws.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Cell(5, 1).Value = "Posición";
            ws.Cell(5, 2).Value = "Cliente";
            ws.Cell(5, 3).Value = "Volumen Total";
            ws.Cell(5, 4).Value = "Operaciones";
            ws.Range("A5:D5").Style.Font.Bold = true;
            ws.Range("A5:D5").Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Range("A5:D5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range("A5:D5").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var row = 6;
            var startRowData = row;
            var pos = 1;
            foreach (var cliente in topClientes)
            {
                ws.Cell(row, 1).Value = pos++;
                ws.Cell(row, 2).Value = cliente.Nombre;
                ws.Cell(row, 3).Value = cliente.Volumen;
                ws.Cell(row, 3).Style.NumberFormat.Format = "₡ #,##0.00";
                ws.Cell(row, 4).Value = cliente.Operaciones;
                row++;
            }

            // Aplicar bordes a toda la tabla
            if (row > startRowData)
            {
                ws.Range($"A{startRowData}:D{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A{startRowData}:D{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustar ancho de columnas
            ws.Column(1).Width = 12;
            ws.Column(2).Width = 40;
            ws.Column(3).Width = 20;
            ws.Column(4).Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// RF-G1: Exportar Volumen Diario a PDF
        /// </summary>
        public async Task<byte[]> ExportarVolumenDiarioPdfAsync(DateTime inicio, DateTime fin)
        {
            var transacciones = await _context.Transacciones
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin && t.Estado == "Exitosa")
                .ToListAsync();

            var volumenDiario = transacciones
                .GroupBy(t => t.FechaCreacion.Date)
                .Select(g => new { Fecha = g.Key, Volumen = g.Sum(x => x.Monto), Operaciones = g.Count() })
                .OrderBy(x => x.Fecha)
                .ToList();

            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            document.Add(new Paragraph("Volumen Diario de Transacciones", fontTitle) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}", fontSubtitle) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });
            document.Add(new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fontSubtitle) { Alignment = Element.ALIGN_RIGHT, SpacingAfter = 20 });

            var table = new PdfPTable(3) { WidthPercentage = 100, SpacingAfter = 20 };

            table.AddCell(new PdfPCell(new Phrase("Fecha", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase("Operaciones", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Volumen", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT });

            foreach (var dia in volumenDiario)
            {
                table.AddCell(new PdfPCell(new Phrase(dia.Fecha.ToString("dd/MM/yyyy"), fontNormal)) { Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase(dia.Operaciones.ToString("N0"), fontNormal)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase($"₡ {dia.Volumen:N2}", fontNormal)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            // Totales
            table.AddCell(new PdfPCell(new Phrase("TOTAL", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase(volumenDiario.Sum(d => d.Operaciones).ToString("N0"), fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase($"₡ {volumenDiario.Sum(d => d.Volumen):N2}", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT });

            document.Add(table);
            document.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// RF-G1: Exportar Volumen Diario a Excel
        /// </summary>
        public async Task<byte[]> ExportarVolumenDiarioExcelAsync(DateTime inicio, DateTime fin)
        {
            var transacciones = await _context.Transacciones
                .Where(t => t.FechaCreacion >= inicio && t.FechaCreacion <= fin && t.Estado == "Exitosa")
                .ToListAsync();

            var volumenDiario = transacciones
                .GroupBy(t => t.FechaCreacion.Date)
                .Select(g => new { Fecha = g.Key, Volumen = g.Sum(x => x.Monto), Operaciones = g.Count() })
                .OrderBy(x => x.Fecha)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Volumen Diario");

            ws.Cell(1, 1).Value = "Volumen Diario de Transacciones";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Value = $"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
            ws.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Cell(5, 1).Value = "Fecha";
            ws.Cell(5, 2).Value = "Operaciones";
            ws.Cell(5, 3).Value = "Volumen";
            ws.Range("A5:C5").Style.Font.Bold = true;
            ws.Range("A5:C5").Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Range("A5:C5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range("A5:C5").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var row = 6;
            var startRowData = row;
            foreach (var dia in volumenDiario)
            {
                ws.Cell(row, 1).Value = dia.Fecha;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                ws.Cell(row, 2).Value = dia.Operaciones;
                ws.Cell(row, 3).Value = dia.Volumen;
                ws.Cell(row, 3).Style.NumberFormat.Format = "₡ #,##0.00";
                row++;
            }

            // Aplicar bordes a los datos
            if (row > startRowData)
            {
                ws.Range($"A{startRowData}:C{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A{startRowData}:C{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Totales
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = volumenDiario.Sum(d => d.Operaciones);
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 3).Value = volumenDiario.Sum(d => d.Volumen);
            ws.Cell(row, 3).Style.NumberFormat.Format = "₡ #,##0.00";
            ws.Cell(row, 3).Style.Font.Bold = true;

            // Aplicar bordes a los totales
            ws.Range($"A{row}:C{row}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range($"A{row}:C{row}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Range($"A{row}:C{row}").Style.Fill.BackgroundColor = XLColor.LightGray;

            // Ajustar ancho de columnas
            ws.Column(1).Width = 15;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 20;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion

        #region RF-G2: Exportación de Auditoría

        /// <summary>
        /// RF-G2: Exportar auditoría a PDF
        /// </summary>
        public async Task<byte[]> ExportarAuditoriaPdfAsync(DateTime inicio, DateTime fin, string? tipoOperacion)
        {
            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .Where(r => r.FechaHora >= inicio && r.FechaHora <= fin);

            if (!string.IsNullOrEmpty(tipoOperacion))
                query = query.Where(r => r.TipoOperacion == tipoOperacion);

            var registros = await query.OrderByDescending(r => r.FechaHora).ToListAsync();

            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 36, 36, 54, 54);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            document.Add(new Paragraph("Reporte de Auditoría del Sistema", fontTitle) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}", fontSubtitle) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(tipoOperacion))
                document.Add(new Paragraph($"Filtro: {tipoOperacion}", fontSubtitle) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fontSubtitle) { Alignment = Element.ALIGN_RIGHT, SpacingAfter = 20 });
            document.Add(new Paragraph($"Total de registros: {registros.Count}", fontSubtitle) { SpacingAfter = 15 });

            var table = new PdfPTable(5) { WidthPercentage = 100, SpacingAfter = 20 };
            table.SetWidths(new float[] { 2, 2, 2, 2, 4 });

            table.AddCell(new PdfPCell(new Phrase("Fecha/Hora", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("Usuario", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("Email", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("Operación", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("Descripción", fontBold)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });

            foreach (var reg in registros)
            {
                table.AddCell(new PdfPCell(new Phrase(reg.FechaHora.ToString("dd/MM/yyyy HH:mm"), fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(reg.Usuario?.Nombre ?? "N/A", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(reg.Usuario?.Email ?? "N/A", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(reg.TipoOperacion, fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(reg.Descripcion ?? "", fontNormal)) { Padding = 4 });
            }

            document.Add(table);
            document.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// RF-G2: Exportar auditoría a Excel
        /// </summary>
        public async Task<byte[]> ExportarAuditoriaExcelAsync(DateTime inicio, DateTime fin, string? tipoOperacion)
        {
            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .Where(r => r.FechaHora >= inicio && r.FechaHora <= fin);

            if (!string.IsNullOrEmpty(tipoOperacion))
                query = query.Where(r => r.TipoOperacion == tipoOperacion);

            var registros = await query.OrderByDescending(r => r.FechaHora).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Auditoría");

            ws.Cell(1, 1).Value = "Reporte de Auditoría del Sistema";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Value = $"Período: {inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}";
            if (!string.IsNullOrEmpty(tipoOperacion))
                ws.Cell(3, 1).Value = $"Filtro: {tipoOperacion}";
            ws.Cell(4, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(4, 3).Value = $"Total registros: {registros.Count}";

            var headerRow = 6;
            ws.Cell(headerRow, 1).Value = "ID";
            ws.Cell(headerRow, 2).Value = "Fecha/Hora";
            ws.Cell(headerRow, 3).Value = "Usuario ID";
            ws.Cell(headerRow, 4).Value = "Usuario";
            ws.Cell(headerRow, 5).Value = "Email";
            ws.Cell(headerRow, 6).Value = "Tipo Operación";
            ws.Cell(headerRow, 7).Value = "Descripción";
            ws.Cell(headerRow, 8).Value = "Detalle JSON";
            ws.Range($"A{headerRow}:H{headerRow}").Style.Font.Bold = true;
            ws.Range($"A{headerRow}:H{headerRow}").Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Range($"A{headerRow}:H{headerRow}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range($"A{headerRow}:H{headerRow}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var row = headerRow + 1;
            var startRowData = row;
            foreach (var reg in registros)
            {
                ws.Cell(row, 1).Value = reg.Id;
                ws.Cell(row, 2).Value = reg.FechaHora;
                ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                ws.Cell(row, 3).Value = reg.UsuarioId;
                ws.Cell(row, 4).Value = reg.Usuario?.Nombre ?? "N/A";
                ws.Cell(row, 5).Value = reg.Usuario?.Email ?? "N/A";
                ws.Cell(row, 6).Value = reg.TipoOperacion;
                ws.Cell(row, 7).Value = reg.Descripcion ?? "";
                ws.Cell(row, 8).Value = reg.DetalleJson ?? "";
                row++;
            }

            // Aplicar bordes a los datos
            if (row > startRowData)
            {
                ws.Range($"A{startRowData}:H{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A{startRowData}:H{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustar ancho de columnas
            ws.Column(1).Width = 10;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 30;
            ws.Column(5).Width = 30;
            ws.Column(6).Width = 20;
            ws.Column(7).Width = 40;
            ws.Column(8).Width = 50;

            // Hoja de resumen por tipo de operación
            var wsResumen = workbook.Worksheets.Add("Resumen por Tipo");
            wsResumen.Cell(1, 1).Value = "Resumen por Tipo de Operación";
            wsResumen.Cell(1, 1).Style.Font.Bold = true;
            wsResumen.Cell(1, 1).Style.Font.FontSize = 14;

            wsResumen.Cell(3, 1).Value = "Tipo Operación";
            wsResumen.Cell(3, 2).Value = "Cantidad";
            wsResumen.Range("A3:B3").Style.Font.Bold = true;
            wsResumen.Range("A3:B3").Style.Fill.BackgroundColor = XLColor.LightGray;
            wsResumen.Range("A3:B3").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("A3:B3").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            row = 4;
            var startRowResumen = row;
            foreach (var grupo in registros.GroupBy(r => r.TipoOperacion).OrderByDescending(g => g.Count()))
            {
                wsResumen.Cell(row, 1).Value = grupo.Key;
                wsResumen.Cell(row, 2).Value = grupo.Count();
                row++;
            }

            // Aplicar bordes a los datos del resumen
            if (row > startRowResumen)
            {
                wsResumen.Range($"A{startRowResumen}:B{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                wsResumen.Range($"A{startRowResumen}:B{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustar ancho de columnas del resumen
            wsResumen.Column(1).Width = 30;
            wsResumen.Column(2).Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion
    }
}
