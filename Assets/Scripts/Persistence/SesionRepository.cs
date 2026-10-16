using GestionInteligente.Persistence.Models;
using System;

namespace GestionInteligente.Persistence
{
    public class SesionRepository : LocalRepository
    {
        public SesionRepository(DatabaseConnection db) : base(db) { }

        public bool GuardarSesion(string estudianteId, string correo, string nombreCompleto,
            byte[] refreshTokenCifrado, DateTime expiraEnUtc)
        {
            return EjecutarTransaccionSegura(() =>
            {
                Connection.Execute(
                    "INSERT INTO perfil_local (id, correo, nombre_completo, sincronizado_en) VALUES (?, ?, ?, ?) " +
                    "ON CONFLICT(id) DO UPDATE SET correo = excluded.correo, nombre_completo = excluded.nombre_completo, sincronizado_en = excluded.sincronizado_en",
                    estudianteId, correo, nombreCompleto, AhoraUtcIso());

                Connection.Execute(
                    "INSERT INTO sesion_local (id, perfil_id, refresh_token_cifrado, expira_en, creado_en) VALUES (1, ?, ?, ?, ?) " +
                    "ON CONFLICT(id) DO UPDATE SET perfil_id = excluded.perfil_id, refresh_token_cifrado = excluded.refresh_token_cifrado, expira_en = excluded.expira_en, creado_en = excluded.creado_en",
                    estudianteId, refreshTokenCifrado, expiraEnUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"), AhoraUtcIso());
            }, nameof(GuardarSesion));
        }

        public (PerfilLocal perfil, SesionLocal sesion) ObtenerSesionGuardada()
        {
            var sesion = Connection.QueryFirstOrDefault(
                "SELECT id, perfil_id, refresh_token_cifrado, expira_en, creado_en FROM sesion_local WHERE id = 1",
                row => new SesionLocal
                {
                    Id = row.GetInt32("id"),
                    PerfilId = row.GetString("perfil_id"),
                    RefreshTokenCifrado = row.GetBytes("refresh_token_cifrado"),
                    ExpiraEn = row.GetString("expira_en"),
                    CreadoEn = row.GetString("creado_en")
                });
            if (sesion?.PerfilId == null) return (null, null);

            var perfil = Connection.QueryFirstOrDefault(
                "SELECT id, correo, nombre_completo, sincronizado_en FROM perfil_local WHERE id = ?",
                row => new PerfilLocal
                {
                    Id = row.GetString("id"),
                    Correo = row.GetString("correo"),
                    NombreCompleto = row.GetString("nombre_completo"),
                    SincronizadoEn = row.GetString("sincronizado_en")
                }, sesion.PerfilId);
            return (perfil, sesion);
        }

        public bool LimpiarSesion() =>
            EjecutarTransaccionSegura(() => Connection.Execute("DELETE FROM sesion_local WHERE id = 1"), nameof(LimpiarSesion));
    }
}
