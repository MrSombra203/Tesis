using System;

namespace GestionInteligente.Simulation
{
    public readonly struct EventoLogisticoGenerado
    {
        public string Tipo { get; }
        public string DetalleJson { get; }
        public EventoLogisticoGenerado(string tipo, string detalleJson) { Tipo = tipo; DetalleJson = detalleJson; }
    }

    public readonly struct EventoAduaneroGenerado
    {
        public string Tipo { get; }
        public double CostoBase { get; }
        public int TiempoDiasBase { get; }
        public bool DocumentacionCompleta { get; }
        public EventoAduaneroGenerado(string tipo, double costoBase, int tiempoDiasBase, bool documentacionCompleta)
        {
            Tipo = tipo; CostoBase = costoBase; TiempoDiasBase = tiempoDiasBase; DocumentacionCompleta = documentacionCompleta;
        }
    }

    public class EventoDinamicoGenerator
    {
        private readonly Random _random;
        private readonly double _probabilidadLogistico;
        private readonly double _probabilidadAduanero;

        public EventoDinamicoGenerator(double probabilidadLogistico = 0.25, double probabilidadAduanero = 0.15, int? semilla = null)
        {
            _random = semilla.HasValue ? new Random(semilla.Value) : new Random();
            _probabilidadLogistico = probabilidadLogistico;
            _probabilidadAduanero = probabilidadAduanero;
        }

        public EventoLogisticoGenerado? IntentarGenerarEventoLogistico()
        {
            if (_random.NextDouble() > _probabilidadLogistico) return null;

            var tipos = new[] { "trafico", "demanda", "disrupcion" };
            var tipo = tipos[_random.Next(tipos.Length)];
            var detalle = tipo switch
            {
                "trafico" => "{\"retrasoHoras\":" + _random.Next(1, 5) + "}",
                "demanda" => "{\"incrementoPorcentaje\":" + _random.Next(10, 40) + "}",
                _ => "{\"disrupcionDias\":" + _random.Next(1, 3) + "}"
            };
            return new EventoLogisticoGenerado(tipo, detalle);
        }

        public EventoAduaneroGenerado? IntentarGenerarEventoAduanero()
        {
            if (_random.NextDouble() > _probabilidadAduanero) return null;

            var tipo = _random.NextDouble() < 0.5 ? "importacion" : "exportacion";
            var costoBase = _random.Next(100, 500);
            var tiempoBase = _random.Next(1, 4);
            var documentacionCompleta = _random.NextDouble() > 0.3;
            return new EventoAduaneroGenerado(tipo, costoBase, tiempoBase, documentacionCompleta);
        }
    }
}