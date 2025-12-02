using iTextSharp.text;
using iTextSharp.text.pdf;
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
    }
}
