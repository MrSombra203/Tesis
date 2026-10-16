using GestionInteligente.Persistence.Models;
using System.Collections.Generic;
using System.Linq;

namespace GestionInteligente.Persistence
{
    public class CatalogoRepository : LocalRepository
    {
        public CatalogoRepository(DatabaseConnection db) : base(db) { }

        public List<VehiculoLocal> ObtenerVehiculos() =>
            Connection.Query("SELECT id, nombre, tipo, capacidad, costo_operativo FROM vehiculo_local ORDER BY nombre",
                row => new VehiculoLocal
                {
                    Id = row.GetString("id"),
                    Nombre = row.GetString("nombre"),
                    Tipo = row.GetString("tipo"),
                    Capacidad = row.GetDouble("capacidad"),
                    CostoOperativo = row.GetDouble("costo_operativo")
                });

        public List<RutaLocal> ObtenerRutas() =>
            Connection.Query("SELECT id, origen, destino, distancia_km, ventana_horas FROM ruta_local ORDER BY origen, destino",
                row => MapRuta(row));

        public List<RecursoLocal> ObtenerRecursos() =>
            Connection.Query("SELECT id, nombre, unidad_medida, costo_unitario FROM recurso_local ORDER BY nombre",
                row => new RecursoLocal
                {
                    Id = row.GetString("id"),
                    Nombre = row.GetString("nombre"),
                    UnidadMedida = row.GetString("unidad_medida"),
                    CostoUnitario = row.GetDouble("costo_unitario")
                });

        public VehiculoLocal ObtenerVehiculo(string id) =>
            Connection.QueryFirstOrDefault("SELECT id, nombre, tipo, capacidad, costo_operativo FROM vehiculo_local WHERE id = ?",
                row => new VehiculoLocal
                {
                    Id = row.GetString("id"),
                    Nombre = row.GetString("nombre"),
                    Tipo = row.GetString("tipo"),
                    Capacidad = row.GetDouble("capacidad"),
                    CostoOperativo = row.GetDouble("costo_operativo")
                }, id);

        public RutaLocal ObtenerRuta(string id) =>
            Connection.QueryFirstOrDefault("SELECT id, origen, destino, distancia_km, ventana_horas FROM ruta_local WHERE id = ?",
                row => MapRuta(row), id);

        public EscenarioLocal ObtenerEscenarioActivo() =>
            Connection.QueryFirstOrDefault(
                "SELECT id, nombre, dificultad, rutas_habilitadas, actualizado_en FROM escenario_local ORDER BY actualizado_en DESC LIMIT 1",
                row => new EscenarioLocal
                {
                    Id = row.GetString("id"),
                    Nombre = row.GetString("nombre"),
                    Dificultad = row.GetString("dificultad"),
                    RutasHabilitadas = row.GetString("rutas_habilitadas"),
                    ActualizadoEn = row.GetString("actualizado_en")
                });

        public List<ConfiguracionLocal> ObtenerConfiguraciones(string escenarioId) =>
            Connection.Query(
                "SELECT escenario_id, clave, valor FROM configuracion_local WHERE escenario_id = ? ORDER BY clave",
                row => new ConfiguracionLocal
                {
                    EscenarioId = row.GetString("escenario_id"),
                    Clave = row.GetString("clave"),
                    Valor = row.GetString("valor")
                }, escenarioId);

        public bool GuardarVehiculos(IEnumerable<VehiculoLocal> vehiculos) =>
            EjecutarTransaccionSegura(() =>
            {
                foreach (var vehiculo in vehiculos)
                {
                    Connection.Execute(
                        "INSERT INTO vehiculo_local (id, nombre, tipo, capacidad, costo_operativo) VALUES (?, ?, ?, ?, ?) " +
                        "ON CONFLICT(id) DO UPDATE SET nombre = excluded.nombre, tipo = excluded.tipo, capacidad = excluded.capacidad, costo_operativo = excluded.costo_operativo",
                        vehiculo.Id, vehiculo.Nombre, vehiculo.Tipo, vehiculo.Capacidad, vehiculo.CostoOperativo);
                }
            }, nameof(GuardarVehiculos));

        public bool GuardarRutas(IEnumerable<RutaLocal> rutas) =>
            EjecutarTransaccionSegura(() =>
            {
                foreach (var ruta in rutas)
                {
                    Connection.Execute(
                        "INSERT INTO ruta_local (id, origen, destino, distancia_km, ventana_horas) VALUES (?, ?, ?, ?, ?) " +
                        "ON CONFLICT(id) DO UPDATE SET origen = excluded.origen, destino = excluded.destino, distancia_km = excluded.distancia_km, ventana_horas = excluded.ventana_horas",
                        ruta.Id, ruta.Origen, ruta.Destino, ruta.DistanciaKm, ruta.VentanaHoras);
                }
            }, nameof(GuardarRutas));

        public bool GuardarRecursos(IEnumerable<RecursoLocal> recursos) =>
            EjecutarTransaccionSegura(() =>
            {
                foreach (var recurso in recursos)
                {
                    Connection.Execute(
                        "INSERT INTO recurso_local (id, nombre, unidad_medida, costo_unitario) VALUES (?, ?, ?, ?) " +
                        "ON CONFLICT(id) DO UPDATE SET nombre = excluded.nombre, unidad_medida = excluded.unidad_medida, costo_unitario = excluded.costo_unitario",
                        recurso.Id, recurso.Nombre, recurso.UnidadMedida, recurso.CostoUnitario);
                }
            }, nameof(GuardarRecursos));

        public bool GuardarEscenarioActivo(EscenarioLocal escenario, IEnumerable<(string clave, string valor)> configuraciones) =>
            EjecutarTransaccionSegura(() =>
            {
                Connection.Execute(
                    "INSERT INTO escenario_local (id, nombre, dificultad, rutas_habilitadas, actualizado_en) VALUES (?, ?, ?, ?, ?) " +
                    "ON CONFLICT(id) DO UPDATE SET nombre = excluded.nombre, dificultad = excluded.dificultad, rutas_habilitadas = excluded.rutas_habilitadas, actualizado_en = excluded.actualizado_en",
                    escenario.Id, escenario.Nombre, escenario.Dificultad, escenario.RutasHabilitadas, escenario.ActualizadoEn);

                Connection.Execute("DELETE FROM configuracion_local WHERE escenario_id = ?", escenario.Id);
                foreach (var (clave, valor) in configuraciones)
                {
                    Connection.Execute(
                        "INSERT INTO configuracion_local (escenario_id, clave, valor) VALUES (?, ?, ?)",
                        escenario.Id, clave, valor);
                }
            }, nameof(GuardarEscenarioActivo));

        private static RutaLocal MapRuta(DatabaseConnection.DatabaseRow row) => new RutaLocal
        {
            Id = row.GetString("id"),
            Origen = row.GetString("origen"),
            Destino = row.GetString("destino"),
            DistanciaKm = row.GetDouble("distancia_km"),
            VentanaHoras = row.GetDouble("ventana_horas")
        };
    }
}
