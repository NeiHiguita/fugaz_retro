using System;
using System.Collections.Generic;

namespace fugaz_retro.Models;

public partial class Permiso
{
    public int IdPermiso { get; set; }

    public string NombrePermiso { get; set; } = null!;

    public bool EstadoPermiso { get; set; }

    public virtual ICollection<RolPermiso> RolPermisos { get; } = new List<RolPermiso>();
}
