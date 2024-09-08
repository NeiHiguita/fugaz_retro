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
    [PermisosFilter("Modulo Ventas")]

    public class DetalleInsumosController : Controller
    {
        private readonly FugazContext _context;

        public DetalleInsumosController(FugazContext context)
        {
            _context = context;
        }

        // GET: DetalleInsumos
        public async Task<IActionResult> Index()
        {
            var detalleInsumos = await _context.DetalleInsumos
                .Include(di => di.DetalleProducto)
                .ThenInclude(dp => dp.Producto) // Asegurarse de incluir Producto
                .Include(di => di.Insumo)
                .ToListAsync();
            return View(detalleInsumos);
        }

        // GET: DetalleInsumos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleInsumo = await _context.DetalleInsumos
                .Include(di => di.DetalleProducto)
                .Include(di => di.Insumo)
                .FirstOrDefaultAsync(di => di.IdDetalleInsumo == id);

            if (detalleInsumo == null)
            {
                return NotFound();
            }

            return View(detalleInsumo);
        }

        // GET: DetalleInsumos/Create
        public IActionResult Create(int? idDetalleProducto)
        {
            // Validar si se proporcionó un idDetalleProducto
            if (idDetalleProducto == null)
            {
                return BadRequest("El IdDetalleProducto es null.");
            }

            // Verificar si el IdDetalleProducto existe en la base de datos
            var detalleProducto = _context.DetalleProductos
                .Include(dp => dp.Producto)
                .FirstOrDefault(dp => dp.IdDetalleProducto == idDetalleProducto.Value);

            if (detalleProducto == null)
            {
                return NotFound("El IdDetalleProducto no existe.");
            }

            // Obtener el IdProducto
            var idProducto = detalleProducto.IdProducto;

            // Obtener todas las tallas asociadas al IdProducto, excluyendo valores nulos o en blanco
            var tallas = _context.DetalleProductos
                .Where(dp => dp.IdProducto == idProducto && !string.IsNullOrWhiteSpace(dp.Talla))
                .Select(dp => dp.Talla)
                .Distinct()
                .ToList();

            // Obtener todos los colores asociados al IdProducto, excluyendo valores nulos o en blanco
            var colores = _context.DetalleProductos
                .Where(dp => dp.IdProducto == idProducto && !string.IsNullOrWhiteSpace(dp.Color))
                .Select(dp => dp.Color)
                .Distinct()
                .ToList();
            // Filtrar insumos que no estén en la categoría "Insumo Gasto"
            var insumosFiltrados = _context.Insumos
                .Where(i => i.CategoriaInsumo != "Insumo Gasto") // Asegúrate de que la propiedad se llama así y corresponde a tu modelo
                .Select(i => new SelectListItem
                {
                    Value = i.IdInsumo.ToString(),
                    Text = i.NombreInsumo
                })
                .ToList();

            // Rellenar el ViewBag con los detalles del producto, insumo, tallas y colores
            ViewBag.NombreProducto = detalleProducto.Producto.NombreProducto;
            ViewBag.Insumos = new SelectList(insumosFiltrados, "Value", "Text");
            ViewBag.Tallas = tallas;
            ViewBag.Colores = colores;
            ViewBag.FotoProducto = detalleProducto.Producto?.Foto; // Agregar esta línea para pasar la foto del producto

            // Crear un nuevo modelo DetalleInsumo y asignar el IdDetalleProducto
            var detalleInsumo = new DetalleInsumo
            {
                IdDetalleProducto = detalleProducto.IdDetalleProducto,
                Color = detalleProducto.Color,
            };

            // Devolver la vista con el modelo.
            return View(detalleInsumo);
        }

        // POST: DetalleInsumos/Create
        [HttpPost]
        public IActionResult Create(DetalleProducto model, List<DetalleInsumo> detalleInsumo)
        {
            if (ModelState.IsValid)
            {
                // Guardar los detalles en la base de datos
                foreach (var detalle in detalleInsumo)
                {
                    _context.DetalleInsumos.Add(detalle);
                }
                _context.SaveChanges();
                return RedirectToAction("Index", "Productos", new { success = true });
            }
            return View(model);
        }


        // GET: DetalleInsumos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleInsumo = await _context.DetalleInsumos.FindAsync(id);
            if (detalleInsumo == null)
            {
                return NotFound();
            }

            var detalleProductos = _context.DetalleProductos
                .Include(dp => dp.Producto)
                .Select(dp => new {
                    IdDetalleProducto = dp.IdDetalleProducto,
                    DisplayText = dp.Producto.NombreProducto + " - " + dp.Color
                }).ToList();

            ViewBag.IdDetalleProducto = new SelectList(detalleProductos, "IdDetalleProducto", "DisplayText", detalleInsumo.IdDetalleProducto);
            ViewBag.IdInsumo = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", detalleInsumo.IdInsumo);
            return View(detalleInsumo);
        }

        // POST: DetalleInsumos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetalleInsumo,IdDetalleProducto,IdInsumo,Cantidad")] DetalleInsumo detalleInsumo)
        {
            if (id != detalleInsumo.IdDetalleInsumo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detalleInsumo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetalleInsumoExists(detalleInsumo.IdDetalleInsumo))
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

            var detalleProductos = _context.DetalleProductos
                .Include(dp => dp.Producto)
                .Select(dp => new {
                    IdDetalleProducto = dp.IdDetalleProducto,
                    DisplayText = dp.Producto.NombreProducto + " - " + dp.Color
                }).ToList();

            ViewBag.IdDetalleProducto = new SelectList(detalleProductos, "IdDetalleProducto", "DisplayText", detalleInsumo.IdDetalleProducto);
            ViewBag.IdInsumo = new SelectList(_context.Insumos, "IdInsumo", "NombreInsumo", detalleInsumo.IdInsumo);
            return View(detalleInsumo);
        }



        private bool DetalleInsumoExists(int id)
        {
            return _context.DetalleInsumos.Any(di => di.IdDetalleInsumo == id);
        }
    }
}