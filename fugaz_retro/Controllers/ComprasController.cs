using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fugaz_retro.Models;
using System.Text.Json.Serialization;
using System.Text.Json;


namespace fugaz_retro.Controllers
{
    public class ComprasController : Controller
    {
        private readonly FugazContext _context;

        public ComprasController(FugazContext context)
        {
            _context = context;
        }

        // GET: Compras
        public IActionResult Index()
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve // Preservar referencias
            };

            var compras = _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .ToList();

            // Serializar objetos compras con referencias preservadas
            var comprasJson = JsonSerializer.Serialize(compras, options);

            ViewData["ComprasJson"] = comprasJson;

            return View(compras);
        }


        // GET: Compras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var compra = await _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .Include(c => c.DetalleCompras) // Incluir los detalles de compra
                    .ThenInclude(dc => dc.IdInsumoNavigation) // Incluir la información del insumo
                .FirstOrDefaultAsync(m => m.IdCompra == id);

            if (compra == null)
            {
                return NotFound();
            }

            return View(compra);
        }


        // GET: Compras/Create

        public IActionResult Create()
        {
            ViewBag.Proveedores = _context.Proveedors.ToList(); // Cargar todos los proveedores
            ViewBag.Insumos = _context.Insumos.ToList(); // Cargar todos los insumos
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
                            _context.DetalleCompras.Add(detalle);

                            // Actualizar el stock del insumo
                            var insumo = await _context.Insumos.FindAsync(detalle.IdInsumo);
                            if (insumo != null)
                            {
                                insumo.Stock += detalle.Cantidad; // Incrementar el stock del insumo
                                insumo.PrecioUnitario = detalle.PrecioUnitario; // Actualizar el precio unitario del insumo
                                // Actualizar el estado del insumo según el stock
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

            // Re-load view data if the model state is invalid
            // Aquí puedes cargar nuevamente los datos necesarios para la vista, si es necesario

            return View(compra);
        }


        public ActionResult GetProveedoresPorTipo(string tipoProveedor)
        {
            List<Proveedor> proveedores = new List<Proveedor>();

            if (tipoProveedor == "Natural")
            {
                proveedores = _context.Proveedors.Where(p => !string.IsNullOrEmpty(p.NombreCompleto)).ToList();
            }
            else if (tipoProveedor == "Juridico")
            {
                proveedores = _context.Proveedors.Where(p => !string.IsNullOrEmpty(p.RepresentanteLegal)).ToList();
            }

            return Json(proveedores); // Devolver la lista de proveedores en formato JSON
        }




        // GET: Compras/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Compras == null)
            {
                return NotFound();
            }

            var compra = await _context.Compras.FindAsync(id);
            if (compra == null)
            {
                return NotFound();
            }
            ViewData["IdProveedor"] = new SelectList(_context.Proveedors, "IdProveedor", "TipoProveedor", compra.IdProveedor);
            return View(compra);
        }

        // POST: Compras/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCompra,IdUsuario,IdProveedor,FechaCompra,MetodoPago,Subtotal,Iva,Descuento,PrecioTotal")] Compra compra)
        {
            if (id != compra.IdCompra)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(compra);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompraExists(compra.IdCompra))
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
            ViewData["IdProveedor"] = new SelectList(_context.Proveedors, "IdProveedor", "IdProveedor", compra.IdProveedor);
            return View(compra);
        }

        // GET: Compras/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Compras == null)
            {
                return NotFound();
            }

            var compra = await _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .FirstOrDefaultAsync(m => m.IdCompra == id);
            if (compra == null)
            {
                return NotFound();
            }

            return View(compra);
        }

        // POST: Compras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Compras == null)
            {
                return Problem("Entity set 'FugazContext.Compras'  is null.");
            }
            var compra = await _context.Compras.FindAsync(id);
            if (compra != null)
            {
                _context.Compras.Remove(compra);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); 
        }

        private bool CompraExists(int id)
        {
            return (_context.Compras?.Any(e => e.IdCompra == id)).GetValueOrDefault();
        }
    }
}
