using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace fugaz_retro.Models;
public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int? IdRol { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public bool Estado { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateRegister { get; set; }

    public string? Document { get; set; }


    public virtual RolPermiso? IdRolNavigation { get; set; }
    public virtual Cliente? Cliente { get; set; }

}
