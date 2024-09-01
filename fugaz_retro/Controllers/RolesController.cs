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
    [PermisosFilter("Modulo Configuracion")]
    public class RolesController : Controller
    {
        private readonly FugazContext _context;

        public RolesController(FugazContext context)
        {
            _context = context;
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.RolPermisos)
                    .ThenInclude(rp => rp.IdPermisoNavigation)
                .ToListAsync();

            return View(roles);
        }


        // GET: Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Roles == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(m => m.IdRol == id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Permisos = _context.Permisos.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role, List<int> SelectedPermisos)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    role.Estado = true; // Estado inicial del rol como activo
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();

                    // Asignar permisos seleccionados al rol
                    foreach (var permisoId in SelectedPermisos)
                    {
                        var rolPermiso = new RolPermiso
                        {
                            IdRol = role.IdRol,
                            IdPermiso = permisoId
                        };
                        _context.RolPermisos.Add(rolPermiso);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log del error
                    Console.WriteLine($"Error: {ex.Message}");
                    ModelState.AddModelError("", $"Error al crear el rol: {ex.Message}");
                }
            }

            // Si algo falla, se vuelve a cargar la lista de permisos
            ViewBag.Permisos = await _context.Permisos.ToListAsync();
            return View(role);
        }
        [HttpPost]
        public async Task<IActionResult> ToggleRoleStatus(int idRol, bool estado)
        {
            var rol = await _context.Roles.FindAsync(idRol);
            if (rol == null)
            {
                return NotFound();
            }

            rol.Estado = estado;

            await _context.SaveChangesAsync();

            return Ok();
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .Include(r => r.RolPermisos)
                .FirstOrDefaultAsync(m => m.IdRol == id);
            if (role == null)
            {
                return NotFound();
            }

            ViewBag.Permisos = _context.Permisos.ToList();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role, List<int> SelectedPermisos)
        {
            if (id != role.IdRol)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(role);
                    await _context.SaveChangesAsync();

                    // Actualizar los permisos del rol
                    var existingPermisos = _context.RolPermisos.Where(rp => rp.IdRol == role.IdRol).ToList();
                    _context.RolPermisos.RemoveRange(existingPermisos);

                    foreach (var permisoId in SelectedPermisos)
                    {
                        var rolPermiso = new RolPermiso
                        {
                            IdRol = role.IdRol,
                            IdPermiso = permisoId
                        };
                        _context.RolPermisos.Add(rolPermiso);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el rol: {ex.Message}");
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Permisos = _context.Permisos.ToList();
            return View(role);
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.Any(e => e.IdRol == id);
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Roles == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(m => m.IdRol == id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Roles == null)
            {
                return Problem("Entity set 'FugazContext.Roles'  is null.");
            }
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Roles/GetRoleDetails/5
        public async Task<IActionResult> GetRoleDetails(int? id)
        {
            if (id == null || _context.Roles == null)
            {
                return NotFound();
            }

            var role = await _context.Roles.FirstOrDefaultAsync(m => m.IdRol == id);
            if (role == null)
            {
                return NotFound();
            }

            return Json(role);
        }

        // POST: Roles/DeleteRole/5
        [HttpPost]
        public async Task<IActionResult> DeleteRole(int id)
        {
            if (_context.Roles == null)
            {
                return Problem("Entity set 'FugazContext.Roles' is null.");
            }

            var role = await _context.Roles.Include(r => r.RolPermisos).FirstOrDefaultAsync(r => r.IdRol == id);
            if (role == null)
            {
                return NotFound();
            }

            // Eliminar permisos asociados al rol
            var permisosAsociados = _context.RolPermisos.Where(rp => rp.IdRol == role.IdRol);
            _context.RolPermisos.RemoveRange(permisosAsociados);

            try
            {
                // Intentar eliminar el rol
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                // Verificar si el error está relacionado con la clave externa (asociado a usuarios)
                if (ex.InnerException != null && ex.InnerException.Message.Contains("foreign key"))
                {
                    return StatusCode(409, "No se puede eliminar el rol porque está asociado a un usuario.");
                }
                else
                {
                    // Si es otro error, mostrar el mensaje genérico
                    return StatusCode(500, "Ocurrió un error al intentar eliminar el rol.");
                }
            }
        }

    }
}
