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
    public class ProveedorsController : Controller
    {
        private readonly FugazContext _context;

        public ProveedorsController(FugazContext context)
        {
            _context = context;
        }

        // GET: Proveedors
        public async Task<IActionResult> Index()
        {
            return _context.Proveedors != null ?
                        View(await _context.Proveedors.ToListAsync()) :
                        Problem("Entity set 'FugazContext.Proveedors'  is null.");
        }

        // GET: Proveedors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Proveedors == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedors
                .FirstOrDefaultAsync(m => m.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // GET: Proveedors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProveedor,TipoProveedor,Empresa,RepresentanteLegal,Rut,NombreCompleto,Documento,Telefono,Direccion,Estado")] Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                if (proveedor.TipoProveedor == "Natural")
                {
                    var documentoExistente = await _context.Proveedors.AnyAsync(p => p.Documento == proveedor.Documento);
                    if (documentoExistente)
                    {
                        ModelState.AddModelError("Documento", "El documento del proveedor ya está registrado.");
                        return View(proveedor);
                    }
                }
                else if (proveedor.TipoProveedor == "Juridico")
                {
                    var rutExistente = await _context.Proveedors.AnyAsync(p => p.Rut == proveedor.Rut);
                    if (rutExistente)
                    {
                        ModelState.AddModelError("Rut", "El Rut del proveedor ya está registrado.");
                        return View(proveedor);
                    }
                }

                _context.Proveedors.Add(proveedor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(proveedor);
        }

        // POST: Proveedors/ToggleEstado/5
        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            proveedor.Estado = !proveedor.Estado;
            _context.Update(proveedor);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: Proveedors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Proveedors == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return View(proveedor);
        }

        // POST: Proveedors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProveedor,TipoProveedor,Empresa,RepresentanteLegal,Rut,NombreCompleto,Documento,Telefono,Direccion,Estado")] Proveedor proveedor)
        {
            if (id != proveedor.IdProveedor)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.IdProveedor))
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
            return View(proveedor);
        }

        // GET: Proveedors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Proveedors == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedors
                .FirstOrDefaultAsync(m => m.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Proveedors == null)
            {
                return Problem("Entity set 'FugazContext.Proveedors'  is null.");
            }

            var proveedor = await _context.Proveedors.Include(p => p.Compras).FirstOrDefaultAsync(p => p.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound();
            }

            if (proveedor.Compras.Any())
            {
                return BadRequest("El proveedor no se puede eliminar ya que se encuentra asociado a una compra.");
            }

            _context.Proveedors.Remove(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Proveedors/GetProveedorDetails/5
        public async Task<IActionResult> GetProveedorDetails(int? id)
        {
            if (id == null || _context.Proveedors == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedors.FirstOrDefaultAsync(m => m.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return Json(proveedor);
        }

        // POST: Proveedors/DeleteProveedor/5
        [HttpPost]
        public async Task<IActionResult> DeleteProveedor(int id)
        {
            if (_context.Proveedors == null)
            {
                return Problem("Entity set 'FugazContext.Proveedors' is null.");
            }

            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            if (_context.Compras.Any(c => c.IdProveedor == id))
            {
                return BadRequest("El proveedor no puede ser eliminado porque tiene una o varias compras asociadas.");
            }

            _context.Proveedors.Remove(proveedor);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool ProveedorExists(int id)
        {
            return (_context.Proveedors?.Any(e => e.IdProveedor == id)).GetValueOrDefault();
        }
    }
}
