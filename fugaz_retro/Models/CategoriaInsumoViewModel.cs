namespace fugaz_retro.Models
{
    public class CategoriaInsumoViewModel
    {
        public List<CategoriaInsumo> Categorias { get; set; } = new List<CategoriaInsumo>();
        public CategoriaInsumo NuevaCategoria { get; set; } = new CategoriaInsumo();

        public CategoriaInsumoViewModel()
        {
            NuevaCategoria = new CategoriaInsumo
            {
                EstadoCategoria = true
            };
        }
    }
}

