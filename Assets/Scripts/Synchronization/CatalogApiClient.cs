using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GestionInteligente.Synchronization
{
    [Serializable] public class VehiculoDto { public string id; public string nombre; public string tipo; public double capacidad; public double costoOperativo; }
    [Serializable] public class RutaDto { public string id; public string origen; public string destino; public double distanciaKm; public double ventanaHoras; }
    [Serializable] public class RecursoDto { public string id; public string nombre; public string unidadMedida; public double costoUnitario; }
    [Serializable] public class ConfiguracionItemDto { public string clave; public string valor; }
    [Serializable]
    public class EscenarioActivoDto
    {
        public string id;
        public string nombre;
        public string dificultad;
        public string[] rutasHabilitadas;
        public string actualizadoEn;
        public ConfiguracionItemDto[] configuraciones;
    }

    [Serializable] internal class VehiculosWrapper { public VehiculoDto[] items; }
    [Serializable] internal class RutasWrapper { public RutaDto[] items; }
    [Serializable] internal class RecursosWrapper { public RecursoDto[] items; }

    internal static class ApiArrayWrapper
    {
        public static string Envolver(string jsonArray) => "{\"items\":" + jsonArray + "}";
    }

    public class CatalogApiClient
    {
        private readonly string _baseUrl;
        private readonly Func<string> _obtenerAccessToken;

        public CatalogApiClient(string baseUrl, Func<string> obtenerAccessToken)
        {
            _baseUrl = baseUrl;
            _obtenerAccessToken = obtenerAccessToken;
        }

        public IEnumerator ObtenerVehiculos(Action<VehiculoDto[]> onExito, Action<string> onError) =>
            ObtenerArray<VehiculoDto, VehiculosWrapper>("/api/v1/catalogos/vehiculos", w => w.items, onExito, onError);

        public IEnumerator ObtenerRutas(Action<RutaDto[]> onExito, Action<string> onError) =>
            ObtenerArray<RutaDto, RutasWrapper>("/api/v1/catalogos/rutas", w => w.items, onExito, onError);

        public IEnumerator ObtenerRecursos(Action<RecursoDto[]> onExito, Action<string> onError) =>
            ObtenerArray<RecursoDto, RecursosWrapper>("/api/v1/catalogos/recursos", w => w.items, onExito, onError);

        public IEnumerator ObtenerEscenarioActivo(Action<EscenarioActivoDto> onExito, Action<string> onError)
        {
            using var request = UnityWebRequest.Get($"{_baseUrl}/api/v1/escenarios/activo");
            request.SetRequestHeader("Authorization", $"Bearer {_obtenerAccessToken()}");
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError($"No se pudo descargar el escenario activo ({request.responseCode}): {request.error}");
                yield break;
            }

            var dto = JsonUtility.FromJson<EscenarioActivoDto>(request.downloadHandler.text);
            onExito(dto);
        }

        private IEnumerator ObtenerArray<TItem, TWrapper>(string ruta, Func<TWrapper, TItem[]> extraer,
            Action<TItem[]> onExito, Action<string> onError)
        {
            using var request = UnityWebRequest.Get($"{_baseUrl}{ruta}");
            request.SetRequestHeader("Authorization", $"Bearer {_obtenerAccessToken()}");
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError($"No se pudo descargar {ruta} ({request.responseCode}): {request.error}");
                yield break;
            }

            var wrapperJson = ApiArrayWrapper.Envolver(request.downloadHandler.text);
            var wrapper = JsonUtility.FromJson<TWrapper>(wrapperJson);
            onExito(extraer(wrapper));
        }
    }
}