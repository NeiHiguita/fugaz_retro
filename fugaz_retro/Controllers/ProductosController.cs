using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using fugaz_retro.Models;

namespace fugaz_retro.Controllers
{
    [Authorize]
    [PermisosFilter("Modulo Ventas")]

    public class ProductosController : Controller
    {
        private readonly FugazContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ProductosController(FugazContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            return _context.Productos != null ?
                        View(await _context.Productos.ToListAsync()) :
                        Problem("Entity set 'FugazContext.Productos'  is null.");
        }

        // GET: Producto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.DetalleProductos)
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, string tallasHiddenInput, string coloresHiddenInput, IFormFile Foto)
        {
            if (ModelState.IsValid)
            {
                if (Foto != null && Foto.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Foto.CopyToAsync(memoryStream);
                        producto.Foto = memoryStream.ToArray();
                    }
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();

                DetalleProducto ultimoDetalleProducto = null;

                if (!string.IsNullOrEmpty(tallasHiddenInput))
                {
                    var tallas = JsonConvert.DeserializeObject<List<DetalleProducto>>(tallasHiddenInput);
                    if (tallas != null)
                    {
                        foreach (var detalle in tallas)
                        {
                            detalle.IdProducto = producto.IdProducto;
                            detalle.Talla = detalle.Talla ?? string.Empty;
                            detalle.Color = detalle.Color ?? string.Empty;
                            _context.DetalleProductos.Add(detalle);
                            ultimoDetalleProducto = detalle; // Guardamos el último detalle añadido
                        }
                    }
                }

                if (!string.IsNullOrEmpty(coloresHiddenInput))
                {
                    var colores = JsonConvert.DeserializeObject<List<DetalleProducto>>(coloresHiddenInput);
                    if (colores != null)
                    {
                        foreach (var detalle in colores)
                        {
                            detalle.IdProducto = producto.IdProducto;
                            _context.DetalleProductos.Add(detalle);
                            ultimoDetalleProducto = detalle; // Guardamos el último detalle añadido
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (ultimoDetalleProducto != null)
                {
                    var redirectUrl = Url.Action("Create", "DetalleInsumos", new { idDetalleProducto = ultimoDetalleProducto.IdDetalleProducto });
                    return Json(new { redirectUrl });
                }

                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }


        // GET: Producto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.DetalleProductos)
                .FirstOrDefaultAsync(p => p.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, string tallasHiddenInput, string coloresHiddenInput, IFormFile Foto)
        {
            if (id != producto.IdProducto)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (Foto != null && Foto.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await Foto.CopyToAsync(memoryStream);
                                producto.Foto = memoryStream.ToArray();
                            }
                        }
                        else
                        {
                            var existingProduct = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.IdProducto == id);
                            if (existingProduct != null)
                            {
                                producto.Foto = existingProduct.Foto;
                            }
                        }

                        _context.Update(producto);
                        await _context.SaveChangesAsync();

                        var nuevasTallas = JsonConvert.DeserializeObject<List<DetalleProducto>>(tallasHiddenInput) ?? new List<DetalleProducto>();
                        var nuevosColores = JsonConvert.DeserializeObject<List<DetalleProducto>>(coloresHiddenInput) ?? new List<DetalleProducto>();

                        var nuevosDetalles = nuevasTallas.Concat(nuevosColores).ToList();

                        var existingDetalleProductos = _context.DetalleProductos.Where(dp => dp.IdProducto == id).ToList();

                        var detallesAEliminar = existingDetalleProductos
                            .Where(e => !nuevosDetalles.Any(d => d.Talla == e.Talla && d.Color == e.Color))
                            .ToList();
                        _context.DetalleProductos.RemoveRange(detallesAEliminar);

                        foreach (var detalle in nuevosDetalles)
                        {
                            var detalleExistente = existingDetalleProductos.FirstOrDefault(e => e.Talla == detalle.Talla && e.Color == detalle.Color);

                            if (detalleExistente == null)
                            {
                                detalle.IdProducto = producto.IdProducto;
                                detalle.Talla = detalle.Talla ?? string.Empty;
                                detalle.Color = detalle.Color ?? string.Empty;
                                _context.DetalleProductos.Add(detalle);
                            }
                            else
                            {
                                detalleExistente.Talla = detalle.Talla;
                                detalleExistente.Color = detalle.Color;
                                _context.DetalleProductos.Update(detalleExistente);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }

            return View(producto);
        }

        // GET: Producto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.DetalleProductos)
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Producto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.DetalleProductos)
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (producto != null)
                    {
                        _context.DetalleProductos.RemoveRange(producto.DetalleProductos);
                        _context.Productos.Remove(producto);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private bool ProductoExists(int id)
        {
            return (_context.Productos?.Any(e => e.IdProducto == id)).GetValueOrDefault();
        }
    }
}