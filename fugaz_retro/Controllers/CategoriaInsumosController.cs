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


        [HttpPost]
        public JsonResult CheckCategoriaExists(string nombreCategoria)
        {
            var exists = _context.CategoriaInsumos.Any(c => c.NombreCategoria == nombreCategoria);
            return Json(exists);
        }

        // POST: CategoriaInsumos/Create desde el modal
        [HttpPost]
        public IActionResult CreateModal(string nombreCategoria)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria))
            {
                return Json(new { success = false, error = "El nombre de la categoría no puede estar vacío." });
            }

            // Verificar si ya existe una categoría con el mismo nombre
            var categoriaExiste = _context.CategoriaInsumos.Any(c => c.NombreCategoria == nombreCategoria);
            if (categoriaExiste)
            {
                return Json(new { success = false, error = "Ya existe una categoría con este nombre." });
            }

            // Crear la nueva categoría con estado activo
            var nuevaCategoria = new CategoriaInsumo
            {
                NombreCategoria = nombreCategoria,
                EstadoCategoria = true // Establecer el estado como activo
            };

            _context.CategoriaInsumos.Add(nuevaCategoria);
            _context.SaveChanges();

            return Json(new { success = true, categoria = new { idCategoria = nuevaCategoria.IdCategoria, nombreCategoria = nuevaCategoria.NombreCategoria, estadoCategoria = nuevaCategoria.EstadoCategoria } });
        }



        [HttpPost]
        public IActionResult UpdateEstado(int idCategoria, bool estadoCategoria)
        {
            var categoria = _context.CategoriaInsumos.FirstOrDefault(c => c.IdCategoria == idCategoria);

            if (categoria == null)
            {
                return Json(new { success = false, error = "Categoría no encontrada." });
            }

            categoria.EstadoCategoria = estadoCategoria;

            try
            {
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { success = false, error = "Hubo un problema al actualizar el estado." });
            }
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



        [HttpGet]
        public IActionResult GetCategoria(int idCategoria)
        {
            var categoria = _context.CategoriaInsumos
                .Where(c => c.IdCategoria == idCategoria)
                .Select(c => new {
                    idCategoria = c.IdCategoria,
                    nombreCategoria = c.NombreCategoria,
                    estadoCategoria = c.EstadoCategoria
                })
                .FirstOrDefault();

            if (categoria == null)
            {
                return Json(new { success = false, error = "Categoría no encontrada." });
            }

            return Json(new { success = true, categoria });
        }
        [HttpPost]
        public IActionResult EditModal(int idCategoria, string nombreCategoria)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria))
            {
                return Json(new { success = false, error = "El nombre de la categoría no puede estar vacío." });
            }

            var categoria = _context.CategoriaInsumos.Find(idCategoria);
            if (categoria == null)
            {
                return Json(new { success = false, error = "Categoría no encontrada." });
            }

            // Verificar si ya existe otra categoría con el mismo nombre
            var categoriaExiste = _context.CategoriaInsumos.Any(c => c.NombreCategoria == nombreCategoria && c.IdCategoria != idCategoria);
            if (categoriaExiste)
            {
                return Json(new { success = false, error = "Ya existe una categoría con este nombre." });
            }

            categoria.NombreCategoria = nombreCategoria;
            _context.SaveChanges();

            return Json(new { success = true, categoria = new { idCategoria = categoria.IdCategoria, nombreCategoria = categoria.NombreCategoria, estadoCategoria = categoria.EstadoCategoria } });
        }




        [HttpGet]
        public IActionResult GetCategoriaDetails(int idCategoria)
        {
            var categoria = _context.CategoriaInsumos
                .Where(c => c.IdCategoria == idCategoria)
                .Select(c => new {
                    nombreCategoria = c.NombreCategoria,
                    estadoCategoria = c.EstadoCategoria
                })
                .FirstOrDefault();

            if (categoria == null)
            {
                return Json(new { success = false, error = "Categoría no encontrada." });
            }

            return Json(new { success = true, categoria });
        }


        private bool CategoriaInsumoExists(int id)
        {
            return (_context.CategoriaInsumos?.Any(e => e.IdCategoria == id)).GetValueOrDefault();
        }
    }
}