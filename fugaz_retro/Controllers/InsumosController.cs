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
    [PermisosFilter("Modulo Compras")]
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
        public IActionResult Details(int id)
        {
            var insumo = _context.Insumos
                .Include(i => i.IdCategoriaNavigation) // Asegúrate de incluir las propiedades de navegación necesarias
                .FirstOrDefault(i => i.IdInsumo == id);

            if (insumo == null)
            {
                return NotFound();
            }

            // Retorna los datos en formato JSON
            return Json(new
            {
                nombreInsumo = insumo.NombreInsumo,
                idCategoriaNavigation = insumo.IdCategoriaNavigation?.NombreCategoria,
                categoriaInsumo = insumo.CategoriaInsumo,
                stock = insumo.Stock,
                estado = insumo.Stock > 3 ? "Disponible" : "Agotado",
                precioUnitario = insumo.PrecioUnitario
            });
        }



        // GET: Insumos/Create
        public IActionResult Create()
        {
            // Obtener solo las categorías activas
            var categoriasActivas = _context.CategoriaInsumos
                .Where(c => c.EstadoCategoria) // Filtra las categorías que están activas
                .ToList();

            // Cargar las categorías activas en el ViewBag
            ViewBag.Categorias = new SelectList(categoriasActivas, "IdCategoria", "NombreCategoria");

            return View();
        }

        // POST: Insumos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdInsumo,IdCategoria,CategoriaInsumo,NombreInsumo,UnidadMedida,Descripcion,Cantidad")] Insumo insumo)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .ToList();

                    return Json(new { success = false, errors = errors });
                }

                // Set default values
                insumo.Stock = 0;
                insumo.Estado = "Agotado";
                insumo.PrecioUnitario = 0;

                _context.Add(insumo);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception (consider using a logging framework)
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, errors = new List<string> { "There was an error processing your request." } });
            }
        }


        // GET: Insumo/Edit/5
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

        // POST: Insumo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdInsumo,NombreInsumo,Descripcion")] Insumo insumo)
        {
            if (id != insumo.IdInsumo)
            {
                return NotFound();
            }

            // Recuperar el insumo original de la base de datos
            var originalInsumo = await _context.Insumos.FindAsync(id);

            if (originalInsumo == null)
            {
                return NotFound();
            }

            // Actualizar solo los campos editables
            originalInsumo.NombreInsumo = insumo.NombreInsumo;
            originalInsumo.Descripcion = insumo.Descripcion;

            try
            {
                _context.Update(originalInsumo);
                await _context.SaveChangesAsync();

                // Retornar un JSON indicando éxito
                return Json(new { success = true });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InsumoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    // Manejar cualquier otro error en la actualización
                    throw;
                }
            }
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
        private bool InsumoExists(int id)
        {
            return _context.Insumos.Any(e => e.IdInsumo == id);
        }
    }
}
