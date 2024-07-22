using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fugaz_retro.Models;

namespace fugaz_retro.Controllers
{
    public class DetalleProductosController : Controller
    {
        private readonly FugazContext _context;

        public DetalleProductosController(FugazContext context)
        {
            _context = context;
        }

        // GET: DetalleProductos
        public async Task<IActionResult> Index()
        {
            var fugazContext = _context.DetalleProductos.Include(u => u.Producto);
            return View(await fugazContext.ToListAsync());
        }

        // GET: DetalleProducto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleProducto = await _context.DetalleProductos
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdDetalleProducto == id);

            if (detalleProducto == null)
            {
                return NotFound();
            }

            return View(detalleProducto);
        }



        // GET: DetalleProductos/Create
        public IActionResult Create()
        {
            ViewBag.IdProducto = new SelectList(_context.Productos, "IdProducto", "NombreProducto");
            return View();
        }

        // POST: DetalleProductos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DetalleProducto detalleProducto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detalleProducto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdProducto = new SelectList(_context.Productos, "IdProducto", "NombreProducto", detalleProducto.IdProducto);
            return View(detalleProducto);
        }

        // GET: DetalleProductos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleProducto = await _context.DetalleProductos
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdDetalleProducto == id);

            if (detalleProducto == null)
            {
                return NotFound();
            }

            ViewBag.IdProducto = new SelectList(_context.Productos, "IdProducto", "NombreProducto", detalleProducto.IdProducto);
            return View(detalleProducto);
        }




        // POST: DetalleProductos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DetalleProducto detalleProducto)
        {
            if (id != detalleProducto.IdDetalleProducto)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detalleProducto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetalleProductoExists(detalleProducto.IdDetalleProducto))
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
            ViewBag.IdProducto = new SelectList(_context.Productos, "IdProducto", "NombreProducto", detalleProducto.IdProducto);
            return View(detalleProducto);
        }

        // GET: DetalleProductos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleProducto = await _context.DetalleProductos.FirstOrDefaultAsync(m => m.IdDetalleProducto == id);
            if (detalleProducto == null)
            {
                return NotFound();
            }

            return View(detalleProducto);
        }

        // POST: DetalleProductos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalleProducto = await _context.DetalleProductos.FindAsync(id);
            _context.DetalleProductos.Remove(detalleProducto);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetalleProductoExists(int id)
        {
            return _context.DetalleProductos.Any(e => e.IdDetalleProducto == id);
        }
    }
}
