using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize]
    public class JugadoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public JugadoresController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Jugadores/Perfil/5
        [AllowAnonymous]
        public async Task<IActionResult> Perfil(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            return View(jugador);
        }

        // GET: Jugadores/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return NotFound();
            }

            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                    .ThenInclude(e => e.Liga)
                .Include(j => j.Estadisticas)
                    .ThenInclude(e => e.Partido)
                .FirstOrDefaultAsync(j => j.UsuarioId == usuario.Id);

            if (jugador == null)
            {
                return NotFound();
            }

            return View("Perfil", jugador);
        }

        // GET: Jugadores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                .FirstOrDefaultAsync(j => j.JugadorId == id);

            if (jugador == null)
            {
                return NotFound();
            }

            // Verificar que el usuario es el dueño del perfil o es administrador
            var usuario = await _userManager.GetUserAsync(User);
            if (jugador.UsuarioId != usuario.Id && !User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            ViewData["EquipoId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", jugador.EquipoId);
            return View(jugador);
        }

        // POST: Jugadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("JugadorId,UsuarioId,EquipoId,Posicion,NumeroCamiseta")] Jugador jugador)
        {
            if (id != jugador.JugadorId)
            {
                return NotFound();
            }

            // Verificar que el usuario es el dueño del perfil o es administrador
            var existingJugador = await _context.Jugadores.AsNoTracking().FirstOrDefaultAsync(j => j.JugadorId == id);
            var usuario = await _userManager.GetUserAsync(User);
            if (existingJugador.UsuarioId != usuario.Id && !User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(jugador);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JugadorExists(jugador.JugadorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return RedirectToAction(nameof(MiPerfil));
                }
            }

            ViewData["EquipoId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", jugador.EquipoId);
            return View(jugador);
        }

        private bool JugadorExists(int id)
        {
            return _context.Jugadores.Any(e => e.JugadorId == id);
        }
    }
}