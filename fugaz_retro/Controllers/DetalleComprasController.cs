using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fugaz_retro.Models;

namespace fugaz_retro.Controllers
{
    public class DetalleComprasController : Controller
    {
        private readonly FugazContext _context;

        public DetalleComprasController(FugazContext context)
        {
            _context = context;
        }

        // GET: DetalleCompras
        public async Task<IActionResult> Index()
        {
            var detalleCompras = await _context.DetalleCompras
                .Include(d => d.IdCompraNavigation)
                .Include(d => d.IdInsumoNavigation)
                .ToListAsync();

            return View(detalleCompras);
        }

        // GET: DetalleCompras/Create
        public IActionResult Create()
        {
            ViewData["IdCompra"] = new SelectList(_context.Compras, "IdCompra", "FechaCompra");
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo");
            return View();
        }

        // POST: DetalleCompras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetalleCompra,IdCompra,IdInsumo,Cantidad")] DetalleCompra detalleCompra)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detalleCompra);
                await _context.SaveChangesAsync();

                // Actualizar el stock en la tabla de Insumo
                var insumo = await _context.Insumos.FindAsync(detalleCompra.IdInsumo);
                if (insumo != null)
                {
                    insumo.Stock += detalleCompra.Cantidad;
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCompra"] = new SelectList(_context.Compras, "IdCompra", "FechaCompra", detalleCompra.IdCompra);
            ViewData["IdInsumo"] = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", detalleCompra.IdInsumo);
            return View(detalleCompra);
        }

        // POST: DetalleCompras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalleCompra = await _context.DetalleCompras.FindAsync(id);
            if (detalleCompra == null)
            {
                return NotFound();
            }

            // Restaurar el stock en la tabla de Insumo
            var insumo = await _context.Insumos.FindAsync(detalleCompra.IdInsumo);
            if (insumo != null)
            {
                insumo.Stock -= detalleCompra.Cantidad;
                await _context.SaveChangesAsync();
            }

            _context.DetalleCompras.Remove(detalleCompra);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetalleCompraExists(int id)
        {
            return (_context.DetalleCompras?.Any(e => e.IdDetalleCompra == id)).GetValueOrDefault();
        }
    }
}