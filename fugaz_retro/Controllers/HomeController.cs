using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using fugaz_retro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace fugaz_retro.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FugazContext _context;

        public HomeController(ILogger<HomeController> logger, FugazContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var userEmail = User.Identity.Name;

            // Verificar si el correo del usuario está presente y no es nulo o cadena vacía
            if (string.IsNullOrEmpty(userEmail))
            {
                // Si no hay correo de usuario, mostrar el índice sin nombre de cliente
                var viewModel = new ProductoViewModel
                {
                    Productos = _context.Productos
                        .Include(p => p.DetalleProductos)
                        .ToList()
                };

                var detalleProductos = _context.DetalleProductos
                    .Include(dp => dp.Producto)
                    .Where(dp => dp.Talla != null && dp.Color != null)
                    .ToList();

                ViewBag.DetalleProductos = detalleProductos.Select(dp => new
                {
                    dp.IdDetalleProducto,
                    Nombre = $"{dp.Producto.NombreProducto}",
                    Precio = $"{dp.Producto.PrecioVenta}",
                    Talla = $"{dp.Talla}",
                    Color = $"{dp.Color}"
                }).ToList();

                // No establecer ViewBag.NombreCliente si no hay usuario logueado
                return View(viewModel);
            }

            // Si hay correo de usuario, intentar encontrar el usuario en la base de datos
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == userEmail);
            if (usuario == null)
            {
                // Mostrar un mensaje o redireccionar según tu caso de uso
                return NotFound("Usuario no encontrado");
            }

            var viewModelWithUser = new ProductoViewModel
            {
                Productos = _context.Productos
                    .Include(p => p.DetalleProductos)
                    .ToList()
            };

            var detalleProductosWithUser = _context.DetalleProductos
                .Include(dp => dp.Producto)
                .Where(dp => dp.Talla != null && dp.Color != null)
                .ToList();

            ViewBag.DetalleProductos = detalleProductosWithUser.Select(dp => new
            {
                dp.IdDetalleProducto,
                Nombre = $"{dp.Producto.NombreProducto}",
                Precio = $"{dp.Producto.PrecioVenta}",
                Talla = $"{dp.Talla}",
                Color = $"{dp.Color}"
            }).ToList();

            ViewBag.NombreCliente = usuario.NombreUsuario;

            return View(viewModelWithUser);
        }

        public async Task<IActionResult> ObtenerDetallesProducto(int idProducto)
        {
            var detalles = await _context.DetalleProductos
                .Where(dp => dp.IdProducto == idProducto && dp.Talla != null && dp.Color != null)
                .ToListAsync();

            return Json(detalles);
        }

        public async Task<IActionResult> GetPedidoDetails(int idPedido)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

            if (pedido == null)
            {
                return NotFound();
            }

            var detalles = pedido.DetallePedidos.Select(dp => new
            {
                Cantidad = dp.Cantidad,
                Talla = dp.talla
            }).ToList();

            var pedidoViewModel = new
            {
                IdPedido = pedido.IdPedido,
                FechaPedido = pedido.FechaPedido,
                FechaEntrega = pedido.FechaEntrega,
                Estado = pedido.Estado,
                MetodoPago = pedido.MetodoPago,
                TipoTransferencia = pedido.TipoTransferencia,
                TotalPedido = pedido.TotalPedido,
                Detalles = detalles
            };

            return Json(pedidoViewModel);
        }

        public async Task<IActionResult> FiltrarPorCategoria(string categoria)
        {
            var productos = await _context.Productos
                .Where(p => p.CategoriaProducto == categoria)
                .Include(p => p.DetalleProductos)
                .ToListAsync();

            return View("Index", new ProductoViewModel { Productos = productos });
        }

        public async Task<IActionResult> Buscar(string query)
        {
            var productos = await _context.Productos
                .Where(p => p.NombreProducto.Contains(query))
                .Include(p => p.DetalleProductos)
                .ToListAsync();

            return View("Index", new ProductoViewModel { Productos = productos });
        }


        [HttpPost]
        public async Task<IActionResult> CrearPedido(Pedido pedido, string detallesPedidoJson, IFormFile? comprobantePago)
        {
            if (ModelState.IsValid)
            {
                var userEmail = User.Identity.Name;

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == userEmail);
                if (usuario == null)
                {
                    ModelState.AddModelError("", "Usuario no encontrado.");
                    return Json(new { success = false, message = "Usuario no encontrado." });
                }

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdUsuario == usuario.IdUsuario);
                if (cliente == null)
                {
                    ModelState.AddModelError("", "Cliente no encontrado.");
                    return Json(new { success = false, message = "Cliente no encontrado." });
                }

                // Asignar el ID del cliente al pedido
                pedido.IdCliente = cliente.IdCliente;

                if (comprobantePago != null && comprobantePago.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await comprobantePago.CopyToAsync(memoryStream);
                        pedido.ComprobantePago = memoryStream.ToArray();
                    }
                }

                pedido.FechaPedido = DateTime.Now;
                pedido.FechaEntrega = DateTime.Now.AddDays(10);
                pedido.Estado = "En proceso";

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Add(pedido);
                        await _context.SaveChangesAsync();

                        var detallesPedido = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(detallesPedidoJson);

                        foreach (var detalle in detallesPedido)
                        {
                            var producto = detalle["Producto"];
                            var color = detalle["Color"];
                            var talla = detalle["Talla"];
                            var cantidad = int.Parse(detalle["Cantidad"]);

                            var detalleProducto = await _context.DetalleProductos
                                .Include(dp => dp.Producto)
                                .FirstOrDefaultAsync(dp => dp.Producto.NombreProducto == producto);

                            if (detalleProducto != null)
                            {
                                var subtotal = detalleProducto.Producto.PrecioVenta * cantidad;

                                var detallePedidoNuevo = new DetallePedido
                                {
                                    IdPedido = pedido.IdPedido,
                                    IdDetalleProducto = detalleProducto.Producto.IdProducto,
                                    Cantidad = cantidad,
                                    Subtotal = subtotal,
                                    talla = talla,
                                    color = color
                                };

                                _context.DetallePedidos.Add(detallePedidoNuevo);
                            }
                            else
                            {
                                ModelState.AddModelError("", $"No se encontró el detalle del producto {producto} - {color} - {talla}.");
                                return Json(new { success = false, message = $"No se encontró el detalle del producto {producto} - {color} - {talla}." });
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ModelState.AddModelError("", $"Error al crear el pedido. Detalles del error: {innerExceptionMessage}");
                        return Json(new { success = false, message = innerExceptionMessage });
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "ModelState no es válido.");
                return Json(new { success = false, message = "ModelState no es válido." });
            }
        }

        //DETALLES DE PEDIDOO
        [HttpGet]
        public async Task<IActionResult> DetallesPedido(int pedidoId)
        {
            var detalles = await _context.DetallePedidos
                //.Include(dp => dp.IdDetalleProductoNavigation) // Navegar desde DetallePedido a DetalleProducto
                //.ThenInclude(dp => dp.Producto)
                .Where(dp => dp.IdPedido == pedidoId)
                .Select(dp => new
                {
                    dp.Cantidad,
                    dp.talla,
                    dp.color,
                    dp.Subtotal,
                    dp.IdPedidoNavigation.ComprobantePago
                })
                .ToListAsync();

            if (detalles == null || !detalles.Any())
            {
                return NotFound();
            }

            return Json(detalles);
        }

        //MIS PEDIDOS

        public async Task<IActionResult> Mispedidos(string estado)
        {
            // Verificar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirigir al usuario a la página de inicio de sesión
            }

            // Obtener el cliente asociado al usuario actual
            var usuarioCorreo = User.Identity.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                return RedirectToAction("Error", "Home"); // Redirigir a una página de error
            }

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdUsuario == usuario.IdUsuario);

            if (cliente == null)
            {
                return RedirectToAction("Error", "Home"); // Redirigir a una página de error
            }

            // Filtrar los detalles de los pedidos según el estado (si se especifica)
            IQueryable<DetallePedido> detallesQuery = _context.DetallePedidos
                .Include(dp => dp.IdPedidoNavigation)
                .Where(dp => dp.IdPedidoNavigation.IdCliente == cliente.IdCliente);

            // Filtrar solo pedidos en proceso si el estado no es "Todos"
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                detallesQuery = detallesQuery.Where(dp => dp.IdPedidoNavigation.Estado == estado);
            }
            else
            {
                detallesQuery = detallesQuery.Where(dp => dp.IdPedidoNavigation.Estado == "En proceso");
            }

            // Cargar los detalles de los pedidos
            var detalles = await detallesQuery.ToListAsync();

            // Pasar el estado seleccionado a la vista
            ViewBag.Estado = estado;

            return View(detalles);
        }

        // Acción para mostrar el historial de pedidos
        public IActionResult HistorialPedidos(string estado)
        {
            // Verificar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirigir al usuario a la página de inicio de sesión
            }

            // Obtener el cliente asociado al usuario actual
            var usuarioCorreo = User.Identity.Name;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                return RedirectToAction("Error", "Home"); // Redirigir a una página de error
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.IdUsuario == usuario.IdUsuario);

            if (cliente == null)
            {
                return RedirectToAction("Error", "Home"); // Redirigir a una página de error
            }

            // Obtener tipos únicos de estados de pedidos para el cliente
            var estados = _context.DetallePedidos
                .Include(dp => dp.IdPedidoNavigation)
                .Where(dp => dp.IdPedidoNavigation.IdCliente == cliente.IdCliente)
                .Select(dp => dp.IdPedidoNavigation.Estado)
                .Distinct()
                .ToList();

            ViewBag.Estados = estados;

            // Filtrar pedidos que están en estado "Enviado" o "Entregado" o "Anulado"
            var historialPedidos = _context.DetallePedidos
                .Include(dp => dp.IdPedidoNavigation)
                .Where(dp => dp.IdPedidoNavigation.IdCliente == cliente.IdCliente
                              && (dp.IdPedidoNavigation.Estado == "Enviado" || dp.IdPedidoNavigation.Estado == "Entregado" || dp.IdPedidoNavigation.Estado == "Anulado"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                historialPedidos = historialPedidos.Where(dp => dp.IdPedidoNavigation.Estado == estado);
            }

            return View(historialPedidos.ToList());
        }



        [HttpPost]
        public async Task<IActionResult> AnularPedido(int idPedido)
        {
            // Verificar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirigir al usuario a la página de inicio de sesión
            }

            // Obtener el pedido a anular
            var pedido = await _context.Pedidos.FindAsync(idPedido);

            if (pedido == null)
            {
                return NotFound(); // Devolver un error si no se encuentra el pedido
            }

            // Verificar si el pedido puede ser anulado (si está en proceso y dentro del límite de tiempo)
            var diasDesdePedido = (DateTime.Now - pedido.FechaPedido).Days;
            if (pedido.Estado != "En proceso" || diasDesdePedido > 2)
            {
                return BadRequest("No se puede anular el pedido."); // Devolver un error si el pedido no puede ser anulado
            }

            pedido.FechaEntrega = DateTime.Now;
            // Cambiar el estado del pedido a "Anulado"
            pedido.Estado = "Anulado";

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            return Json(new { success = true }); // Devolver una respuesta de éxito
        }

        //NOSOTROS
        public ActionResult Nosotros()
        {
            return View();
        }
    }
}
