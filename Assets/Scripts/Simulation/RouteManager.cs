using System;
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
        private double _eficienciaAcumulada;

        public double PctEntregasATiempo => _entregasTotales == 0 ? 100 : (_entregasATiempo * 100.0 / _entregasTotales);
        public double EficienciaRutas => _entregasTotales == 0 ? 100 : _eficienciaAcumulada / _entregasTotales;

        public ValidacionRuta AsignarRuta(VehiculoLocal vehiculo, RutaLocal ruta, double demandaTotal, double horasEstimadas)
        {
            if (vehiculo == null)
                return ValidacionRuta.Rechazada("No se seleccionó un vehículo.");
            if (ruta == null)
                return ValidacionRuta.Rechazada("No se seleccionó una ruta.");
            if (string.IsNullOrWhiteSpace(vehiculo.Id) || string.IsNullOrWhiteSpace(ruta.Id))
                return ValidacionRuta.Rechazada("El vehículo y la ruta deben tener identificadores válidos.");
            if (!EsFinito(demandaTotal) || demandaTotal <= 0)
                return ValidacionRuta.Rechazada("La cantidad debe ser un valor finito mayor que cero.");
            if (!EsFinito(horasEstimadas) || horasEstimadas < 0)
                return ValidacionRuta.Rechazada("Las horas estimadas deben ser un valor finito no negativo.");
            if (!EsFinito(vehiculo.Capacidad) || vehiculo.Capacidad < 0
                || !EsFinito(vehiculo.CostoOperativo) || vehiculo.CostoOperativo < 0
                || !EsFinito(ruta.DistanciaKm) || ruta.DistanciaKm < 0
                || !EsFinito(ruta.VentanaHoras) || ruta.VentanaHoras < 0)
                return ValidacionRuta.Rechazada("El vehículo o la ruta contienen valores de configuración inválidos.");
            if (demandaTotal > vehiculo.Capacidad)
                return ValidacionRuta.Rechazada("Excede la capacidad del vehículo.");
            if (horasEstimadas > ruta.VentanaHoras)
                return ValidacionRuta.Rechazada("Fuera de la ventana horaria permitida.");

            var costo = vehiculo.CostoOperativo + (ruta.DistanciaKm * 0.01 * demandaTotal);
            if (!EsFinito(costo))
                return ValidacionRuta.Rechazada("No se pudo calcular un costo válido para la ruta.");

            return ValidacionRuta.Aceptada(costo);
        }

        public void RegistrarEntrega(bool aTiempo, double horasEstimadas, double ventanaHoras)
        {
            _entregasTotales++;
            if (aTiempo) _entregasATiempo++;

            var eficiencia = ventanaHoras <= 0 ? 0 : System.Math.Max(0, 100 - (horasEstimadas / ventanaHoras * 100 - 100));
            _eficienciaAcumulada += System.Math.Min(100, eficiencia);
        }

        private static bool EsFinito(double valor) => !double.IsNaN(valor) && !double.IsInfinity(valor);
    }
}