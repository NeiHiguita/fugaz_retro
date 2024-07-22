using System;
using System.Collections.Generic;

namespace fugaz_retro.Models;

public partial class Role
{
    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public virtual ICollection<RolPermiso> RolPermisos { get; } = new List<RolPermiso>();
}
