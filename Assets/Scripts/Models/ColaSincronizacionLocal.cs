namespace GestionInteligente.Persistence.Models
{
    public class ColaSincronizacionLocal
    {
        public string Id { get; set; }
        public string Entidad { get; set; }
        public string EntidadId { get; set; }
        public string IdempotencyKey { get; set; }
        public string PayloadJson { get; set; }
        public string Estado { get; set; }
        public int NumeroIntentos { get; set; }
        public string FechaUltimoIntento { get; set; }
        public string FechaCreacion { get; set; }
        public string FechaSincronizacion { get; set; }
        public string MensajeError { get; set; }
    }
}
