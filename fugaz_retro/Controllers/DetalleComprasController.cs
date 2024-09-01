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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Compra compra, List<DetalleCompra> detallesCompra)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Compras.Add(compra);
                        await _context.SaveChangesAsync();

                        foreach (var detalle in detallesCompra)
                        {
                            detalle.IdCompra = compra.IdCompra;

                            detalle.Cantidad = detalle.Cantidad;

                            // Log para verificar los valores antes de guardar
                            Console.WriteLine($"Cantidad ajustada: {detalle.Cantidad}");
                            Console.WriteLine($"PrecioUnitario recibido: {detalle.PrecioUnitario}");

                            _context.DetalleCompras.Add(detalle);

                            // Actualizar el stock del insumo
                            var insumo = await _context.Insumos.FindAsync(detalle.IdInsumo);
                            if (insumo != null)
                            {
                                insumo.Stock += detalle.Cantidad;
                                insumo.PrecioUnitario = detalle.PrecioUnitario;
                                insumo.Estado = insumo.Stock > 3 ? "Disponible" : "Agotado";

                                _context.Insumos.Update(insumo);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ModelState.AddModelError("", $"Error al crear la compra. Detalles del error: {innerExceptionMessage}");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "ModelState no es válido.");
            }

            ViewBag.Proveedores = _context.Proveedors.ToList(); // Cargar todos los proveedores
            ViewBag.Insumos = _context.Insumos.ToList(); // Cargar todos los insumos

            return View(compra);
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
            return _context.DetalleCompras.Any(e => e.IdDetalleCompra == id);
        }
    }
}