using System;
using System.Globalization;
using System.IO;
using System.Linq;
using GestionInteligente.Persistence;
using UnityEngine;

namespace GestionInteligente.Core
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        private static AppBootstrap _instance;

        public static DatabaseConnection Db { get; private set; }
        public static PartidaRepository Partidas { get; private set; }
        public static SyncRepository SyncRepo { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Db = null;
            Partidas = null;
            SyncRepo = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CrearBootstrap()
        {
            if (_instance != null) return;
            var bootstrap = new GameObject(nameof(AppBootstrap));
            bootstrap.AddComponent<AppBootstrap>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                var databasePath = Path.Combine(Application.persistentDataPath, "gestion_inteligente.db");
                Db = new DatabaseConnection(databasePath);
                AplicarMigraciones(Db);
                Partidas = new PartidaRepository(Db);
                SyncRepo = new SyncRepository(Db);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AppBootstrap] No se pudo iniciar la base de datos local: {exception}");
                throw;
            }
        }

        private static void AplicarMigraciones(DatabaseConnection db)
        {
            var migrationDirectory = Path.Combine(Application.streamingAssetsPath, "Database");
            if (!Directory.Exists(migrationDirectory))
                throw new DirectoryNotFoundException($"No se encontró el directorio de migraciones: {migrationDirectory}");

            var hasVersionTable = db.QueryInt(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'version_esquema'") > 0;
            var currentVersion = hasVersionTable
                ? db.QueryInt("SELECT COALESCE(MAX(version), 0) FROM version_esquema")
                : 0;

            var migrationFiles = Directory.GetFiles(migrationDirectory, "*.sql")
                .Where(path => Path.GetFileName(path).Length >= 4
                    && char.IsDigit(Path.GetFileName(path)[0])
                    && char.IsDigit(Path.GetFileName(path)[1])
                    && char.IsDigit(Path.GetFileName(path)[2])
                    && Path.GetFileName(path)[3] == '_')
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
            foreach (var path in migrationFiles)
            {
                var fileName = Path.GetFileName(path);
                var version = int.Parse(fileName.Substring(0, 3), CultureInfo.InvariantCulture);
                if (version <= currentVersion) continue;

                db.BeginTransaction();
                try
                {
                    db.ExecuteScript(File.ReadAllText(path));
                    db.Commit();
                    currentVersion = version;
                    Debug.Log($"[AppBootstrap] Migración SQLite aplicada: {fileName}");
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
        }

        private void OnApplicationQuit()
        {
            Db?.Dispose();
            Db = null;
        }
    }
}
