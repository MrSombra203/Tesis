namespace GestionInteligente.Simulation
{
    public class KpiResultado
    {
        public double CostoTotal { get; }
        public double PctEntregasATiempo { get; }
        public double UtilizacionRecursos { get; }
        public double EficienciaRutas { get; }
        public double PuntuacionGlobal { get; }

        public KpiResultado(double costoTotal, double pctEntregasATiempo, double utilizacionRecursos, double eficienciaRutas, double puntuacionGlobal)
        {
            CostoTotal = costoTotal;
            PctEntregasATiempo = pctEntregasATiempo;
            UtilizacionRecursos = utilizacionRecursos;
            EficienciaRutas = eficienciaRutas;
            PuntuacionGlobal = puntuacionGlobal;
        }
    }

    public class KPIManager
    {
        public KpiResultado CalcularKpiFinal(double costoTotal, double pctEntregasATiempo, double utilizacionRecursos, double eficienciaRutas)
        {
            var puntuacion = (pctEntregasATiempo * 0.4) + (eficienciaRutas * 0.3)
                            + (utilizacionRecursos * 0.2) - (costoTotal * 0.0001);
            puntuacion = System.Math.Max(0, System.Math.Round(puntuacion, 2));

            return new KpiResultado(costoTotal, pctEntregasATiempo, utilizacionRecursos, eficienciaRutas, puntuacion);
        }
    }
}
