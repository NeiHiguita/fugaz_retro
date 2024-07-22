using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fugaz_retro.Models;
using Microsoft.AspNetCore.Authorization;

namespace fugaz_retro.Controllers
{
    [Authorize]
    public class PerdidaInsumosController : Controller
    {
        private readonly FugazContext _context;

        public PerdidaInsumosController(FugazContext context)
        {
            _context = context;
        }

        // GET: PerdidaInsumos
        public async Task<IActionResult> Index()
        {
            var fugazContext = _context.PerdidaInsumos.Include(p => p.IdInsumoNavigation);
            return View(await fugazContext.ToListAsync());
        }

        // GET: PerdidaInsumos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.PerdidaInsumos == null)
            {
                return NotFound();
            }

            var perdidaInsumo = await _context.PerdidaInsumos
                .Include(p => p.IdInsumoNavigation)
                .FirstOrDefaultAsync(m => m.IdPerdidaInsumo == id);
            if (perdidaInsumo == null)
            {
                return NotFound();
            }

            return View(perdidaInsumo);
        }

        // GET: PerdidaInsumos/Create
        public IActionResult Create()
        {
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo");
            return View();
        }

        // POST: PerdidaInsumos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPerdidaInsumo,IdInsumo,Cantidad,Fecha")] PerdidaInsumo perdidaInsumo)
        {
            if (ModelState.IsValid)
            {
                // Buscar el insumo asociado
                var insumo = await _context.Insumos.FindAsync(perdidaInsumo.IdInsumo);
                if (insumo != null)
                {
                    // Restar la cantidad perdida del stock
                    insumo.Stock -= perdidaInsumo.Cantidad;

                    // Actualizar el estado del insumo según el nuevo stock
                    insumo.Estado = insumo.Stock > 3 ? "Disponible" : "Agotado";

                    _context.Update(insumo);
                }

                _context.Add(perdidaInsumo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", perdidaInsumo.IdInsumo);
            return View(perdidaInsumo);
        }

        // GET: PerdidaInsumos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.PerdidaInsumos == null)
            {
                return NotFound();
            }

            var perdidaInsumo = await _context.PerdidaInsumos.FindAsync(id);
            if (perdidaInsumo == null)
            {
                return NotFound();
            }
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", perdidaInsumo.IdInsumo);
            return View(perdidaInsumo);
        }

        // POST: PerdidaInsumos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPerdidaInsumo,IdInsumo,Cantidad,Fecha")] PerdidaInsumo perdidaInsumo)
        {
            if (id != perdidaInsumo.IdPerdidaInsumo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(perdidaInsumo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PerdidaInsumoExists(perdidaInsumo.IdPerdidaInsumo))
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
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", perdidaInsumo.IdInsumo);
            return View(perdidaInsumo);
        }

        // GET: PerdidaInsumos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.PerdidaInsumos == null)
            {
                return NotFound();
            }

            var perdidaInsumo = await _context.PerdidaInsumos
                .Include(p => p.IdInsumoNavigation)
                .FirstOrDefaultAsync(m => m.IdPerdidaInsumo == id);
            if (perdidaInsumo == null)
            {
                return NotFound();
            }

            return View(perdidaInsumo);
        }

        // POST: PerdidaInsumos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.PerdidaInsumos == null)
            {
                return Problem("Entity set 'FugazContext.PerdidaInsumos' is null.");
            }
            var perdidaInsumo = await _context.PerdidaInsumos.FindAsync(id);
            if (perdidaInsumo != null)
            {
                _context.PerdidaInsumos.Remove(perdidaInsumo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PerdidaInsumoExists(int id)
        {
            return (_context.PerdidaInsumos?.Any(e => e.IdPerdidaInsumo == id)).GetValueOrDefault();
        }
    }
}
