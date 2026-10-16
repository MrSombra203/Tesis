using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GestionInteligente.Auth
{
    [Serializable]
    internal class LoginRequestPayload
    {
        public string correo;
        public string password;
    }

    [Serializable]
    internal class LoginResponsePayload
    {
        public string accessToken;
        public string refreshToken;
        public string expiraEn;
        public string[] roles;
    }

    [Serializable]
    internal class ApiErrorPayload
    {
        public string code;
        public string message;
    }

    public class LoginResult
    {
        public bool EsExitoso { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public DateTime ExpiraEnUtc { get; private set; }
        public string[] Roles { get; private set; }
        public string MensajeError { get; private set; }

        public static LoginResult Exito(string accessToken, string refreshToken, DateTime expiraEnUtc, string[] roles) =>
            new LoginResult { EsExitoso = true, AccessToken = accessToken, RefreshToken = refreshToken, ExpiraEnUtc = expiraEnUtc, Roles = roles };

        public static LoginResult Fallo(string mensajeError) =>
            new LoginResult { EsExitoso = false, MensajeError = mensajeError };
    }

    public class AuthApiClient
    {
        private readonly string _baseUrl;
        public AuthApiClient(string baseUrl) => _baseUrl = baseUrl;

        public IEnumerator Login(string correo, string password, Action<LoginResult> onCompletado)
        {
            var body = JsonUtility.ToJson(new LoginRequestPayload { correo = correo, password = password });

            using var request = new UnityWebRequest($"{_baseUrl}/api/v1/auth/login", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                onCompletado(LoginResult.Fallo("Sin conexión. Verifica tu red e intenta nuevamente."));
                yield break;
            }

            var codigo = (int)request.responseCode;
            if (codigo == 200)
            {
                var respuesta = JsonUtility.FromJson<LoginResponsePayload>(request.downloadHandler.text);
                var expiraEn = DateTime.Parse(respuesta.expiraEn).ToUniversalTime();
                onCompletado(LoginResult.Exito(respuesta.accessToken, respuesta.refreshToken, expiraEn, respuesta.roles));
            }
            else if (codigo == 401)
            {
                onCompletado(LoginResult.Fallo("Correo o contraseña incorrectos."));
            }
            else
            {
                var mensaje = $"Error del servidor ({codigo}).";
                try
                {
                    var error = JsonUtility.FromJson<ApiErrorPayload>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(error?.message)) mensaje = error.message;
                }
                catch { }
                onCompletado(LoginResult.Fallo(mensaje));
            }
        }
    }
}