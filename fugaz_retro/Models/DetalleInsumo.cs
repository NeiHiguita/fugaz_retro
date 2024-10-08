﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace fugaz_retro.Models
{
    public class DetalleInsumo
    {
        public int IdDetalleInsumo { get; set; }

        public int IdDetalleProducto { get; set; }

        public int IdInsumo { get; set; }

        public string UnidadMedida { get; set; } = null!;

        public string Color { get; set; } = null!;

        public double Cantidad { get; set; }

        public virtual DetalleProducto? DetalleProducto { get; set; }
        public virtual Insumo? Insumo { get; set; }
    }
}
