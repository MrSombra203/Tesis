namespace GestionInteligente.Simulation
{
    public class ResultadoAduana
    {
        public double Costo { get; private set; }
        public int TiempoDias { get; private set; }
        public string Estado { get; private set; }

        public static ResultadoAduana Aprobado(double costo, int tiempoDias) =>
            new ResultadoAduana { Costo = costo, TiempoDias = tiempoDias, Estado = "aprobado" };

        public static ResultadoAduana Penalizado(double costo, int tiempoDias) =>
            new ResultadoAduana { Costo = costo, TiempoDias = tiempoDias, Estado = "penalizado" };
    }

    public class CustomsEventManager
    {
        public ResultadoAduana ProcesarEvento(double costoBase, int tiempoDiasBase, bool documentacionCompleta)
        {
            return documentacionCompleta
                ? ResultadoAduana.Aprobado(costoBase, tiempoDiasBase)
                : ResultadoAduana.Penalizado(costoBase * 1.25, tiempoDiasBase + 2);
        }
    }
}