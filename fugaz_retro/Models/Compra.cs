using System;
using System.Collections.Generic;

namespace fugaz_retro.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public int? IdProveedor { get; set; }

    public DateTime FechaCompra { get; set; }

    public string MetodoPago { get; set; } = null!;

    public double Subtotal { get; set; }

    public double Iva { get; set; }

    public double? Descuento { get; set; }

    public double PrecioTotal { get; set; }

    public string? TipoTransferencia { get; set; }
    public virtual ICollection<DetalleCompra> DetalleCompras { get; } = new List<DetalleCompra>();

    public virtual Proveedor? IdProveedorNavigation { get; set; }

}
