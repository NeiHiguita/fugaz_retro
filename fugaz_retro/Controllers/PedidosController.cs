using fugaz_retro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Rotativa.AspNetCore;

namespace fugaz_retro.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly FugazContext _context;

        private readonly IWebHostEnvironment _hostingEnvironment;

        public PedidosController(FugazContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index()
        {
            var fugazContext = _context.Pedidos.Include(p => p.IdClienteNavigation);
            // Filtrar los pedidos por estado diferente a "Entregado"
            var pedidos = await fugazContext.Where(p => p.Estado != "Entregado").ToListAsync();
            return View(pedidos);
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Pedidos == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.IdClienteNavigation)
                .ThenInclude(c => c.Usuarios)
                .FirstOrDefaultAsync(m => m.IdPedido == id);

            if (pedido == null)
            {
                return NotFound();
            }

            var imageData = pedido.ComprobantePago != null ? Convert.ToBase64String(pedido.ComprobantePago) : null;
            var imageSrc = imageData != null ? string.Format("data:image/png;base64,{0}", imageData) : null;

            ViewBag.ComprobantePago = imageSrc;
            ViewBag.ClienteNombre = pedido.IdClienteNavigation?.Usuarios?.NombreUsuario;

            return View(pedido);
        }
        public IActionResult GetProductNames()
        {
            var productNames = _context.Productos
                .Select(p => new { p.IdProducto, p.NombreProducto })
                .ToList();
            return Json(productNames);
        }

        public IActionResult GetProductDetails(int idProducto)
        {
            var productDetails = _context.Productos
                .Where(p => p.IdProducto == idProducto)
                .Select(p => new
                {
                    p.PrecioVenta,
                    Detalles = p.DetalleProductos.Select(dp => new { dp.Talla, dp.Color })
                })
                .FirstOrDefault();

            if (productDetails == null)
            {
                return Json(new { success = false });
            }

            return Json(new { success = true, details = productDetails });
        }

        [HttpGet]
        public async Task<JsonResult> GetInsumoDetails(int idProducto)
        {
            var producto = await _context.Productos
                .Include(p => p.DetalleProductos)
                    .ThenInclude(dp => dp.DetalleInsumos)
                        .ThenInclude(di => di.Insumo)
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            var detallesInsumos = producto.DetalleProductos
                .SelectMany(dp => dp.DetalleInsumos.Select(di => new
                {
                    IdDetalleInsumo = di.IdDetalleInsumo,
                    NombreInsumo = $"{di.Insumo.NombreInsumo} - {di.Cantidad}"
                }));

            return Json(new { success = true, detallesInsumos });
        }



        // GET: Pedidos/Create
        public async Task<IActionResult> Create()
        {
            var detalleProductos = await _context.DetalleProductos
          .Include(dp => dp.Producto)
          .Where(dp => dp.Talla != null && dp.Color != null) // Filtrar los productos con valores no nulos
          .ToListAsync();

            ViewData["IdCliente"] = new SelectList(_context.Clientes.Select(p => new { p.IdCliente, Nombre = $"{p.Usuarios.NombreUsuario}" }), "IdCliente", "Nombre");
            ViewData["DetalleProductosNombre"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Nombre = $"{dp.Producto.NombreProducto}" }), "IdDetalleProducto", "Nombre");
            ViewData["DetalleProductosPrecio"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Precio = $"{dp.Producto.PrecioVenta}" }), "IdDetalleProducto", "Precio");
            ViewData["DetalleProductosTalla"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Talla = $"{dp.Talla}" }), "IdDetalleProducto", "Talla");
            ViewData["DetalleProductosColor"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Color = $"{dp.Color}" }), "IdDetalleProducto", "Color");
            ViewData["DetalleInsumos"] = new SelectList(_context.DetalleInsumos.Select(p => new { p.IdDetalleInsumo, NombreCantidad = $"{p.Insumo.NombreInsumo} - {p.Cantidad}" }), "IdDetalleInsumo", "NombreCantidad");

            ViewBag.Ciudades = new List<string>
            {
                "Barranquilla", "Bogota", "Cali", "Cartagena" ,"Medellin"
            };
            ViewBag.Ciudades.Sort(); 

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido, List<DetallePedido> detallesPedido, IFormFile? comprobantePago)
        {
            if (ModelState.IsValid)
            {
                if (comprobantePago != null && comprobantePago.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await comprobantePago.CopyToAsync(memoryStream);
                        pedido.ComprobantePago = memoryStream.ToArray();
                    }
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        pedido.Estado = "En proceso";
                        _context.Add(pedido);
                        await _context.SaveChangesAsync();

                        foreach (var detalle in detallesPedido)
                        {
                            detalle.IdPedido = pedido.IdPedido;
                            _context.DetallePedidos.Add(detalle);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ModelState.AddModelError("", $"Error al crear el pedido. Detalles del error: {innerExceptionMessage}");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "ModelState no es válido.");
            }

            // Re-load view data if the model state is invalid
            var detalleProductos = await _context.DetalleProductos
                .Include(dp => dp.Producto)
                .ToListAsync();
            ViewData["IdCliente"] = new SelectList(_context.Clientes.Select(p => new { p.IdCliente, Nombre = $"{p.Usuarios.NombreUsuario}" }), "IdCliente", "Nombre");
            ViewData["DetalleProductosNombre"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Nombre = $"{dp.Producto.NombreProducto}" }), "IdDetalleProducto", "Nombre");
            ViewData["DetalleProductosPrecio"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Precio = $"{dp.Producto.PrecioVenta}" }), "IdDetalleProducto", "Precio");
            ViewData["DetalleProductosTalla"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Talla = $"{dp.Talla}" }), "IdDetalleProducto", "Talla");
            ViewData["DetalleProductosColor"] = new SelectList(detalleProductos.Select(dp => new { dp.IdDetalleProducto, Color = $"{dp.Color}" }), "IdDetalleProducto", "Color");
            ViewData["DetalleInsumos"] = new SelectList(_context.DetalleInsumos.Select(p => new { p.IdDetalleInsumo, NombreCantidad = $"{p.Insumo.NombreInsumo} - {p.Cantidad}" }), "IdDetalleInsumo", "NombreCantidad");



            return View(pedido);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            pedido.Estado = "Anulado"; // O cualquier otro estado que defina la anulación
            _context.Update(pedido);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                                       .Include(p => p.IdClienteNavigation)
                                       .ThenInclude(c => c.Usuarios)
                                       .Include(p => p.DetallePedidos)
                                       .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null)
            {
                return NotFound();
            }

            ViewBag.ClienteNombre = pedido.IdClienteNavigation?.Usuarios?.NombreUsuario;

            ViewBag.Ciudades = new List<string>
            {
                "Barranquilla", "Bogota", "Cali", "Cartagena","Medellin"
            };
            ViewBag.Ciudades.Sort();

            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pedido pedido, List<DetallePedido> detallesPedido, IFormFile? comprobantePago)
        {
            if (id != pedido.IdPedido)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (comprobantePago != null && comprobantePago.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await comprobantePago.CopyToAsync(memoryStream);
                                pedido.ComprobantePago = memoryStream.ToArray();
                            }
                        }

                        // Asegúrate de que el cliente esté incluido al actualizar el pedido
                        _context.Entry(pedido).Reference(p => p.IdClienteNavigation).IsModified = false;

                        _context.Update(pedido);
                        await _context.SaveChangesAsync();

                        foreach (var detalle in detallesPedido)
                        {
                            detalle.IdPedido = pedido.IdPedido;
                            _context.DetallePedidos.Update(detalle);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Verificar si el estado del pedido es "Entregado"
                        if (pedido.Estado == "Entregado")
                        {
                            // Crear una nueva venta
                            var venta = new Venta
                            {
                                IdPedido = pedido.IdPedido,
                                // Establecer otros campos de la venta aquí según tu modelo
                            };

                            _context.Ventas.Add(venta);
                            await _context.SaveChangesAsync();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        await transaction.RollbackAsync();
                        if (!PedidoExists(pedido.IdPedido))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            ViewData["IdCliente"] = new SelectList(_context.Clientes.Include(c => c.Usuarios).ToList(), "IdCliente", "Usuarios.NombreUsuario", pedido.IdCliente);
            return View(pedido);
        }



        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Pedidos == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.IdClienteNavigation)
                .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Pedidos == null)
            {
                return Problem("Entity set 'FugazContext.Pedidos'  is null.");
            }
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return (_context.Pedidos?.Any(e => e.IdPedido == id)).GetValueOrDefault();
        }
    }
}
