using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class EquiposController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<EquiposController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EquiposController(ApplicationDbContext context, UserManager<Usuario> userManager, ILogger<EquiposController> logger, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Equipos
        public async Task<IActionResult> Index()
        {
            var equipos = await _context.Equipos
                .Include(e => e.Liga)
                .Include(e => e.Jugadores)
                .Include(e => e.Entrenadores)
                .ToListAsync();

            return View(equipos);
        }

        // GET: Equipos/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos
                 .Include(e => e.Entrenadores)
                .Include(e => e.Liga)
                .Include(e => e.Jugadores)
                    .ThenInclude(j => j.Usuario)
                .Include(e => e.Entrenadores)
                    .ThenInclude(e => e.Usuario)
                .Include(e => e.PartidosLocal)
                .Include(e => e.PartidosVisitante)
                .FirstOrDefaultAsync(m => m.EquipoId == id);

            if (equipo == null)
            {
                return NotFound();
            }

            return View(equipo);
        }

        /*// GET: Equipos/Create
        public async Task<IActionResult> Create()
        {
            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre");
            return View();
        }*/
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CrearEquipoViewModel
            {
                // Inicializar las listas para evitar null
                Jugadores = new List<JugadorViewModel>(),
                Entrenadores = new List<EntrenadorViewModel>()
            };

            ViewData["LigaId"] = new SelectList(_context.Ligas, "LigaId", "Nombre");
            ViewBag.UsuariosDisponibles = _userManager.Users.ToList();
            return View(model);
        }

        // POST: Equipos/Create
        /*[HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Create([Bind("EquipoId,Nombre,EscudoUrl,ColorPrincipal,ColorSecundario,LigaId")] Equipo equipo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(equipo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", equipo.LigaId);
            return View(equipo);
        }*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearEquipoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    // Asignar valores por defecto si son nulos
                    var equipo = new Equipo
                    {
                        Nombre = model.Nombre,
                        LigaId = model.LigaId,
                        ColorPrincipal = model.ColorPrincipal ?? "#000000", 
                        ColorSecundario = model.ColorSecundario ?? "#FFFFFF", 
                        EscudoUrl = model.EscudoUrl
                    };

                    _context.Equipos.Add(equipo);
                    await _context.SaveChangesAsync(); // Guardar para obtener ID

                    // Agregar jugadores
                    if (model.Jugadores != null && model.Jugadores.Any())
                    {
                        foreach (var jugadorVm in model.Jugadores)
                        {
                            if (!string.IsNullOrEmpty(jugadorVm.Nombre))
                            {
                                var jugador = new Jugador
                                {
                                    Nombre = jugadorVm.Nombre,
                                    Posicion = jugadorVm.Posicion,
                                    NumeroCamiseta = jugadorVm.NumeroCamiseta,
                                    EquipoId = equipo.EquipoId,
                                    UsuarioId = null 
                                };
                                _context.Jugadores.Add(jugador);
                            }
                        }
                    }

                    // Agregar entrenadores
                    if (model.Entrenadores != null && model.Entrenadores.Any())
                    {
                        var usuarioActual = await _userManager.GetUserAsync(User);

                        foreach (var entrenadorVm in model.Entrenadores)
                        {
                            var entrenador = new Entrenador
                            {
                                Nombre = entrenadorVm.Nombre,
                                Especialidad = entrenadorVm.Especialidad,
                                EquipoId = equipo.EquipoId,
                                UsuarioId = null // O asignar usuario si es necesario
                            };

                            //Si se seleccionó un usuario, actualizar el nombre
                            if (!string.IsNullOrEmpty(entrenadorVm.UsuarioId))
                            {
                                var usuario = await _userManager.FindByIdAsync(entrenadorVm.UsuarioId);
                                entrenador.Nombre = usuario.NombreCompleto;
                                entrenador.UsuarioId = entrenadorVm.UsuarioId;
                            }
                            _context.Entrenadores.Add(entrenador);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = equipo.EquipoId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completo al guardar equipo");
                    ModelState.AddModelError("", $"Ocurrió un error al guardar el equipo: {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex.InnerException, "Inner exception al guardar equipo");
                        ModelState.AddModelError("", $"Detalle: {ex.InnerException.Message}");
                    }

                    ViewData["LigaId"] = new SelectList(_context.Ligas, "LigaId", "Nombre", model.LigaId);
                    return View(model);
                }
            }

            // Si llegamos aquí, algo falló
            ViewData["LigaId"] = new SelectList(_context.Ligas, "LigaId", "Nombre", model.LigaId);
            return View(model);
        }
        private async Task<SelectList> GetLigasSelectListAsync(int? selectedLigaId = null)
        {
            var ligas = await _context.Ligas
                .Where(l => l.Estado == "EnCurso" || l.Estado == "Planificada")
                .OrderBy(l => l.Nombre)
                .ToListAsync();

            return new SelectList(ligas, "LigaId", "Nombre", selectedLigaId);
        }

        // GET: Equipos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", equipo.LigaId);
            return View(equipo);
        }

        // POST: Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EquipoId,Nombre,EscudoUrl,ColorPrincipal,ColorSecundario,LigaId")] Equipo equipo)
        {
            if (id != equipo.EquipoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(equipo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipoExists(equipo.EquipoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", equipo.LigaId);
            return View(equipo);
        }

        // GET: Equipos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos
                .Include(e => e.Liga)
                .FirstOrDefaultAsync(m => m.EquipoId == id);

            if (equipo == null)
            {
                return NotFound();
            }

            return View(equipo);
        }

        // POST: Equipos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            _context.Equipos.Remove(equipo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Equipos/AsignarJugador/5
        public async Task<IActionResult> AsignarJugador(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            var jugadoresDisponibles = await _userManager.GetUsersInRoleAsync("Jugador");
            var jugadoresEnEquipo = await _context.Jugadores
                .Where(j => j.EquipoId == id)
                .Select(j => j.UsuarioId)
                .ToListAsync();

            var jugadoresParaAsignar = jugadoresDisponibles
                .Where(u => !jugadoresEnEquipo.Contains(u.Id))
                .Select(u => new { u.Id, u.NombreCompleto });

            ViewData["EquipoId"] = id;
            ViewData["EquipoNombre"] = equipo.Nombre;
            ViewData["JugadorId"] = new SelectList(jugadoresParaAsignar, "Id", "NombreCompleto");

            return View();
        }

        // POST: Equipos/AsignarJugador/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarJugador(int id, [Bind("JugadorId,Posicion,NumeroCamiseta")] AsignarJugadorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var jugador = new Jugador
                {
                    UsuarioId = model.JugadorId,
                    EquipoId = id,
                    Posicion = model.Posicion,
                    NumeroCamiseta = model.NumeroCamiseta
                };

                _context.Add(jugador);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id });
            }

            // Si llegamos aquí, algo falló, volver a mostrar la vista con los datos
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            var jugadoresDisponibles = await _userManager.GetUsersInRoleAsync("Jugador");
            var jugadoresEnEquipo = await _context.Jugadores
                .Where(j => j.EquipoId == id)
                .Select(j => j.UsuarioId)
                .ToListAsync();

            var jugadoresParaAsignar = jugadoresDisponibles
                .Where(u => !jugadoresEnEquipo.Contains(u.Id))
                .Select(u => new { u.Id, u.NombreCompleto });

            ViewData["EquipoId"] = id;
            ViewData["EquipoNombre"] = equipo.Nombre;
            ViewData["JugadorId"] = new SelectList(jugadoresParaAsignar, "Id", "NombreCompleto", model.JugadorId);

            return View(model);
        }


        private bool EquipoExists(int id)
        {
            return _context.Equipos.Any(e => e.EquipoId == id);
        }
    }
}