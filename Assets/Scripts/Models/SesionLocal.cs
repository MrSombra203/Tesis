namespace GestionInteligente.Persistence.Models
{
    public class SesionLocal
    {
        public int Id { get; set; } = 1;

        public string PerfilId { get; set; }

        public byte[] RefreshTokenCifrado { get; set; }

        public string ExpiraEn { get; set; }

        public string CreadoEn { get; set; }
    }
}