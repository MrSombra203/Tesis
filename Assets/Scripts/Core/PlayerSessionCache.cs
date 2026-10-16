namespace GestionInteligente.Core
{
    public static class PlayerSessionCache
    {
        public static string AccessTokenEnMemoria { get; set; }
        public static string EstudianteIdEnMemoria { get; set; }

        public static void Limpiar()
        {
            AccessTokenEnMemoria = null;
            EstudianteIdEnMemoria = null;
        }
    }
}
