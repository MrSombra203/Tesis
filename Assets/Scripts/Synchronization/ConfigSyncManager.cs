using System;
using System.Collections;
using System.Linq;
using GestionInteligente.Persistence;
using GestionInteligente.Persistence.Models;
using UnityEngine;

namespace GestionInteligente.Synchronization
{
    public class ConfigSyncManager : MonoBehaviour
    {
        private CatalogApiClient _apiClient;
        private CatalogoRepository _catalogoRepo;

        public void Inicializar(DatabaseConnection db, string apiBaseUrl, Func<string> obtenerAccessToken)
        {
            _catalogoRepo = new CatalogoRepository(db);
            _apiClient = new CatalogApiClient(apiBaseUrl, obtenerAccessToken);
        }

        public IEnumerator SincronizarConfiguracion(Action<bool> onCompletado)
        {
            bool huboError = false;

            yield return _apiClient.ObtenerVehiculos(
                dtos =>
                {
                    if (!_catalogoRepo.GuardarVehiculos(dtos.Select(d => new VehiculoLocal
                    {
                        Id = d.id,
                        Nombre = d.nombre,
                        Tipo = d.tipo,
                        Capacidad = d.capacidad,
                        CostoOperativo = d.costoOperativo
                    })))
                    {
                        Debug.LogError("[ConfigSyncManager] No se pudieron guardar los vehículos descargados.");
                        huboError = true;
                    }
                },
                error => { Debug.LogWarning($"[ConfigSyncManager] {error}"); huboError = true; });

            yield return _apiClient.ObtenerRutas(
                dtos =>
                {
                    if (!_catalogoRepo.GuardarRutas(dtos.Select(d => new RutaLocal
                    {
                        Id = d.id,
                        Origen = d.origen,
                        Destino = d.destino,
                        DistanciaKm = d.distanciaKm,
                        VentanaHoras = d.ventanaHoras
                    })))
                    {
                        Debug.LogError("[ConfigSyncManager] No se pudieron guardar las rutas descargadas.");
                        huboError = true;
                    }
                },
                error => { Debug.LogWarning($"[ConfigSyncManager] {error}"); huboError = true; });

            yield return _apiClient.ObtenerRecursos(
                dtos =>
                {
                    if (!_catalogoRepo.GuardarRecursos(dtos.Select(d => new RecursoLocal
                    {
                        Id = d.id,
                        Nombre = d.nombre,
                        UnidadMedida = d.unidadMedida,
                        CostoUnitario = d.costoUnitario
                    })))
                    {
                        Debug.LogError("[ConfigSyncManager] No se pudieron guardar los recursos descargados.");
                        huboError = true;
                    }
                },
                error => { Debug.LogWarning($"[ConfigSyncManager] {error}"); huboError = true; });

            yield return _apiClient.ObtenerEscenarioActivo(
                dto =>
                {
                    var escenario = new EscenarioLocal
                    {
                        Id = dto.id,
                        Nombre = dto.nombre,
                        Dificultad = dto.dificultad,
                        RutasHabilitadas = "[" + string.Join(",", (dto.rutasHabilitadas ?? Array.Empty<string>())
                            .Select(r => $"\"{r.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"")) + "]",
                        ActualizadoEn = dto.actualizadoEn
                    };
                    var configuraciones = (dto.configuraciones ?? Array.Empty<ConfiguracionItemDto>())
                        .Select(c => (c.clave, c.valor));
                    if (!_catalogoRepo.GuardarEscenarioActivo(escenario, configuraciones))
                    {
                        Debug.LogError("[ConfigSyncManager] No se pudo guardar el escenario activo.");
                        huboError = true;
                    }
                },
                error => { Debug.LogWarning($"[ConfigSyncManager] {error}"); huboError = true; });

            if (huboError)
                Debug.LogWarning("[ConfigSyncManager] La configuración se sincronizó parcialmente; el juego sigue con lo último guardado localmente.");
            else
                Debug.Log("[ConfigSyncManager] Catálogos y escenario activo sincronizados correctamente.");

            onCompletado(!huboError);
        }
    }
}