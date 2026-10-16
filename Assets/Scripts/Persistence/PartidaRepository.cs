using GestionInteligente.Persistence.Models;
using System;
using System.Linq;
using System.Globalization;
using System.Text;

namespace GestionInteligente.Persistence
{
    public class PartidaRepository : LocalRepository
    {
        public PartidaRepository(DatabaseConnection db) : base(db) { }

        public PartidaLocal RegistrarPartida(string nivelId)
        {
            var partidaId = Guid.NewGuid().ToString();
            var partida = new PartidaLocal
            {
                Id = partidaId,
                NivelId = nivelId,
                IdempotencyKey = partidaId,
                Finalizada = 0,
                EstadoSincronizacion = "pendiente",
                VersionRegistro = 1,
                Eliminado = 0
            };
            var guardada = EjecutarTransaccionSegura(() =>
            {
                Connection.Execute(
                    "INSERT INTO partida_local (id, nivel_id, idempotency_key) VALUES (?, ?, ?)",
                    partida.Id, partida.NivelId, partida.IdempotencyKey);
                var registro = ObtenerPartida(partida.Id);
                partida.FechaCreacion = registro.FechaCreacion;
                partida.FechaModificacion = registro.FechaModificacion;
            }, nameof(RegistrarPartida));

            if (!guardada) throw new InvalidOperationException("No se pudo registrar la partida local.");
            return partida;
        }

        public bool GuardarDecision(DecisionLocal decision) =>
            EjecutarTransaccionSegura(() => Connection.Execute(
                "INSERT INTO decision_local (id, partida_id, turno, ruta_id, vehiculo_id, recurso_id, cantidad, costo_calculado, entrega_a_tiempo, payload) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                decision.Id, decision.PartidaId, decision.Turno, decision.RutaId, decision.VehiculoId, decision.RecursoId,
                decision.Cantidad, decision.CostoCalculado, decision.EntregaATiempo, decision.Payload ?? "{}"),
                nameof(GuardarDecision));

        public bool GuardarEvento(EventoLocal evento) =>
            EjecutarTransaccionSegura(() => Connection.Execute(
                "INSERT INTO evento_local (id, partida_id, categoria, tipo, costo, tiempo_dias, estado, detalle) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                evento.Id, evento.PartidaId, evento.Categoria, evento.Tipo, evento.Costo, evento.TiempoDias,
                evento.Estado, evento.Detalle ?? "{}"), nameof(GuardarEvento));

        public PartidaLocal ObtenerPartida(string id) =>
            Connection.QueryFirstOrDefault(
                "SELECT id, nivel_id, idempotency_key, finalizada, estado_sincronizacion, fecha_sincronizacion, " +
                "version_registro, fecha_creacion, fecha_modificacion, eliminado FROM partida_local WHERE id = ?",
                row => new PartidaLocal
                {
                    Id = row.GetString("id"),
                    NivelId = row.GetString("nivel_id"),
                    IdempotencyKey = row.GetString("idempotency_key"),
                    Finalizada = row.GetInt32("finalizada"),
                    EstadoSincronizacion = row.GetString("estado_sincronizacion"),
                    FechaSincronizacion = row.GetString("fecha_sincronizacion"),
                    VersionRegistro = row.GetInt32("version_registro"),
                    FechaCreacion = row.GetString("fecha_creacion"),
                    FechaModificacion = row.GetString("fecha_modificacion"),
                    Eliminado = row.GetInt32("eliminado")
                }, id);

        public bool FinalizarPartida(PartidaLocal partida, KpiLocal kpi, SyncRepository syncRepo, string perfilId)
        {
            if (partida == null) throw new ArgumentNullException(nameof(partida));
            if (kpi == null) throw new ArgumentNullException(nameof(kpi));
            if (syncRepo == null) throw new ArgumentNullException(nameof(syncRepo));

            kpi.Id = Guid.NewGuid().ToString();
            kpi.PartidaId = partida.Id;

            return EjecutarTransaccionSegura(() =>
            {
                Connection.Execute(
                    "UPDATE partida_local SET finalizada = 1, fecha_modificacion = ?, version_registro = version_registro + 1 WHERE id = ? AND eliminado = 0",
                    AhoraUtcIso(), partida.Id);
                Connection.Execute(
                    "INSERT INTO kpi_local (id, partida_id, costo_total, pct_entregas_a_tiempo, utilizacion_recursos, eficiencia_rutas, puntuacion_global) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?)",
                    kpi.Id, kpi.PartidaId, kpi.CostoTotal, kpi.PctEntregasATiempo, kpi.UtilizacionRecursos,
                    kpi.EficienciaRutas, kpi.PuntuacionGlobal);
                Connection.Execute("INSERT INTO resultado_local (id, kpi_id) VALUES (?, ?)", Guid.NewGuid().ToString(), kpi.Id);

                var decisiones = Connection.Query(
                    "SELECT id, partida_id, turno, ruta_id, vehiculo_id, recurso_id, cantidad, costo_calculado, entrega_a_tiempo, payload " +
                    "FROM decision_local WHERE partida_id = ? ORDER BY turno",
                    row => new DecisionSyncPayload
                    {
                        id = row.GetString("id"),
                        turno = row.GetInt32("turno"),
                        rutaId = row.GetString("ruta_id"),
                        vehiculoId = row.GetString("vehiculo_id"),
                        recursoId = row.GetString("recurso_id"),
                        cantidad = row.GetNullableDouble("cantidad"),
                        costoCalculado = row.GetDouble("costo_calculado"),
                        entregaATiempo = row.GetNullableInt32("entrega_a_tiempo"),
                        payload = row.GetString("payload")
                    }, partida.Id).ToArray();
                var eventos = Connection.Query(
                    "SELECT id, categoria, tipo, costo, tiempo_dias, estado, detalle FROM evento_local WHERE partida_id = ? ORDER BY fecha_creacion",
                    row => new EventoSyncPayload
                    {
                        id = row.GetString("id"),
                        categoria = row.GetString("categoria"),
                        tipo = row.GetString("tipo"),
                        costo = row.GetNullableDouble("costo"),
                        tiempoDias = row.GetNullableInt32("tiempo_dias"),
                        estado = row.GetString("estado"),
                        detalle = row.GetString("detalle")
                    }, partida.Id).ToArray();

                var payload = ConstruirPayload(partida, perfilId, decisiones, eventos, kpi);
                if (!syncRepo.Encolar("partida", partida.Id, partida.IdempotencyKey, payload))
                    throw new InvalidOperationException("No se pudo agregar la partida a la cola de sincronización.");
            }, nameof(FinalizarPartida));
        }

        private static string ConstruirPayload(PartidaLocal partida, string perfilId,
            DecisionSyncPayload[] decisiones, EventoSyncPayload[] eventos, KpiLocal kpi)
        {
            var builder = new StringBuilder();
            builder.Append("{\"id\":").Append(SerializarCadena(partida.Id))
                .Append(",\"nivelId\":").Append(SerializarCadena(partida.NivelId))
                .Append(",\"perfilId\":").Append(SerializarCadena(perfilId))
                .Append(",\"idempotencyKey\":").Append(SerializarCadena(partida.IdempotencyKey))
                .Append(",\"decisiones\":[");

            for (var i = 0; i < decisiones.Length; i++)
            {
                if (i > 0) builder.Append(',');
                var decision = decisiones[i];
                builder.Append("{\"id\":").Append(SerializarCadena(decision.id))
                    .Append(",\"turno\":").Append(decision.turno.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"rutaId\":").Append(SerializarCadena(decision.rutaId))
                    .Append(",\"vehiculoId\":").Append(SerializarCadena(decision.vehiculoId))
                    .Append(",\"recursoId\":").Append(SerializarCadena(decision.recursoId))
                    .Append(",\"cantidad\":").Append(SerializarNumero(decision.cantidad))
                    .Append(",\"costoCalculado\":").Append(SerializarNumero(decision.costoCalculado))
                    .Append(",\"entregaATiempo\":").Append(SerializarNumero(decision.entregaATiempo))
                    .Append(",\"payload\":").Append(SerializarCadena(decision.payload)).Append('}');
            }

            builder.Append("],\"eventos\":[");
            for (var i = 0; i < eventos.Length; i++)
            {
                if (i > 0) builder.Append(',');
                var evento = eventos[i];
                builder.Append("{\"id\":").Append(SerializarCadena(evento.id))
                    .Append(",\"categoria\":").Append(SerializarCadena(evento.categoria))
                    .Append(",\"tipo\":").Append(SerializarCadena(evento.tipo))
                    .Append(",\"costo\":").Append(SerializarNumero(evento.costo))
                    .Append(",\"tiempoDias\":").Append(SerializarNumero(evento.tiempoDias))
                    .Append(",\"estado\":").Append(SerializarCadena(evento.estado))
                    .Append(",\"detalle\":").Append(SerializarCadena(evento.detalle)).Append('}');
            }

            builder.Append("],\"kpi\":{\"id\":").Append(SerializarCadena(kpi.Id))
                .Append(",\"costoTotal\":").Append(SerializarNumero(kpi.CostoTotal))
                .Append(",\"pctEntregasATiempo\":").Append(SerializarNumero(kpi.PctEntregasATiempo))
                .Append(",\"utilizacionRecursos\":").Append(SerializarNumero(kpi.UtilizacionRecursos))
                .Append(",\"eficienciaRutas\":").Append(SerializarNumero(kpi.EficienciaRutas))
                .Append(",\"puntuacionGlobal\":").Append(SerializarNumero(kpi.PuntuacionGlobal))
                .Append("}}");
            return builder.ToString();
        }

        private static string SerializarCadena(string valor)
        {
            if (valor == null) return "null";
            var builder = new StringBuilder(valor.Length + 2).Append('"');
            foreach (var caracter in valor)
            {
                switch (caracter)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (caracter < 0x20)
                            builder.Append("\\u").Append(((int)caracter).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(caracter);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }

        private static string SerializarNumero(double? valor) =>
            valor.HasValue ? valor.Value.ToString("R", CultureInfo.InvariantCulture) : "null";

        private static string SerializarNumero(int? valor) =>
            valor.HasValue ? valor.Value.ToString(CultureInfo.InvariantCulture) : "null";

        private static string SerializarNumero(double valor) =>
            valor.ToString("R", CultureInfo.InvariantCulture);

        private sealed class DecisionSyncPayload
        {
            public string id;
            public int turno;
            public string rutaId;
            public string vehiculoId;
            public string recursoId;
            public double? cantidad;
            public double costoCalculado;
            public int? entregaATiempo;
            public string payload;
        }

        private sealed class EventoSyncPayload
        {
            public string id;
            public string categoria;
            public string tipo;
            public double? costo;
            public int? tiempoDias;
            public string estado;
            public string detalle;
        }
    }
}
