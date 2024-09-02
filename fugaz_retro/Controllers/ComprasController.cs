using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using fugaz_retro.Models;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace fugaz_retro.Controllers
{
    [Authorize]
    [PermisosFilter("Modulo Compras")]
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
            var compras = _context.Compras
                .Include(c => c.DetalleCompras) // Incluir los detalles de la compra
                    .ThenInclude(dc => dc.IdInsumoNavigation) // Incluir la relación con Insumo para obtener los nombres
                .Include(c => c.IdProveedorNavigation) // Incluir la relación con el proveedor si es necesario
                .ToList();

            return View(compras);
        }


        // GET: Compras/Details/5
        public IActionResult Details(int id)
        {
            var compra = _context.Compras
                .Include(c => c.DetalleCompras)
                .ThenInclude(dc => dc.IdInsumoNavigation)
                .Include(c => c.IdProveedorNavigation) // Incluir la información del proveedor
                .FirstOrDefault(c => c.IdCompra == id);

            if (compra == null)
            {
                return NotFound();
            }

            var nombreProveedor = compra.IdProveedorNavigation.TipoProveedor == "Natural"
                ? compra.IdProveedorNavigation.NombreCompleto
                : compra.IdProveedorNavigation.Empresa;

            var detalles = compra.DetalleCompras.Select(dc => new
            {
                nombreInsumo = dc.IdInsumoNavigation.NombreInsumo,
                cantidad = dc.Cantidad,
                precioUnitario = dc.PrecioUnitario,
                total = dc.Cantidad * dc.PrecioUnitario
            }).ToList();

            var result = new
            {
                fechaCompra = compra.FechaCompra.ToString("dd-MM-yyyy"),
                tipoProveedor = compra.IdProveedorNavigation.TipoProveedor,
                nombreProveedor = nombreProveedor,
                detalles = detalles
            };

            return Json(result);
        }



        // GET: Compras/Create
        public IActionResult Create()
        {
            ViewBag.Proveedores = _context.Proveedors.ToList();
            ViewBag.Insumos = _context.Insumos.ToList();

            var model = new Compra
            {
                DetalleCompras = new List<DetalleCompra>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Compra compra)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Compras.Add(compra);
                    await _context.SaveChangesAsync();


                    foreach (var detalle in compra.DetalleCompras)
                    {
                        if (double.TryParse(detalle.Cantidad.ToString(), out double cantidad))
                        {
                            detalle.Cantidad = cantidad;
                            var originalInsumo = await _context.Insumos.FindAsync(detalle.IdInsumo);
                            if (originalInsumo != null)
                            {
                                originalInsumo.Stock += detalle.Cantidad;
                                originalInsumo.PrecioUnitario = detalle.PrecioUnitario;

                                _context.Update(originalInsumo);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "El valor ingresado para la cantidad no es válido.");
                            ViewBag.Proveedores = _context.Proveedors.ToList();
                            ViewBag.Insumos = _context.Insumos.ToList();
                            return View(compra);
                        }

                        detalle.IdCompra = compra.IdCompra;
                        _context.DetalleCompras.Add(detalle);
                    }
                    await _context.SaveChangesAsync(); //Detalles 
                    return RedirectToAction(nameof(Index));


                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al crear la compra: " + ex.Message);
                }
            }

            ViewBag.Proveedores = _context.Proveedors.ToList();
            ViewBag.Insumos = _context.Insumos.ToList();
            return View(compra);
        }
        private bool CompraExists(int id)
        {
            return (_context.Compras?.Any(e => e.IdCompra == id)).GetValueOrDefault();
        }
    }
}
