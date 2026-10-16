using System;
using GestionInteligente.Persistence;
using GestionInteligente.Persistence.Models;

namespace GestionInteligente.Simulation
{
    public class SimulationManager
    {
        private readonly BudgetManager _budget;
        private readonly RouteManager _routes;
        private readonly CustomsEventManager _customs;
        private readonly EventoDinamicoGenerator _eventos;
        private readonly PartidaRepository _partidaRepo;

        private PartidaLocal _partidaActual;
        private int _turnoActual;

        public SimulationManager(BudgetManager budget, RouteManager routes, CustomsEventManager customs,
            EventoDinamicoGenerator eventos, PartidaRepository partidaRepo)
        {
            _budget = budget;
            _routes = routes;
            _customs = customs;
            _eventos = eventos;
            _partidaRepo = partidaRepo;
        }

        public void Inicializar(PartidaLocal partida, double presupuestoInicial)
        {
            _partidaActual = partida;
            _turnoActual = 0;
            _budget.Inicializar(presupuestoInicial);
        }

        public DecisionLocal ProcesarTurno(VehiculoLocal vehiculo, RutaLocal ruta, RecursoLocal recurso,
            double cantidad, double horasEstimadas, out string motivoRechazo)
        {
            motivoRechazo = null;
            var turnoSiguiente = _turnoActual + 1;

            var demandaTotal = cantidad;
            var validacion = _routes.AsignarRuta(vehiculo, ruta, demandaTotal, horasEstimadas);
            if (!validacion.EsValida)
            {
                motivoRechazo = validacion.MotivoRechazo;
                return null;
            }

            if (validacion.CostoCalculado > _budget.PresupuestoDisponible)
            {
                motivoRechazo = "Presupuesto insuficiente.";
                return null;
            }

            var aTiempo = horasEstimadas <= ruta.VentanaHoras;

            var decision = new DecisionLocal
            {
                Id = Guid.NewGuid().ToString(),
                PartidaId = _partidaActual.Id,
                Turno = turnoSiguiente,
                RutaId = ruta.Id,
                VehiculoId = vehiculo.Id,
                RecursoId = recurso?.Id,
                Cantidad = cantidad,
                CostoCalculado = validacion.CostoCalculado,
                EntregaATiempo = aTiempo ? 1 : 0,
                Payload = "{}"
            };

            if (!_partidaRepo.GuardarDecision(decision))
            {
                motivoRechazo = "No se pudo guardar la decisión local.";
                return null;
            }

            _budget.DescontarCosto(validacion.CostoCalculado);
            _routes.RegistrarEntrega(aTiempo, horasEstimadas, ruta.VentanaHoras);
            _turnoActual = turnoSiguiente;
            return decision;
        }

        public (EventoLocal logistico, EventoLocal aduanero) GenerarEventosDelTurno()
        {
            EventoLocal eventoLog = null;
            EventoLocal eventoAd = null;

            var logisticoGenerado = _eventos.IntentarGenerarEventoLogistico();
            if (logisticoGenerado.HasValue)
                eventoLog = ProcesarEventoLogistico(logisticoGenerado.Value.Tipo, logisticoGenerado.Value.DetalleJson);

            var aduaneroGenerado = _eventos.IntentarGenerarEventoAduanero();
            if (aduaneroGenerado.HasValue)
            {
                var g = aduaneroGenerado.Value;
                eventoAd = ProcesarEventoAduanero(g.Tipo, g.CostoBase, g.TiempoDiasBase, g.DocumentacionCompleta);
            }

            return (eventoLog, eventoAd);
        }

        public EventoLocal ProcesarEventoLogistico(string tipo, string detalleJson)
        {
            var evento = new EventoLocal
            {
                Id = Guid.NewGuid().ToString(),
                PartidaId = _partidaActual.Id,
                Categoria = "logistico",
                Tipo = tipo,
                Detalle = detalleJson
            };
            return _partidaRepo.GuardarEvento(evento) ? evento : null;
        }

        public EventoLocal ProcesarEventoAduanero(string tipo, double costoBase, int tiempoDiasBase, bool documentacionCompleta)
        {
            var resultado = _customs.ProcesarEvento(costoBase, tiempoDiasBase, documentacionCompleta);

            var evento = new EventoLocal
            {
                Id = Guid.NewGuid().ToString(),
                PartidaId = _partidaActual.Id,
                Categoria = "aduanero",
                Tipo = tipo,
                Costo = resultado.Costo,
                TiempoDias = resultado.TiempoDias,
                Estado = resultado.Estado,
                Detalle = "{}"
            };

            if (!_partidaRepo.GuardarEvento(evento)) return null;
            _budget.DescontarCosto(resultado.Costo);
            return evento;
        }

        public double CostoTotalActual => _budget.CostoTotal;
        public double PresupuestoDisponible => _budget.PresupuestoDisponible;
        public double UtilizacionRecursosActual => _budget.UtilizacionRecursos;
        public double PctEntregasATiempoActual => _routes.PctEntregasATiempo;
        public double EficienciaRutasActual => _routes.EficienciaRutas;
    }
}