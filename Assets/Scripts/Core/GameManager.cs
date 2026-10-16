using GestionInteligente.Persistence;
using GestionInteligente.Persistence.Models;
using GestionInteligente.Simulation;
using UnityEngine;

namespace GestionInteligente.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Configuración del nivel (ejemplo — normalmente vendría de escenario_local)")]
        [SerializeField] private string nivelId;
        [SerializeField] private double presupuestoInicial = 5000;

        private SimulationManager _simulation;
        private KPIManager _kpiManager;
        private PartidaLocal _partidaActual;

        private void Start()
        {
            IniciarNivel();
        }

        public void IniciarNivel()
        {
            var budget = new BudgetManager();
            var routes = new RouteManager();
            var customs = new CustomsEventManager();
            var eventos = new EventoDinamicoGenerator();
            _kpiManager = new KPIManager();

            _partidaActual = AppBootstrap.Partidas.RegistrarPartida(nivelId);
            _simulation = new SimulationManager(budget, routes, customs, eventos, AppBootstrap.Partidas);
            _simulation.Inicializar(_partidaActual, presupuestoInicial);

            Debug.Log($"[GameManager] Partida iniciada: {_partidaActual.Id}");
        }

        public void JugarTurno(VehiculoLocal vehiculo, RutaLocal ruta, RecursoLocal recurso, double cantidad, double horasEstimadas)
        {
            var decision = _simulation.ProcesarTurno(vehiculo, ruta, recurso, cantidad, horasEstimadas, out var motivoRechazo);
            if (decision == null)
            {
                Debug.LogWarning($"[GameManager] Turno rechazado: {motivoRechazo}");
                return;
            }
            Debug.Log($"[GameManager] Turno {decision.Turno} guardado. Costo: {decision.CostoCalculado:F2}, presupuesto restante: {_simulation.PresupuestoDisponible:F2}");

            var (eventoLog, eventoAd) = _simulation.GenerarEventosDelTurno();
            if (eventoLog != null) Debug.Log($"[GameManager] Evento logístico: {eventoLog.Tipo} — {eventoLog.Detalle}");
            if (eventoAd != null) Debug.Log($"[GameManager] Evento aduanero: {eventoAd.Tipo}, estado {eventoAd.Estado}, costo {eventoAd.Costo:F2}. Presupuesto restante: {_simulation.PresupuestoDisponible:F2}");
        }

        public void FinalizarNivel()
        {
            var kpiCalculado = _kpiManager.CalcularKpiFinal(
                costoTotal: _simulation.CostoTotalActual,
                pctEntregasATiempo: _simulation.PctEntregasATiempoActual,
                utilizacionRecursos: _simulation.UtilizacionRecursosActual,
                eficienciaRutas: _simulation.EficienciaRutasActual);

            var kpiLocal = new KpiLocal
            {
                CostoTotal = kpiCalculado.CostoTotal,
                PctEntregasATiempo = kpiCalculado.PctEntregasATiempo,
                UtilizacionRecursos = kpiCalculado.UtilizacionRecursos,
                EficienciaRutas = kpiCalculado.EficienciaRutas,
                PuntuacionGlobal = kpiCalculado.PuntuacionGlobal
            };

            var estudianteId = PlayerSessionCache.EstudianteIdEnMemoria;
            var exito = AppBootstrap.Partidas.FinalizarPartida(_partidaActual, kpiLocal, AppBootstrap.SyncRepo, estudianteId);

            Debug.Log(exito
                ? $"[GameManager] Partida {_partidaActual.Id} finalizada. Puntuación: {kpiLocal.PuntuacionGlobal}"
                : "[GameManager] Error al finalizar la partida (ver log de LocalRepository).");
        }
    }
}