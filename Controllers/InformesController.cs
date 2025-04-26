using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using DocumentFormat.OpenXml.Presentation;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Properties;
using System.Diagnostics;
using System;
using System.IO;
using System.Linq;
using iText.Layout;
using iText.Layout.Element; 
using iText.Kernel.Geom; 
using SistemaGestionDeportiva.Repositories;
namespace SistemaGestionDeportiva.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class InformesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExcelExporter _excelExporter;
        private readonly IPdfGenerator _pdfGenerator;
        private readonly IRepository _repository;

        public InformesController(ApplicationDbContext context, IExcelExporter excelExporter,IPdfGenerator pdfGenerator, IRepository repository)
        {
            _context = context;
            _excelExporter = excelExporter;
            _pdfGenerator = pdfGenerator;
            _repository = repository;
        }

        // GET: Informes
        public IActionResult Index()
        {
            var model = new InformeViewModel
            {
                FechaInicio = DateTime.Now.AddMonths(-1),
                FechaFin = DateTime.Now
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> GenerarInforme(InformeViewModel filtros)
        {
            var informe = new InformeViewModel
            {
                FechaInicio = filtros.FechaInicio ?? DateTime.Now.AddMonths(-1),
                FechaFin = filtros.FechaFin ?? DateTime.Now,
                LigaId = filtros.LigaId,
                EquipoId = filtros.EquipoId
            };

            // Verifica que los datos no sean null o cero
            Debug.WriteLine($"Jugadores: {informe.EstadisticasJugadores?.Count}");
            Debug.WriteLine($"Partidos ganados equipo: {informe.EstadisticasEquipo?.PartidosGanados}");


            // Obtener estadísticas de jugadores filtradas
            informe.EstadisticasJugadores = await _context.Jugadores
                .Where(j => (!filtros.EquipoId.HasValue || j.EquipoId == filtros.EquipoId) &&
                           (!filtros.LigaId.HasValue || j.Equipo.LigaId == filtros.LigaId))
                .Select(j => new JugadorEstadisticas
                {
                    JugadorEstadisticasId = j.JugadorId,
                    Nombre = j.Nombre,
                    Goles = j.Goles.Count(g => (!filtros.FechaInicio.HasValue || g.Partido.FechaHora >= filtros.FechaInicio) &&
                                              (!filtros.FechaFin.HasValue || g.Partido.FechaHora <= filtros.FechaFin)),
                    // Otras estadísticas...
                })
                .ToListAsync();

            // Obtener estadísticas de equipo
            if (filtros.EquipoId.HasValue)
            {
                var equipo = await _context.Equipos
                    .Include(e => e.PartidosLocal)
                    .Include(e => e.PartidosVisitante)
                    .FirstOrDefaultAsync(e => e.EquipoId == filtros.EquipoId);

                if (equipo != null)
                {
                    var partidosLocal = equipo.PartidosLocal.Where(p => p.Estado == "Finalizado").ToList();
                    var partidosVisitante = equipo.PartidosVisitante.Where(p => p.Estado == "Finalizado").ToList();
                    var todosPartidos = partidosLocal.Concat(partidosVisitante).ToList();

                    informe.EstadisticasEquipo = new EquipoEstadisticas
                    {
                        Nombre = equipo.Nombre,
                        PartidosGanados = partidosLocal.Count(p => p.GolesLocal > p.GolesVisitante) +
                                         partidosVisitante.Count(p => p.GolesVisitante > p.GolesLocal),
                        PartidosEmpatados = todosPartidos.Count(p => p.GolesLocal == p.GolesVisitante),
                        PartidosPerdidos = partidosLocal.Count(p => p.GolesLocal < p.GolesVisitante) +
                                          partidosVisitante.Count(p => p.GolesVisitante < p.GolesLocal),
                        GolesAFavor = partidosLocal.Sum(p => p.GolesLocal ?? 0) +
                                     partidosVisitante.Sum(p => p.GolesVisitante ?? 0),
                        GolesEnContra = partidosLocal.Sum(p => p.GolesVisitante ?? 0) +
                                       partidosVisitante.Sum(p => p.GolesLocal ?? 0)
                    };
                }
            }
            else if (filtros.LigaId.HasValue)
            {
                var partidos = await _context.Partidos
                    .Where(p => p.LigaId == filtros.LigaId && p.Estado == "Finalizado")
                    .ToListAsync();

                var equipos = await _context.Equipos
                    .Where(e => e.LigaId == filtros.LigaId)
                    .ToListAsync();

                informe.EstadisticasEquipo = new EquipoEstadisticas
                {
                    Nombre = "Estadísticas Generales de la Liga",
                    PartidosGanados = partidos.Count(p => p.GolesLocal > p.GolesVisitante),
                    PartidosEmpatados = partidos.Count(p => p.GolesLocal == p.GolesVisitante),
                    PartidosPerdidos = partidos.Count(p => p.GolesLocal < p.GolesVisitante),
                    GolesAFavor = partidos.Sum(p => p.GolesLocal ?? 0),
                    GolesEnContra = partidos.Sum(p => p.GolesVisitante ?? 0),
                    TotalEquipos = equipos.Count
                };
            }

            // Tendencias (últimos 6 meses)
            filtros.Tendencias = Enumerable.Range(0, 6)
                .Select(i => new
                {
                    Periodo = DateTime.Now.AddMonths(-i).ToString("MMM yyyy"),
                    Mes = DateTime.Now.AddMonths(-i).Month,
                    Anio = DateTime.Now.AddMonths(-i).Year
                })
                .Select(p => new Tendencia
                {
                    Periodo = p.Periodo,
                    Goles = _context.Goles.Count(g => g.Jugador.Equipo.LigaId == filtros.LigaId &&
                                                g.Partido.FechaHora.Month == p.Mes &&
                                                g.Partido.FechaHora.Year == p.Anio),
                    // ... otras tendencias
                })
                .OrderBy(t => t.Periodo)
                .ToList();
            return View("Index", informe);
            return View(informe);
            
            
        }

        [HttpPost]
        public IActionResult ExportarPDF(InformeViewModel model)
        {
            var pdfBytes = _pdfGenerator.GenerateInformePdf(model);
            return File(pdfBytes, "application/pdf", $"Informe_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpPost]
        public IActionResult ExportarExcel(InformeViewModel model)
        {
            var excelBytes = _excelExporter.GenerateInformeExcel(model);
            return File(excelBytes,
                      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                      $"Informe_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: Informes/Equipo/5
        public async Task<IActionResult> Equipo(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.Liga)
                .Include(e => e.Jugadores)
                    .ThenInclude(j => j.Estadisticas)
                        .ThenInclude(es => es.Partido)
                .Include(e => e.PartidosLocal)
                .Include(e => e.PartidosVisitante)
                .FirstOrDefaultAsync(e => e.EquipoId == id);

            if (equipo == null)
            {
                return NotFound();
            }

            var partidos = equipo.PartidosLocal
                .Concat(equipo.PartidosVisitante)
                .Where(p => p.Estado == "Finalizado")
                .OrderByDescending(p => p.FechaHora)
                .ToList();

            var viewModel = new InformeEquipoViewModel
            {
                Equipo = equipo,
                PartidosJugados = partidos.Count,
                PartidosGanados = partidos.Count(p =>
                    (p.EquipoLocalId == equipo.EquipoId && p.GolesLocal > p.GolesVisitante) ||
                    (p.EquipoVisitanteId == equipo.EquipoId && p.GolesVisitante > p.GolesLocal)),
                PartidosEmpatados = partidos.Count(p => p.GolesLocal == p.GolesVisitante),
                PartidosPerdidos = partidos.Count(p =>
                    (p.EquipoLocalId == equipo.EquipoId && p.GolesLocal < p.GolesVisitante) ||
                    (p.EquipoVisitanteId == equipo.EquipoId && p.GolesVisitante < p.GolesLocal)),
                GolesAFavor = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0),
                GolesEnContra = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0),
                JugadoresDestacados = equipo.Jugadores
                    .Select(j => new JugadorInformeViewModel
                    {
                        Jugador = j,
                        PartidosJugados = j.Estadisticas.Count,
                        Goles = j.Estadisticas.Sum(e => e.Goles),
                        Asistencias = j.Estadisticas.Sum(e => e.Asistencias),
                        TarjetasAmarillas = j.Estadisticas.Sum(e => e.TarjetasAmarillas),
                        TarjetasRojas = j.Estadisticas.Sum(e => e.TarjetasRojas)
                    })
                    .OrderByDescending(j => j.Goles)
                    .ToList(),
                UltimosPartidos = partidos
                    .Take(5)
                    .Select(p => new PartidoInformeViewModel
                    {
                        Fecha = p.FechaHora,
                        Rival = p.EquipoLocalId == equipo.EquipoId ? p.EquipoVisitante.Nombre : p.EquipoLocal.Nombre,
                        EsLocal = p.EquipoLocalId == equipo.EquipoId,
                        GolesFavor = p.EquipoLocalId == equipo.EquipoId ? p.GolesLocal ?? 0 : p.GolesVisitante ?? 0,
                        GolesContra = p.EquipoLocalId == equipo.EquipoId ? p.GolesVisitante ?? 0 : p.GolesLocal ?? 0,
                        Resultado = p.EquipoLocalId == equipo.EquipoId ?
                            (p.GolesLocal > p.GolesVisitante ? "G" : p.GolesLocal == p.GolesVisitante ? "E" : "P") :
                            (p.GolesVisitante > p.GolesLocal ? "G" : p.GolesVisitante == p.GolesLocal ? "E" : "P")
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: Informes/ExportarExcelEquipo/5
        public async Task<IActionResult> ExportarExcelEquipo(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.Liga)
                .Include(e => e.Jugadores)
                    .ThenInclude(j => j.Estadisticas)
                .Include(e => e.PartidosLocal)
                .Include(e => e.PartidosVisitante)
                .FirstOrDefaultAsync(e => e.EquipoId == id);

            if (equipo == null)
            {
                return NotFound();
            }

            var partidos = equipo.PartidosLocal
                .Concat(equipo.PartidosVisitante)
                .Where(p => p.Estado == "Finalizado")
                .OrderByDescending(p => p.FechaHora)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                // Hoja de resumen
                var worksheet = workbook.Worksheets.Add("Resumen");

                // Título
                worksheet.Cell(1, 1).Value = $"Informe del Equipo: {equipo.Nombre}";
                worksheet.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.FontSize = 16;

                // Datos básicos
                worksheet.Cell(3, 1).Value = "Liga:";
                worksheet.Cell(3, 2).Value = equipo.Liga.Nombre;

                worksheet.Cell(4, 1).Value = "Partidos Jugados:";
                worksheet.Cell(4, 2).Value = partidos.Count;

                worksheet.Cell(5, 1).Value = "Partidos Ganados:";
                worksheet.Cell(5, 2).Value = partidos.Count(p =>
                    (p.EquipoLocalId == equipo.EquipoId && p.GolesLocal > p.GolesVisitante) ||
                    (p.EquipoVisitanteId == equipo.EquipoId && p.GolesVisitante > p.GolesLocal));

                worksheet.Cell(6, 1).Value = "Partidos Empatados:";
                worksheet.Cell(6, 2).Value = partidos.Count(p => p.GolesLocal == p.GolesVisitante);

                worksheet.Cell(7, 1).Value = "Partidos Perdidos:";
                worksheet.Cell(7, 2).Value = partidos.Count(p =>
                    (p.EquipoLocalId == equipo.EquipoId && p.GolesLocal < p.GolesVisitante) ||
                    (p.EquipoVisitanteId == equipo.EquipoId && p.GolesVisitante < p.GolesLocal));

                worksheet.Cell(8, 1).Value = "Goles a Favor:";
                worksheet.Cell(8, 2).Value = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0);

                worksheet.Cell(9, 1).Value = "Goles en Contra:";
                worksheet.Cell(9, 2).Value = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0);

                // Hoja de jugadores
                var jugadoresWorksheet = workbook.Worksheets.Add("Jugadores");

                // Encabezados
                jugadoresWorksheet.Cell(1, 1).Value = "Jugador";
                jugadoresWorksheet.Cell(1, 2).Value = "Posición";
                jugadoresWorksheet.Cell(1, 3).Value = "Partidos";
                jugadoresWorksheet.Cell(1, 4).Value = "Goles";
                jugadoresWorksheet.Cell(1, 5).Value = "Asistencias";
                jugadoresWorksheet.Cell(1, 6).Value = "Tarjetas Amarillas";
                jugadoresWorksheet.Cell(1, 7).Value = "Tarjetas Rojas";

                // Datos
                int row = 2;
                foreach (var jugador in equipo.Jugadores.OrderByDescending(j => j.Estadisticas.Sum(e => e.Goles)))
                {
                    jugadoresWorksheet.Cell(row, 1).Value = jugador.Usuario.NombreCompleto;
                    jugadoresWorksheet.Cell(row, 2).Value = jugador.Posicion;
                    jugadoresWorksheet.Cell(row, 3).Value = jugador.Estadisticas.Count;
                    jugadoresWorksheet.Cell(row, 4).Value = jugador.Estadisticas.Sum(e => e.Goles);
                    jugadoresWorksheet.Cell(row, 5).Value = jugador.Estadisticas.Sum(e => e.Asistencias);
                    jugadoresWorksheet.Cell(row, 6).Value = jugador.Estadisticas.Sum(e => e.TarjetasAmarillas);
                    jugadoresWorksheet.Cell(row, 7).Value = jugador.Estadisticas.Sum(e => e.TarjetasRojas);
                    row++;
                }

                // Ajustar formato
                jugadoresWorksheet.Columns().AdjustToContents();

                // Hoja de partidos
                var partidosWorksheet = workbook.Worksheets.Add("Partidos");

                // Encabezados
                partidosWorksheet.Cell(1, 1).Value = "Fecha";
                partidosWorksheet.Cell(1, 2).Value = "Local";
                partidosWorksheet.Cell(1, 3).Value = "Visitante";
                partidosWorksheet.Cell(1, 4).Value = "Resultado";

                // Datos
                row = 2;
                foreach (var partido in partidos.OrderByDescending(p => p.FechaHora))
                {
                    partidosWorksheet.Cell(row, 1).Value = partido.FechaHora;
                    partidosWorksheet.Cell(row, 2).Value = partido.EquipoLocal.Nombre;
                    partidosWorksheet.Cell(row, 3).Value = partido.EquipoVisitante.Nombre;
                    partidosWorksheet.Cell(row, 4).Value = $"{partido.GolesLocal} - {partido.GolesVisitante}";
                    row++;
                }

                // Ajustar formato
                partidosWorksheet.Columns().AdjustToContents();

                // Generar el archivo
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Informe_{equipo.Nombre}_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // GET: Informes/Jugador/5
        public async Task<IActionResult> Jugador(int id)
        {
            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                    .ThenInclude(e => e.Liga)
                .Include(j => j.Estadisticas)
                    .ThenInclude(e => e.Partido)
                        .ThenInclude(p => p.EquipoLocal)
                .Include(j => j.Estadisticas)
                    .ThenInclude(e => e.Partido)
                        .ThenInclude(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(j => j.JugadorId == id);

            if (jugador == null)
            {
                return NotFound();
            }

            var viewModel = new InformeJugadorViewModel
            {
                Jugador = jugador,
                PartidosJugados = jugador.Estadisticas.Count,
                Goles = jugador.Estadisticas.Sum(e => e.Goles),
                Asistencias = jugador.Estadisticas.Sum(e => e.Asistencias),
                TarjetasAmarillas = jugador.Estadisticas.Sum(e => e.TarjetasAmarillas),
                TarjetasRojas = jugador.Estadisticas.Sum(e => e.TarjetasRojas),
                PromedioGoles = jugador.Estadisticas.Count > 0 ?
                    (double)jugador.Estadisticas.Sum(e => e.Goles) / jugador.Estadisticas.Count : 0,
                PromedioAsistencias = jugador.Estadisticas.Count > 0 ?
                    (double)jugador.Estadisticas.Sum(e => e.Asistencias) / jugador.Estadisticas.Count : 0,
                UltimosPartidos = jugador.Estadisticas
                    .OrderByDescending(e => e.Partido.FechaHora)
                    .Take(5)
                    .Select(e => new PartidoJugadorViewModel
                    {
                        Fecha = e.Partido.FechaHora,
                        Rival = e.Partido.EquipoLocalId == jugador.EquipoId ?
                            e.Partido.EquipoVisitante.Nombre : e.Partido.EquipoLocal.Nombre,
                        Goles = e.Goles,
                        Asistencias = e.Asistencias,
                        TarjetasAmarillas = e.TarjetasAmarillas,
                        TarjetasRojas = e.TarjetasRojas,
                        MinutosJugados = e.MinutosJugados
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: Informes/ExportarExcelJugador/5
        public async Task<IActionResult> ExportarExcelJugador(int id)
        {
            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                .Include(j => j.Estadisticas)
                    .ThenInclude(e => e.Partido)
                        .ThenInclude(p => p.EquipoLocal)
                .Include(j => j.Estadisticas)
                    .ThenInclude(e => e.Partido)
                        .ThenInclude(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(j => j.JugadorId == id);

            if (jugador == null)
            {
                return NotFound();
            }

            using (var workbook = new XLWorkbook())
            {
                // Hoja de resumen
                var worksheet = workbook.Worksheets.Add("Resumen");

                // Título
                worksheet.Cell(1, 1).Value = $"Informe del Jugador: {jugador.Usuario.NombreCompleto}";
                worksheet.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.FontSize = 16;

                // Datos básicos
                worksheet.Cell(3, 1).Value = "Equipo:";
                worksheet.Cell(3, 2).Value = jugador.Equipo.Nombre;

                worksheet.Cell(4, 1).Value = "Posición:";
                worksheet.Cell(4, 2).Value = jugador.Posicion;

                worksheet.Cell(5, 1).Value = "Número:";
                worksheet.Cell(5, 2).Value = jugador.NumeroCamiseta;

                worksheet.Cell(6, 1).Value = "Partidos Jugados:";
                worksheet.Cell(6, 2).Value = jugador.Estadisticas.Count;

                worksheet.Cell(7, 1).Value = "Goles:";
                worksheet.Cell(7, 2).Value = jugador.Estadisticas.Sum(e => e.Goles);

                worksheet.Cell(8, 1).Value = "Asistencias:";
                worksheet.Cell(8, 2).Value = jugador.Estadisticas.Sum(e => e.Asistencias);

                worksheet.Cell(9, 1).Value = "Tarjetas Amarillas:";
                worksheet.Cell(9, 2).Value = jugador.Estadisticas.Sum(e => e.TarjetasAmarillas);

                worksheet.Cell(10, 1).Value = "Tarjetas Rojas:";
                worksheet.Cell(10, 2).Value = jugador.Estadisticas.Sum(e => e.TarjetasRojas);

                worksheet.Cell(11, 1).Value = "Promedio de Goles:";
                worksheet.Cell(11, 2).Value = jugador.Estadisticas.Count > 0 ?
                    (double)jugador.Estadisticas.Sum(e => e.Goles) / jugador.Estadisticas.Count : 0;

                worksheet.Cell(12, 1).Value = "Promedio de Asistencias:";
                worksheet.Cell(12, 2).Value = jugador.Estadisticas.Count > 0 ?
                    (double)jugador.Estadisticas.Sum(e => e.Asistencias) / jugador.Estadisticas.Count : 0;

                // Hoja de partidos
                var partidosWorksheet = workbook.Worksheets.Add("Partidos");

                // Encabezados
                partidosWorksheet.Cell(1, 1).Value = "Fecha";
                partidosWorksheet.Cell(1, 2).Value = "Rival";
                partidosWorksheet.Cell(1, 3).Value = "Goles";
                partidosWorksheet.Cell(1, 4).Value = "Asistencias";
                partidosWorksheet.Cell(1, 5).Value = "Tarjetas Amarillas";
                partidosWorksheet.Cell(1, 6).Value = "Tarjetas Rojas";
                partidosWorksheet.Cell(1, 7).Value = "Minutos";

                // Datos
                int row = 2;
                foreach (var estadistica in jugador.Estadisticas.OrderByDescending(e => e.Partido.FechaHora))
                {
                    partidosWorksheet.Cell(row, 1).Value = estadistica.Partido.FechaHora;
                    partidosWorksheet.Cell(row, 2).Value = estadistica.Partido.EquipoLocalId == jugador.EquipoId ?
                        estadistica.Partido.EquipoVisitante.Nombre : estadistica.Partido.EquipoLocal.Nombre;
                    partidosWorksheet.Cell(row, 3).Value = estadistica.Goles;
                    partidosWorksheet.Cell(row, 4).Value = estadistica.Asistencias;
                    partidosWorksheet.Cell(row, 5).Value = estadistica.TarjetasAmarillas;
                    partidosWorksheet.Cell(row, 6).Value = estadistica.TarjetasRojas;
                    partidosWorksheet.Cell(row, 7).Value = estadistica.MinutosJugados;
                    row++;
                }

                // Ajustar formato
                partidosWorksheet.Columns().AdjustToContents();

                // Generar el archivo
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Informe_{jugador.Usuario.NombreCompleto}_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        public byte[] GenerateInformePdf(InformeViewModel model)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Fuentes
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Título
                document.Add(new Paragraph("Informe de Rendimiento")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetFont(boldFont));

                // Fechas
                document.Add(new Paragraph($"Período: {model.FechaInicio:dd/MM/yyyy} - {model.FechaFin:dd/MM/yyyy}")
                    .SetTextAlignment(TextAlignment.CENTER));
                // Sección de equipo
                if (model.EstadisticasEquipo != null)
                {
                    document.Add(new Paragraph("ESTADÍSTICAS DEL EQUIPO")
                        .SetFontSize(16)
                        .SetFont(boldFont)
                        .SetMarginTop(15));

                    var equipoTable = new Table(2).UseAllAvailableWidth();

                    AddTableRow(equipoTable, "Nombre del equipo", model.EstadisticasEquipo.Nombre, boldFont, normalFont);
                    AddTableRow(equipoTable, "Partidos jugados",
                        (model.EstadisticasEquipo.PartidosGanados ?? 0) +
                        (model.EstadisticasEquipo.PartidosEmpatados ?? 0) +
                        (model.EstadisticasEquipo.PartidosPerdidos ?? 0),
                        boldFont, normalFont);
                    // Agrega más filas...

                    document.Add(equipoTable);
                }

                // Sección de jugadores
                if (model.EstadisticasJugadores?.Any() == true)
                {
                    document.Add(new Paragraph("ESTADÍSTICAS DE JUGADORES")
                        .SetFontSize(16)
                        .SetFont(boldFont)
                        .SetMarginTop(20));

                    var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1, 1, 1, 1 }))
                        .UseAllAvailableWidth();

                    // Encabezados
                    AddTableHeader(table, "Jugador", boldFont);
                    AddTableHeader(table, "Goles", boldFont);
                    // Agrega más encabezados...

                    // Datos
                    foreach (var jugador in model.EstadisticasJugadores)
                    {
                        AddTableCell(table, jugador.Nombre ?? "N/A", normalFont);
                        AddTableCell(table, (jugador.Goles ?? 0).ToString(), normalFont);
                        // Agrega más celdas...
                    }

                    document.Add(table);
                }
                // Tabla de jugadores (verifica que model.EstadisticasJugadores tenga datos)
                if (model.EstadisticasJugadores?.Any() == true)
                {
                    document.Add(new Paragraph("Jugadores").SetFontSize(16).SetFont(boldFont));

                    var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1, 1, 1, 1 }))
                        .UseAllAvailableWidth();

                    // Encabezados
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Jugador").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Goles").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Asistencias").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Amarillas").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Rojas").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Partidos").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Prom. Goles").SetFont(boldFont)));

                    // Datos - Asegúrate que los valores no sean null
                    foreach (var jugador in model.EstadisticasJugadores)
                    {
                        table.AddCell(jugador.Nombre ?? "N/A");
                        table.AddCell((jugador.Goles ?? 0).ToString());
                        table.AddCell((jugador.Asistencias ?? 0).ToString());
                        table.AddCell((jugador.TarjetasAmarillas ?? 0).ToString());
                        table.AddCell((jugador.TarjetasRojas ?? 0).ToString());
                        table.AddCell((jugador.PartidosJugados ?? 0).ToString());
                        table.AddCell((jugador.PromedioGoles ?? 0).ToString("0.00"));
                    }

                    document.Add(table);
                }

                // Estadísticas de equipo (asegúrate que model.EstadisticasEquipo no sea null)
                if (model.EstadisticasEquipo != null)
                {
                    document.Add(new Paragraph("Estadísticas del Equipo")
                        .SetFontSize(16)
                        .SetFont(boldFont)
                        .SetMarginTop(15));

                    var equipoTable = new Table(2).UseAllAvailableWidth();

                    // Añade propiedades con manejo de valores null
                    AddTableRow(equipoTable, "Partidos jugados",
                        (model.EstadisticasEquipo.PartidosGanados ?? 0) +
                        (model.EstadisticasEquipo.PartidosEmpatados ?? 0) +
                        (model.EstadisticasEquipo.PartidosPerdidos ?? 0));

                    AddTableRow(equipoTable, "Partidos ganados", model.EstadisticasEquipo.PartidosGanados);
                    AddTableRow(equipoTable, "Partidos empatados", model.EstadisticasEquipo.PartidosEmpatados);
                    AddTableRow(equipoTable, "Partidos perdidos", model.EstadisticasEquipo.PartidosPerdidos);
                    AddTableRow(equipoTable, "Goles a favor", model.EstadisticasEquipo.GolesAFavor);
                    AddTableRow(equipoTable, "Goles en contra", model.EstadisticasEquipo.GolesEnContra);
                    AddTableRow(equipoTable, "Diferencia de goles",
                        (model.EstadisticasEquipo.GolesAFavor ?? 0) -
                        (model.EstadisticasEquipo.GolesEnContra ?? 0));

                    document.Add(equipoTable);
                }

                document.Close();
                return ms.ToArray();

            }
        }

        // Método auxiliar para añadir filas a la tabla
        private void AddTableRow(Table table, string label, object value)
        {
            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont)));
            table.AddCell(new Cell().Add(new Paragraph(value?.ToString() ?? "0")));
        }
        private void AddTableRow(Table table, string label, object value, PdfFont boldFont, PdfFont normalFont)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont)));
            table.AddCell(new Cell().Add(new Paragraph(value?.ToString() ?? "N/A").SetFont(normalFont)));
        }

        private void AddTableHeader(Table table, string text, PdfFont font)
        {
            table.AddHeaderCell(new Cell().Add(new Paragraph(text).SetFont(font)));
        }

        private void AddTableCell(Table table, string text, PdfFont font)
        {
            table.AddCell(new Cell().Add(new Paragraph(text).SetFont(font)));
        }

        [HttpGet]
        public IActionResult GetEquiposByLiga(int ligaId)
        {
            var equipos = _context.Equipos
                .Where(e => e.LigaId == ligaId)
                .Select(e => new { value = e.EquipoId, text = e.Nombre })
                .ToList();

            return Json(equipos);
        }
    }
}