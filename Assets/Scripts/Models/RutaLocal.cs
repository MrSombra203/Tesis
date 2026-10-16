namespace GestionInteligente.Persistence.Models
{
    public class RutaLocal
    {
        public string Id { get; set; }

        public string Origen { get; set; }
        public string Destino { get; set; }
        public double DistanciaKm { get; set; }
        public double VentanaHoras { get; set; }
    }
}