using System.Collections.Generic;
using GestionInteligente.Persistence.Models;

namespace GestionInteligente.Simulation
{
    public class ValidacionRuta
    {
        public bool EsValida { get; private set; }
        public string MotivoRechazo { get; private set; }
        public double CostoCalculado { get; private set; }

        public static ValidacionRuta Rechazada(string motivo) => new ValidacionRuta { EsValida = false, MotivoRechazo = motivo };
        public static ValidacionRuta Aceptada(double costo) => new ValidacionRuta { EsValida = true, CostoCalculado = costo };
    }

    public class RouteManager
    {
        private int _entregasTotales;
        private int _entregasATiempo;
        private readonly List<double> _eficienciaPorEntrega = new();

        public double PctEntregasATiempo => _entregasTotales == 0 ? 100 : (_entregasATiempo * 100.0 / _entregasTotales);
        public double EficienciaRutas => _eficienciaPorEntrega.Count == 0 ? 100 : Promedio(_eficienciaPorEntrega);

        public ValidacionRuta AsignarRuta(VehiculoLocal vehiculo, RutaLocal ruta, double demandaTotal, double horasEstimadas)
        {
            if (demandaTotal > vehiculo.Capacidad)
                return ValidacionRuta.Rechazada("Excede la capacidad del vehículo.");
            if (horasEstimadas > ruta.VentanaHoras)
                return ValidacionRuta.Rechazada("Fuera de la ventana horaria permitida.");

            var costo = vehiculo.CostoOperativo + (ruta.DistanciaKm * 0.01 * demandaTotal);
            return ValidacionRuta.Aceptada(costo);
        }

        public void RegistrarEntrega(bool aTiempo, double horasEstimadas, double ventanaHoras)
        {
            _entregasTotales++;
            if (aTiempo) _entregasATiempo++;

            var eficiencia = ventanaHoras <= 0 ? 0 : System.Math.Max(0, 100 - (horasEstimadas / ventanaHoras * 100 - 100));
            _eficienciaPorEntrega.Add(System.Math.Min(100, eficiencia));
        }

        private static double Promedio(List<double> valores)
        {
            double suma = 0;
            foreach (var v in valores) suma += v;
            return suma / valores.Count;
        }
    }
}