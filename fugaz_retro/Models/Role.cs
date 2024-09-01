using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fugaz_retro.Models
{
    public partial class Role
    {
        public int IdRol { get; set; }

        [Required]
        public string NombreRol { get; set; } = null!;

        [Column(TypeName = "tinyint(1)")]
        public bool Estado { get; set; }

        public virtual ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}