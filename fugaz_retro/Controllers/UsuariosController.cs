using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using fugaz_retro.Models;
using Microsoft.AspNetCore.Authorization;

namespace fugaz_retro.Controllers
{
    [Authorize]
    [PermisosFilter("Modulo Usuarios")]
    public class UsuariosController : Controller
    {
        private readonly FugazContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public UsuariosController(FugazContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var fugazContext = _context.Usuarios
                                       .Include(u => u.IdRolNavigation)
                                       .ThenInclude(rp => rp.RolPermisos)
                                       .ThenInclude(rp => rp.IdPermisoNavigation);

            var roles = await _context.Roles.ToListAsync();
            ViewData["Roles"] = roles;

            return View(await fugazContext.ToListAsync());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                                        .Include(u => u.Cliente)
                                        .Include(u => u.IdRolNavigation)
                                        .FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
            ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol");
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, string Telefono)
        {
            if (ModelState.IsValid)
            {
                var existingUserWithEmail = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario.Correo);
                if (existingUserWithEmail != null)
                {
                    ModelState.AddModelError(string.Empty, "El correo electrónico ya está registrado.");
                    var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
                    ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
                    return View(usuario);
                }

                var existingUserWithDocument = await _context.Usuarios.FirstOrDefaultAsync(u => u.Document == usuario.Document);
                if (existingUserWithDocument != null)
                {
                    ModelState.AddModelError(string.Empty, "El documento ya está registrado.");
                    var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
                    ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
                    return View(usuario);
                }

                usuario.DateRegister = DateTime.Now;

                var identityUser = new IdentityUser { UserName = usuario.Correo, Email = usuario.Correo, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(identityUser, usuario.Contraseña);

                if (result.Succeeded)
                {
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();

                    if (usuario.IdRol == 7)
                    {
                        var cliente = new Cliente
                        {
                            IdUsuario = usuario.IdUsuario,
                            Telefono = Telefono
                        };

                        _context.Clientes.Add(cliente);
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
                    ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
                    return View(usuario);
                }
            }
            else
            {
                var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
                ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
                return View(usuario);
            }
        }



        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                                        .Include(u => u.Cliente)
                                        .Include(u => u.IdRolNavigation)
                                        .FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
            ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,IdRol,NombreUsuario,Correo,Contraseña,Estado,DateRegister,Document")] Usuario usuario, string Telefono)
        {
            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Usuarios
                                                     .Include(u => u.Cliente)
                                                     .FirstOrDefaultAsync(u => u.IdUsuario == usuario.IdUsuario);

                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.NombreUsuario = usuario.NombreUsuario;
                    existingUser.Correo = usuario.Correo;
                    existingUser.Contraseña = usuario.Contraseña;
                    existingUser.Estado = usuario.Estado;
                    existingUser.DateRegister = usuario.DateRegister;
                    existingUser.Document = usuario.Document;
                    existingUser.IdRol = usuario.IdRol;

                    if (existingUser.Cliente != null)
                    {
                        existingUser.Cliente.Telefono = Telefono;
                    }
                    else if (usuario.IdRol == 7)
                    {
                        existingUser.Cliente = new Cliente
                        {
                            IdUsuario = usuario.IdUsuario,
                            Telefono = Telefono
                        };
                    }

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario))
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

            var rolesActivos = _context.Roles.Where(r => r.Estado == true).ToList();
            ViewData["IdRol"] = new SelectList(rolesActivos, "IdRol", "NombreRol", usuario.IdRol);
            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID de usuario inválido.");
            }

            var usuario = await _context.Usuarios
                                        .Include(u => u.Cliente)
                                        .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            try
            {
                if (usuario.Cliente != null)
                {
                    _context.Clientes.Remove(usuario.Cliente);
                }

                var identityUser = await _userManager.FindByEmailAsync(usuario.Correo);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                if (User.Identity.Name == usuario.Correo)
                {
                    await _signInManager.SignOutAsync();
                }

                return Ok();
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest("No se pudo eliminar el usuario ya que tiene un pedido asociado.");
            }
            catch (Exception ex)
            {
                return BadRequest("No se pudo eliminar el usuario. Error: " + ex.Message);
            }
        }

        private bool UsuarioExists(int id)
        {
            return (_context.Usuarios?.Any(e => e.IdUsuario == id)).GetValueOrDefault();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, bool estado)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Estado = estado;

            var identityUser = await _userManager.FindByEmailAsync(usuario.Correo);
            if (identityUser != null)
            {
                identityUser.EmailConfirmed = estado;
                await _userManager.UpdateAsync(identityUser);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var usuario = await _context.Usuarios
                                        .Include(u => u.Cliente)
                                        .Include(u => u.IdRolNavigation)
                                        .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return Json(new
            {
                idUsuario = usuario.IdUsuario,
                nombreUsuario = usuario.NombreUsuario,
                correo = usuario.Correo,
                estado = usuario.Estado,
                dateRegister = usuario.DateRegister,
                document = usuario.Document,
                telefono = usuario.Cliente?.Telefono
            });
        }
    }
}