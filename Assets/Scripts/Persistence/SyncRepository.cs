using GestionInteligente.Persistence.Models;
using System;
using System.Collections.Generic;

namespace GestionInteligente.Persistence
{
    public class SyncRepository : LocalRepository
    {
        public SyncRepository(DatabaseConnection db) : base(db) { }

        public bool Encolar(string entidad, string entidadId, string idempotencyKey, string payloadJson)
        {
            Connection.Execute(
                "INSERT INTO cola_sincronizacion (id, entidad, entidad_id, idempotency_key, payload_json, estado) " +
                "VALUES (?, ?, ?, ?, ?, 'pendiente') " +
                "ON CONFLICT(entidad, entidad_id) DO UPDATE SET idempotency_key = excluded.idempotency_key, " +
                "payload_json = excluded.payload_json, estado = 'pendiente', mensaje_error = NULL",
                Guid.NewGuid().ToString(), entidad, entidadId, idempotencyKey, payloadJson);
            return true;
        }

        public List<ColaSincronizacionLocal> ObtenerPendientes(int limite = 50) =>
            Connection.Query(
                "SELECT id, entidad, entidad_id, idempotency_key, payload_json, estado, numero_intentos, " +
                "fecha_ultimo_intento, fecha_creacion, fecha_sincronizacion, mensaje_error " +
                "FROM cola_sincronizacion WHERE estado IN ('pendiente','error') ORDER BY fecha_creacion LIMIT ?",
                row => new ColaSincronizacionLocal
                {
                    Id = row.GetString("id"),
                    Entidad = row.GetString("entidad"),
                    EntidadId = row.GetString("entidad_id"),
                    IdempotencyKey = row.GetString("idempotency_key"),
                    PayloadJson = row.GetString("payload_json"),
                    Estado = row.GetString("estado"),
                    NumeroIntentos = row.GetInt32("numero_intentos"),
                    FechaUltimoIntento = row.GetString("fecha_ultimo_intento"),
                    FechaCreacion = row.GetString("fecha_creacion"),
                    FechaSincronizacion = row.GetString("fecha_sincronizacion"),
                    MensajeError = row.GetString("mensaje_error")
                }, limite);

        public bool RegistrarIntento(string colaId) =>
            EjecutarTransaccionSegura(() => Connection.Execute(
                "UPDATE cola_sincronizacion SET estado = 'en_proceso', numero_intentos = numero_intentos + 1, fecha_ultimo_intento = ? WHERE id = ?",
                AhoraUtcIso(), colaId), nameof(RegistrarIntento));

        public bool MarcarSincronizado(string colaId, string entidad, string entidadId, string resultado, int codigoHttp)
        {
            return EjecutarTransaccionSegura(() =>
            {
                var ahora = AhoraUtcIso();
                Connection.Execute(
                    "UPDATE cola_sincronizacion SET estado = 'sincronizado', fecha_sincronizacion = ?, mensaje_error = NULL WHERE id = ?",
                    ahora, colaId);
                Connection.Execute(
                    "INSERT INTO registro_sincronizacion (entidad, entidad_id, resultado, codigo_http) VALUES (?, ?, ?, ?)",
                    entidad, entidadId, resultado, codigoHttp);
            }, nameof(MarcarSincronizado));
        }

        public bool MarcarError(string colaId, int? codigoHttp, string mensaje)
        {
            return EjecutarTransaccionSegura(() =>
            {
                Connection.Execute(
                    "UPDATE cola_sincronizacion SET estado = 'error', mensaje_error = ? WHERE id = ?",
                    mensaje, colaId);
                Connection.Execute(
                    "INSERT INTO error_sincronizacion (cola_id, codigo_http, mensaje) VALUES (?, ?, ?)",
                    colaId, codigoHttp, mensaje);
            }, nameof(MarcarError));
        }
    }
}
