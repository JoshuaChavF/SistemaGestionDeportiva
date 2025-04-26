using DocumentFormat.OpenXml.Presentation;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaGestionDeportiva.Models;


namespace SistemaGestionDeportiva.Services
{
    public interface IPdfGenerator
    {
        byte[] GenerateInformePdf(InformeViewModel model);
    }

    public class PdfGenerator : IPdfGenerator
    {
        public byte[] GenerateInformePdf(InformeViewModel model)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                // Fuente en negrita para reutilizar
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                // 1. Encabezado
                document.Add(new Paragraph("INFORME DE RENDIMIENTO")
                    .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(18)
                    .SetFont(boldFont)); ;

                // 2. Información básica
                document.Add(new Paragraph($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}"));

                if (model.FechaInicio.HasValue && model.FechaFin.HasValue)
                {
                    document.Add(new Paragraph($"Período analizado: {model.FechaInicio:dd/MM/yyyy} - {model.FechaFin:dd/MM/yyyy}"));
                }

                // 3. Estadísticas de jugadores
                if (model.EstadisticasJugadores?.Any() == true)
                {
                    document.Add(new Paragraph("ESTADÍSTICAS DE JUGADORES")
                        .SetFontSize(14)
                        .SetMarginTop(15)
                        .SetFont(boldFont));

                    var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();

                    // Encabezados de tabla
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Jugador").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Goles").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Asist").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Amar").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Rojas").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("P.J.").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Prom.").SetFont(boldFont)));

                    // Datos
                    foreach (var jugador in model.EstadisticasJugadores)
                    {
                        table.AddCell(jugador.Nombre);
                        table.AddCell(jugador.Goles.ToString());
                        table.AddCell(jugador.Asistencias.ToString());
                        table.AddCell(jugador.TarjetasAmarillas.ToString());
                        table.AddCell(jugador.TarjetasRojas.ToString());
                        table.AddCell(jugador.PartidosJugados.ToString());
                        table.AddCell(jugador.PromedioGoles is double promedio
        ? promedio.ToString("0.00")
        : "0.00");
                    }

                    document.Add(table);
                }

                // 4. Estadísticas de equipo
                if (model.EstadisticasEquipo != null)
                {
                    document.Add(new Paragraph("ESTADÍSTICAS DE EQUIPO")
                        .SetFontSize(14)
                        .SetMarginTop(15)
                        .SetFont(boldFont));

                    var equipoTable = new Table(2)
                        .UseAllAvailableWidth();

                    equipoTable.AddCell(new Cell().Add(new Paragraph("Nombre del equipo").SetFont(boldFont)));
                    equipoTable.AddCell(model.EstadisticasEquipo.Nombre);

                    equipoTable.AddCell(new Cell().Add(new Paragraph("Partidos jugados").SetFont(boldFont)));
                    equipoTable.AddCell((model.EstadisticasEquipo.PartidosGanados +
                                       model.EstadisticasEquipo.PartidosEmpatados +
                                       model.EstadisticasEquipo.PartidosPerdidos).ToString());

                    // ... agregar más estadísticas de equipo

                    document.Add(equipoTable);
                }

                document.Close();
                return ms.ToArray();
            }
        }
    }
}