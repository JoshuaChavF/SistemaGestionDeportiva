using SistemaGestionDeportiva.Models;
using iText.Kernel.Pdf; 
using iText.Layout;     
using iText.Layout.Element; 
using iText.Kernel.Font; 
using iText.IO.Font.Constants;
using iText.Layout.Properties;  
using iText.Kernel.Geom;       

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

            // Título
            document.Add(new Paragraph("Informe de Rendimiento")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20));

            // Fechas
            document.Add(new Paragraph($"Período: {model.FechaInicio:dd/MM/yyyy} - {model.FechaFin:dd/MM/yyyy}")
                .SetTextAlignment(TextAlignment.CENTER));

            // Tabla de jugadores
            if (model.EstadisticasJugadores?.Any() == true)
            {
                document.Add(new Paragraph("Jugadores").SetFontSize(16));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1, 1, 1, 1 }))
                    .UseAllAvailableWidth();

                // Encabezados
                table.AddHeaderCell("Jugador");
                table.AddHeaderCell("Goles");
                table.AddHeaderCell("Asistencias");
                table.AddHeaderCell("Amarillas");
                table.AddHeaderCell("Rojas");
                table.AddHeaderCell("Partidos");
                table.AddHeaderCell("Prom. Goles");

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

            document.Close();
            return ms.ToArray();
        }
    }
}