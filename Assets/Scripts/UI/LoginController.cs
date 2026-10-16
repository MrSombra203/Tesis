using GestionInteligente.Auth;
using GestionInteligente.Core;
using GestionInteligente.Persistence;
using GestionInteligente.Synchronization;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GestionInteligente.UI
{
    public class LoginController : MonoBehaviour
    {
        [Header("API")]
        [SerializeField] private string apiBaseUrl = "https://api-portal-docente.example.com";

        [Header("UI (arrastrar desde el Inspector)")]
        [SerializeField] private InputField correoInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text mensajeErrorText;

        [Header("Navegación")]
        [SerializeField] private string escenaSiguiente = "SimulationLevel";

        private AuthApiClient _authClient;
        private SesionRepository _sesionRepo;

        private void Awake()
        {
            _authClient = new AuthApiClient(apiBaseUrl);
            _sesionRepo = new SesionRepository(AppBootstrap.Db);

            loginButton.onClick.AddListener(OnLoginButtonClick);
            if (mensajeErrorText != null) mensajeErrorText.text = "";

            IntentarAutoLogin();
        }

        private void IntentarAutoLogin()
        {
            var (perfil, sesion) = _sesionRepo.ObtenerSesionGuardada();
            if (perfil != null && correoInput != null)
                correoInput.text = perfil.Correo;
        }

        private void OnLoginButtonClick()
        {
            var correo = correoInput.text.Trim();
            var password = passwordInput.text;

            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                MostrarError("Ingresa tu correo y tu contraseña.");
                return;
            }

            loginButton.interactable = false;
            MostrarError("");
            StartCoroutine(_authClient.Login(correo, password, resultado => OnLoginCompletado(resultado, correo)));
        }

        private void OnLoginCompletado(LoginResult resultado, string correo)
        {
            loginButton.interactable = true;

            if (!resultado.EsExitoso)
            {
                MostrarError(resultado.MensajeError);
                return;
            }

            var estudianteId = JwtPayloadReader.ObtenerClaim(resultado.AccessToken, "estudianteId");
            if (string.IsNullOrEmpty(estudianteId))
            {
                MostrarError("Esta cuenta no tiene acceso al videojuego.");
                return;
            }
            var refreshCifrado = SecureTokenStorage.Proteger(resultado.RefreshToken);
            var sesionGuardada = _sesionRepo.GuardarSesion(
                estudianteId: estudianteId,
                correo: correo,
                nombreCompleto: correo,
                refreshTokenCifrado: refreshCifrado,
                expiraEnUtc: resultado.ExpiraEnUtc);
            if (!sesionGuardada)
            {
                MostrarError("No se pudo guardar la sesión de forma segura en este dispositivo.");
                return;
            }
            PlayerSessionCache.AccessTokenEnMemoria = resultado.AccessToken;
            PlayerSessionCache.EstudianteIdEnMemoria = estudianteId;

            var configSync = gameObject.AddComponent<ConfigSyncManager>();
            configSync.Inicializar(AppBootstrap.Db, apiBaseUrl, () => PlayerSessionCache.AccessTokenEnMemoria);
            StartCoroutine(configSync.SincronizarConfiguracion(exito => SceneManager.LoadScene(escenaSiguiente)));
        }

        private void MostrarError(string mensaje)
        {
            if (mensajeErrorText != null) mensajeErrorText.text = mensaje;
            else if (!string.IsNullOrEmpty(mensaje)) Debug.LogWarning($"[LoginController] {mensaje}");
        }
    }
}
