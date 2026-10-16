using System;
using UnityEngine;

namespace GestionInteligente.Persistence
{
    public abstract class LocalRepository
    {
        protected DatabaseConnection Connection { get; }

        protected LocalRepository(DatabaseConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        protected bool EjecutarTransaccionSegura(Action operacion, string nombreOperacion)
        {
            try
            {
                Connection.BeginTransaction();
                operacion();
                Connection.Commit();
                return true;
            }
            catch (Exception exception)
            {
                try { Connection.Rollback(); }
                catch (Exception rollbackException)
                {
                    Debug.LogError($"[LocalRepository] Falló el rollback de {nombreOperacion}: {rollbackException.Message}");
                }

                Debug.LogError($"[LocalRepository] Falló {nombreOperacion}: {exception.Message}");
                return false;
            }
        }

        protected static string AhoraUtcIso() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
    }
}
