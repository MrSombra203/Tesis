namespace GestionInteligente.Simulation
{
    public class BudgetManager
    {
        public double PresupuestoDisponible { get; private set; }
        public double CostoTotal { get; private set; }
        public double UtilizacionRecursos { get; private set; }

        private double _presupuestoInicial;

        public void Inicializar(double presupuestoInicial)
        {
            _presupuestoInicial = presupuestoInicial;
            PresupuestoDisponible = presupuestoInicial;
            CostoTotal = 0;
            UtilizacionRecursos = 0;
        }

        public bool DescontarCosto(double monto)
        {
            if (monto > PresupuestoDisponible) return false;

            PresupuestoDisponible -= monto;
            CostoTotal += monto;
            UtilizacionRecursos = _presupuestoInicial > 0
                ? System.Math.Min(100, (CostoTotal / _presupuestoInicial) * 100)
                : 0;
            return true;
        }
    }
}