namespace GestionInteligente.Persistence.Models
{
    public class KpiLocal
    {
        public string Id { get; set; }
        public string PartidaId { get; set; }
        public double CostoTotal { get; set; }
        public double PctEntregasATiempo { get; set; }
        public double UtilizacionRecursos { get; set; }
        public double EficienciaRutas { get; set; }
        public double PuntuacionGlobal { get; set; }
        public string FechaCreacion { get; set; }
    }
}
