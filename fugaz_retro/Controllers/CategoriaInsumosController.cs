using fugaz_retro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fugaz_retro.Controllers
{
    [Authorize]
    [PermisosFilter("Modulo Compras")]
    public class CategoriaInsumosController : Controller
    {
        private readonly FugazContext _context;

        public CategoriaInsumosController(FugazContext context)
        {
            _context = context;
        }

        // GET: CategoriaInsumos
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.CategoriaInsumos.ToListAsync();
            var viewModel = new CategoriaInsumoViewModel
            {
                Categorias = categorias
            };
            return View(viewModel);
        }

        // GET: CategoriaInsumos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var categoria = await _context.CategoriaInsumos
                .Where(c => c.IdCategoria == id)
                .Select(c => new {
                    NombreCategoria = c.NombreCategoria,
                    EstadoCategoria = c.EstadoCategoria
                })
                .FirstOrDefaultAsync();

            if (categoria == null)
            {
                return NotFound();
            }

            return Json(categoria);
        }


        // GET: CategoriaInsumos/Create
        public IActionResult Create()
        {
            var categoriaInsumo = new CategoriaInsumo
            {
                EstadoCategoria = true // Estado activo por defecto
            };
            return View(categoriaInsumo);
        }

        // POST: CategoriaInsumos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCategoria,NombreCategoria,EstadoCategoria")] CategoriaInsumo categoriaInsumo)
        {
            if (ModelState.IsValid)
            {

                // Establece EstadoCategoria a true antes de guardar
                categoriaInsumo.EstadoCategoria = true;

                _context.Add(categoriaInsumo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoriaInsumo);
        }


        [HttpPost]
        public JsonResult CheckCategoriaExists(string nombreCategoria)
        {
            var exists = _context.CategoriaInsumos.Any(c => c.NombreCategoria == nombreCategoria);
            return Json(exists);
        }

        // POST: CategoriaInsumos/Create desde el modal
        [HttpPost]
        public async Task<IActionResult> CreateModal([FromBody] CategoriaInsumoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                viewModel.NuevaCategoria.EstadoCategoria = true;
                _context.CategoriaInsumos.Add(viewModel.NuevaCategoria);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, error = "Error al crear la categoría." });
        }

        // POST: CategoriaInsumos/ToggleEstado/5
        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var categoriaInsumo = await _context.CategoriaInsumos.FindAsync(id);
            if (categoriaInsumo == null)
            {
                return NotFound();
            }

            categoriaInsumo.EstadoCategoria = !categoriaInsumo.EstadoCategoria;
            _context.Update(categoriaInsumo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: CategoriaInsumos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CategoriaInsumos == null)
            {
                return NotFound();
            }

            var categoriaInsumo = await _context.CategoriaInsumos.FindAsync(id);
            if (categoriaInsumo == null)
            {
                return NotFound();
            }
            return View(categoriaInsumo);
        }

        // POST: CategoriaInsumos/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] CategoriaInsumoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Modelo inválido" });
            }

            var categoria = model.NuevaCategoria;

            // Verificar si la categoría existe
            var existingCategoria = await _context.CategoriaInsumos
                .FirstOrDefaultAsync(c => c.IdCategoria == categoria.IdCategoria);

            if (existingCategoria == null)
            {
                return NotFound(new { error = "Categoría no encontrada." });
            }

            try
            {
                // Actualizar los campos
                existingCategoria.NombreCategoria = categoria.NombreCategoria;
                //existingCategoria.EstadoCategoria = categoria.EstadoCategoria;

                _context.CategoriaInsumos.Update(existingCategoria);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Registrar la excepción y devolver un error genérico
                // _logger.LogError(ex, "Error al actualizar la categoría"); // Si usas un logger
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        public IActionResult GetCategoria(int id)
        {
            var categoria = _context.CategoriaInsumos
                                    .Where(c => c.IdCategoria == id)
                                    .Select(c => new
                                    {
                                        c.IdCategoria,
                                        c.NombreCategoria
                                    })
                                    .FirstOrDefault();

            if (categoria == null)
            {
                return NotFound();
            }

            return Json(categoria);
        }


        // GET: CategoriaInsumos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CategoriaInsumos == null)
            {
                return NotFound();
            }

            var categoriaInsumo = await _context.CategoriaInsumos
                .FirstOrDefaultAsync(m => m.IdCategoria == id);
            if (categoriaInsumo == null)
            {
                return NotFound();
            }

            return View(categoriaInsumo);
        }

        // POST: CategoriaInsumos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CategoriaInsumos == null)
            {
                return Problem("Entity set 'FugazContext.CategoriaInsumos'  is null.");
            }
            var categoriaInsumo = await _context.CategoriaInsumos.FindAsync(id);
            if (categoriaInsumo != null)
            {
                _context.CategoriaInsumos.Remove(categoriaInsumo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: CategoriaInsumos/GetCategoriaInsumoDetails/5
        public async Task<IActionResult> GetCategoriaInsumoDetails(int? id)
        {
            if (id == null || _context.CategoriaInsumos == null)
            {
                return NotFound();
            }

            var categoriaInsumo = await _context.CategoriaInsumos.FirstOrDefaultAsync(m => m.IdCategoria == id);
            if (categoriaInsumo == null)
            {
                return NotFound();
            }

            return Json(categoriaInsumo);
        }

        // POST: CategoriaInsumos/DeleteCategoriaInsumo/5
        [HttpPost]
        public async Task<IActionResult> DeleteCategoriaInsumo(int id)
        {
            if (_context.CategoriaInsumos == null)
            {
                return Problem("Entity set 'FugazContext.CategoriaInsumos' is null.");
            }

            var categoriaInsumo = await _context.CategoriaInsumos.FindAsync(id);
            if (categoriaInsumo == null)
            {
                return NotFound();
            }

            if (_context.Insumos.Any(i => i.IdCategoria == id))
            {
                return BadRequest("La categoría de insumo no puede ser eliminada porque tiene uno o varios insumos asociados.");
            }

            _context.CategoriaInsumos.Remove(categoriaInsumo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool CategoriaInsumoExists(int id)
        {
            return (_context.CategoriaInsumos?.Any(e => e.IdCategoria == id)).GetValueOrDefault();
        }
    }
}