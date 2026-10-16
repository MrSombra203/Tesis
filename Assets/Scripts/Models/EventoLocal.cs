namespace GestionInteligente.Persistence.Models
{
    public class EventoLocal
    {
        public string Id { get; set; }
        public string PartidaId { get; set; }
        public string Categoria { get; set; }
        public string Tipo { get; set; }
        public double? Costo { get; set; }
        public int? TiempoDias { get; set; }
        public string Estado { get; set; }
        public string Detalle { get; set; }
        public string FechaCreacion { get; set; }
    }
}
