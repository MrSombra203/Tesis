namespace GestionInteligente.Persistence.Models
{
    public class VehiculoLocal
    {
        public string Id { get; set; }

        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public double Capacidad { get; set; }
        public double CostoOperativo { get; set; }
    }
}