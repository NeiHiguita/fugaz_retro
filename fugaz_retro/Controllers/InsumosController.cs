using System;
using System.Collections.Generic;
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
    public class InsumosController : Controller
    {
        private readonly FugazContext _context;

        public InsumosController(FugazContext context)
        {
            _context = context;
        }

        // GET: Insumos
        public async Task<IActionResult> Index()
        {
            var fugazContext = _context.Insumos.Include(i => i.IdCategoriaNavigation);
            return View(await fugazContext.ToListAsync());
        }

        // GET: Insumos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Insumos == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.IdCategoriaNavigation)
                .FirstOrDefaultAsync(m => m.IdInsumo == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // GET: Insumos/Create
        public IActionResult Create()
        {
            ViewBag.Categorias = new SelectList(_context.CategoriaInsumos, "IdCategoria", "NombreCategoria");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdInsumo,IdCategoria,NombreInsumo,UnidadMedida,Descripcion")] Insumo insumo)
        {
            if (ModelState.IsValid)
            {
                insumo.Stock = 0; // Establecer el stock a 0
                insumo.Estado = "Agotado"; // Establecer el estado como no disponible
                insumo.PrecioUnitario = 0;

                _context.Add(insumo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Si hay errores de validación, asegúrate de volver a llenar ViewBag para mantener las opciones de categorías en el formulario
            ViewBag.Categorias = new SelectList(_context.CategoriaInsumos, "IdCategoria", "NombreCategoria", insumo.IdCategoria);
            return View(insumo);
        }

        // GET: Insumos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Insumos == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
            {
                return NotFound();
            }
            ViewBag.Categorias = new SelectList(_context.CategoriaInsumos, "IdCategoria", "NombreCategoria", insumo.IdCategoria);
            return View(insumo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdInsumo,IdCategoria,NombreInsumo,UnidadMedida,Cantidad,Descripcion,Stock,PrecioUnitario,Estado")] Insumo insumo)
        {
            if (id != insumo.IdInsumo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(insumo);
                    await _context.SaveChangesAsync(); // Guardar cambios en la base de datos
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InsumoExists(insumo.IdInsumo))
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
            ViewBag.Categorias = new SelectList(_context.CategoriaInsumos, "IdCategoria", "NombreCategoria", insumo.IdCategoria);
            return View(insumo);
        }

        // Método separado para actualizar el stock y el estado automáticamente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int newStock)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
            {
                return NotFound();
            }

            insumo.Stock = newStock;
            UpdateInsumoEstado(insumo);

            try
            {
                _context.Update(insumo);
                await _context.SaveChangesAsync(); // Save changes to the database
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InsumoExists(insumo.IdInsumo))
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

        // Método para actualizar el estado del insumo basado en el stock
        private void UpdateInsumoEstado(Insumo insumo)
        {
            if (insumo.Stock > 3)
            {
                insumo.Estado = "Disponible";
            }
            else
            {
                insumo.Estado = "Agotado";
            }
        }

        // GET: Insumos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Insumos == null)
            {
                return NotFound();
            }

            var insumo = await _context.Insumos
                .Include(i => i.IdCategoriaNavigation)
                .FirstOrDefaultAsync(m => m.IdInsumo == id);
            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        // POST: Insumos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Insumos == null)
            {
                return Problem("Entity set 'FugazContext.Insumos'  is null.");
            }
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo != null)
            {
                _context.Insumos.Remove(insumo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool InsumoExists(int id)
        {
            return _context.Insumos.Any(e => e.IdInsumo == id);
        }
    }
}
