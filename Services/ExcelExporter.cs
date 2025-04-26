using SistemaGestionDeportiva.Models;
using OfficeOpenXml;

public interface IExcelExporter
{
    byte[] GenerateInformeExcel(InformeViewModel model);
}

public class ExcelExporter : IExcelExporter
{
    public byte[] GenerateInformeExcel(InformeViewModel model)
    {
        using (var package = new ExcelPackage())
        {
            // Hoja de jugadores
            var jugadoresSheet = package.Workbook.Worksheets.Add("Jugadores");
            jugadoresSheet.Cells.LoadFromCollection(model.EstadisticasJugadores, true);

            // Hoja de equipo
            if (model.EstadisticasEquipo != null)
            {
                var equipoSheet = package.Workbook.Worksheets.Add("Equipo");
                equipoSheet.Cells["A1"].Value = "Estadísticas del Equipo";
                equipoSheet.Cells["A1:B1"].Merge = true;

                var equipoProps = typeof(EquipoEstadisticas).GetProperties();
                for (int i = 0; i < equipoProps.Length; i++)
                {
                    equipoSheet.Cells[i + 2, 1].Value = equipoProps[i].Name;
                    equipoSheet.Cells[i + 2, 2].Value = equipoProps[i].GetValue(model.EstadisticasEquipo);
                }
            }

            return package.GetAsByteArray();
        }
    }
}