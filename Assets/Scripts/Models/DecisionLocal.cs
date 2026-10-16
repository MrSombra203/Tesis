namespace GestionInteligente.Persistence.Models
{
    public class DecisionLocal
    {
        public string Id { get; set; }
        public string PartidaId { get; set; }
        public int Turno { get; set; }
        public string RutaId { get; set; }
        public string VehiculoId { get; set; }
        public string RecursoId { get; set; }
        public double? Cantidad { get; set; }
        public double CostoCalculado { get; set; }
        public int? EntregaATiempo { get; set; }
        public string Payload { get; set; }
        public string FechaCreacion { get; set; }
    }
}
