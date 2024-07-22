using System;
using System.Collections.Generic;

namespace fugaz_retro.Models;

public partial class DetalleCompra
{
    public int IdDetalleCompra { get; set; }

    public int? IdCompra { get; set; }

    public int? IdInsumo { get; set; }

    public int Cantidad { get; set; }
    public double PrecioUnitario { get; set; }

    public virtual Compra? IdCompraNavigation { get; set; }

    public virtual Insumo? IdInsumoNavigation { get; set; }
}