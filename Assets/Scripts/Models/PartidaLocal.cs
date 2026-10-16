namespace GestionInteligente.Persistence.Models
{
    public class PartidaLocal
    {
        public string Id { get; set; }
        public string NivelId { get; set; }
        public string IdempotencyKey { get; set; }
        public int Finalizada { get; set; }
        public string EstadoSincronizacion { get; set; }
        public string FechaSincronizacion { get; set; }
        public int VersionRegistro { get; set; }
        public string FechaCreacion { get; set; }
        public string FechaModificacion { get; set; }
        public int Eliminado { get; set; }
    }
}
