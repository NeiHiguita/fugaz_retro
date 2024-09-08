﻿using System.Diagnostics;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using fugaz_retro.Services;
using System.Text;
using BCrypt.Net;


namespace fugaz_retro.Controllers
{
    public class HomeController : Controller
    {
        private readonly FugazContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailService _emailService;

        public HomeController(
            ILogger<HomeController> logger,
            FugazContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            var userEmail = User.Identity.Name;
            List<Producto> nuevasTendencias, masVendidos, restoProductos;
            // Cargar las ciudades
            var ciudades = _context.CostoEnvios.ToList();

            if (string.IsNullOrEmpty(userEmail))
            {
                var productos = _context.Productos
                    .Include(p => p.DetalleProductos)
                    .ToList();

                nuevasTendencias = productos.Take(3).ToList();
                masVendidos = productos.Skip(3).Take(3).ToList();
                restoProductos = productos.Skip(6).ToList();

                var viewModel = new ProductoViewModel
                {
                    Productos = productos,
                    NuevasTendencias = nuevasTendencias,
                    MasVendidos = masVendidos,
                    RestoProductos = restoProductos,
                                Ciudades = ciudades // Pasar las ciudades
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

                return View(viewModel);
            }

            var usuario = _context.Usuarios
                .Include(u => u.Cliente)
                .FirstOrDefault(u => u.Correo == userEmail);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var productosConUsuario = _context.Productos
                .Include(p => p.DetalleProductos)
                .ToList();

            nuevasTendencias = productosConUsuario.Take(3).ToList();
            masVendidos = productosConUsuario.Skip(3).Take(3).ToList();
            restoProductos = productosConUsuario.Skip(6).ToList();

            var viewModelWithUser = new ProductoViewModel
            {
                Productos = productosConUsuario,
                NuevasTendencias = nuevasTendencias,
                MasVendidos = masVendidos,
                RestoProductos = restoProductos,
                Ciudades = ciudades // Pasar las ciudades
            };

            var detalleProductosWithUser = _context.DetalleProductos
                .Include(dp => dp.Producto)
                .Where(dp => dp.Talla == null && dp.Color == null)
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
            ViewBag.TelefonoCliente = usuario.Cliente?.Telefono;

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

            var viewModel = new ProductoViewModel
            {
                Productos = productos,
                NuevasTendencias = productos.Take(3).ToList(),
                MasVendidos = productos.Skip(3).Take(3).ToList(),
                RestoProductos = productos.Skip(6).ToList()
            };

            return View("Index", viewModel);
        }

        public async Task<IActionResult> Buscar(string query)
        {
            query = query.ToLower();

            var productos = await _context.Productos
                .Include(p => p.DetalleProductos)
                .Where(p => p.NombreProducto.ToLower().Contains(query) ||
                            p.DetalleProductos.Any(dp => dp.Color.ToLower().Contains(query)))
                .ToListAsync();

            var viewModel = new ProductoViewModel
            {
                Productos = productos,
                NuevasTendencias = productos.Take(3).ToList(),
                MasVendidos = productos.Skip(3).Take(3).ToList(),
                RestoProductos = productos.Skip(6).ToList()
            };

            return View("Index", viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetCostoEnvio(string ciudad)
        {
            if (string.IsNullOrEmpty(ciudad))
            {
                return Json(new { success = false, message = "Ciudad no válida." });
            }

            var costoEnvio = await _context.CostoEnvios
                .Where(ce => ce.Ciudad == ciudad)
                .Select(ce => ce.Costo)
                .FirstOrDefaultAsync();

            if (costoEnvio == 0)
            {
                return Json(new { success = false, message = "Costo de envío no encontrado." });
            }

            return Json(new { success = true, costoEnvio });
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
                var ciudades = await _context.CostoEnvios
                    .Select(ce => new { ce.Ciudad, ce.Costo })
                    .Distinct()
                    .ToListAsync();

                ViewBag.Ciudades = new SelectList(ciudades, "Ciudad", "Ciudad");

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
            try
            {
                var detalles = await _context.DetallePedidos
                    .Where(dp => dp.IdPedido == pedidoId)
                    .Select(dp => new
                    {
                        dp.Cantidad,
                        dp.talla,
                        dp.color,
                        dp.Subtotal,
                        ComprobantePago = dp.IdPedidoNavigation.ComprobantePago,
                        dp.IdDetalleProducto // Obtén solo el ID del producto aquí
                    })
                    .ToListAsync();

                if (detalles == null || !detalles.Any())
                {
                    return NotFound();
                }

                var resultado = new List<object>();

                foreach (var detalle in detalles)
                {
                    var producto = await _context.Productos
                        .Where(p => p.IdProducto == detalle.IdDetalleProducto)
                        .Select(p => new
                        {
                            p.NombreProducto,
                            p.PrecioVenta,
                            p.Foto 
                        })
                        .FirstOrDefaultAsync();

                    string fotoBase64 = producto?.Foto != null ? Convert.ToBase64String(producto.Foto) : null;
                    string comprobanteBase64 = detalle.ComprobantePago != null ? Convert.ToBase64String(detalle.ComprobantePago) : null;

                    resultado.Add(new
                    {
                        cantidad = detalle.Cantidad,
                        talla = detalle.talla,
                        color = detalle.color,
                        subtotal = detalle.Subtotal,
                        nombreProducto = producto?.NombreProducto,
                        precio=producto?.PrecioVenta,
                        fotoProducto = fotoBase64, 
                        comprobantePago = comprobanteBase64 
                    });
                }

                return Json(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en DetallesPedido: " + ex.Message);
                return StatusCode(500, "Ocurrió un error al procesar la solicitud.");
            }
        }

        //MIS PEDIDOS

        public async Task<IActionResult> Mispedidos(string estado)
        {
            // Verificar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirigir al usuario a la página de inicio de sesión
            }

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

            var detalles = await detallesQuery.ToListAsync();

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

            return Json(new { success = true });
        }

        //NOSOTROS
        public ActionResult Nosotros()
        {
            return View();
        }

        //Miperfil ------------------------------------------------------
        [HttpGet]
        [Route("Home/Miperfil")]
        public async Task<IActionResult> Miperfil()
        {
            var userEmail = User.Identity.Name;
            var usuario = await _context.Usuarios.Include(u => u.Cliente).FirstOrDefaultAsync(u => u.Correo == userEmail);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var aspNetUser = await _userManager.FindByEmailAsync(userEmail);
            if (aspNetUser == null)
            {
                return NotFound("Usuario de identidad no encontrado");
            }

            ViewBag.IdUsuario = usuario.IdUsuario;
            ViewBag.NombreUsuario = usuario.NombreUsuario;
            ViewBag.Correo = usuario.Correo;
            ViewBag.Document = usuario.Document;
            ViewBag.Telefono = usuario.Cliente?.Telefono;
            ViewBag.AspNetUserId = aspNetUser.Id;

            return View();
        }
        //----------------------------------------------------------------

        [HttpPost]
        [Route("Home/Miperfil")]
        public async Task<IActionResult> Miperfil(int idUsuario, string nombreUsuario, string telefono)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios.Include(u => u.Cliente).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Actualizar Usuario
                usuario.NombreUsuario = nombreUsuario;

                if (usuario.Cliente != null)
                {
                    usuario.Cliente.Telefono = telefono;
                }
                else
                {
                    usuario.Cliente = new Cliente
                    {
                        IdUsuario = idUsuario,
                        Telefono = telefono
                    };
                }

                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                ViewBag.IdUsuario = usuario.IdUsuario;
                ViewBag.NombreUsuario = usuario.NombreUsuario;
                ViewBag.Correo = usuario.Correo;
                ViewBag.Document = usuario.Document;
                ViewBag.Telefono = usuario.Cliente.Telefono;

                ViewBag.Message = "Perfil actualizado con éxito";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            if (newPassword != confirmNewPassword)
            {
                TempData["ErrorMessage"] = "Las contraseñas no coinciden";
                return RedirectToAction("Miperfil");
            }

            var userEmail = User.Identity.Name;
            var aspNetUser = await _userManager.FindByEmailAsync(userEmail);

            if (aspNetUser == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado";
                return RedirectToAction("Miperfil");
            }

            var result = await _userManager.ChangePasswordAsync(aspNetUser, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Contraseña actualizada con éxito";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al cambiar la contraseña";
            }

            return RedirectToAction("Miperfil");
        }

        // RECUPERAR CONTRASEÑA


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generar un código de recuperación aleatorio
            var recoveryCode = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < 6; i++)
            {
                recoveryCode.Append(random.Next(0, 10));
            }

            // Buscar el usuario por correo electrónico
            var user = _context.Usuarios.FirstOrDefault(u => u.Correo == model.Email); // Utiliza tu tabla de usuarios
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Correo electrónico no encontrado.");
                return View(model);
            }

            // Guardar el código de recuperación en la base de datos
            user.RecoveryCode = recoveryCode.ToString();
            await _context.SaveChangesAsync();

            // Enviar el correo electrónico con el código de recuperación
            var emailDto = new EmailDTO
            {
                Para = model.Email,
                Asunto = "Código de recuperación de contraseña",
                Contenido = $"Tu código de recuperación es: {recoveryCode}"
            };
            _emailService.SendEmail(emailDto);

            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar el código de recuperación
            var user = _context.Usuarios.FirstOrDefault(u => u.RecoveryCode == model.RecoveryCode && u.Correo == model.Email); // Utiliza tu tabla de usuarios
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Código de recuperación inválido.");
                return View(model);
            }

            // Actualizar la contraseña del usuario
            user.Contraseña = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); // Suponiendo que estás usando BCrypt para el hashing de contraseñas
            user.RecoveryCode = null; // Eliminar el código de recuperación después de su uso
            await _context.SaveChangesAsync();

            // Actualizar la contraseña en la tabla aspnetusers
            var aspnetUser = await _userManager.FindByNameAsync(model.Email);
            if (aspnetUser != null)
            {
                await _userManager.RemovePasswordAsync(aspnetUser);
                await _userManager.AddPasswordAsync(aspnetUser, model.NewPassword);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult LoadMoreProducts(int skip, int take)
        {
            var productos = _context.Productos
                .Skip(skip)
                .Take(take)
                .Select(p => new {
                    p.IdProducto,
                    p.NombreProducto,
                    p.PrecioVenta,
                    Foto = Convert.ToBase64String(p.Foto) // Asumiendo que Foto es un byte[]
                })
                .ToList();

            // Verificar si hay más productos para cargar
            if (productos.Count == 0)
            {
                return Json(new { success = false, message = "No hay más productos." });
            }

            return Json(productos);
        }

    }
}