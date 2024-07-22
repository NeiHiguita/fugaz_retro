using System;
using System.Collections.Generic;

namespace fugaz_retro.Models;

public partial class PerdidaInsumo
{
    public int IdPerdidaInsumo { get; set; }

    public int? IdInsumo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public virtual Insumo? IdInsumoNavigation { get; set; }
}
