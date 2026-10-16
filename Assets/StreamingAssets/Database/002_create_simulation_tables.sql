CREATE TABLE partida_local (
    id TEXT PRIMARY KEY,
    nivel_id TEXT NOT NULL,
    idempotency_key TEXT NOT NULL UNIQUE,
    finalizada INTEGER NOT NULL DEFAULT 0 CHECK (finalizada IN (0,1)),
    estado_sincronizacion TEXT NOT NULL DEFAULT 'pendiente'
        CHECK (estado_sincronizacion IN ('pendiente','en_proceso','sincronizado','error','conflicto')),
    fecha_sincronizacion TEXT,
    version_registro INTEGER NOT NULL DEFAULT 1,
    fecha_creacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    fecha_modificacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    eliminado INTEGER NOT NULL DEFAULT 0 CHECK (eliminado IN (0,1))
);
CREATE INDEX idx_partida_local_estado ON partida_local(estado_sincronizacion) WHERE eliminado = 0;

CREATE TABLE decision_local (
    id TEXT PRIMARY KEY,
    partida_id TEXT NOT NULL REFERENCES partida_local(id) ON DELETE RESTRICT,
    turno INTEGER NOT NULL CHECK (turno > 0),
    ruta_id TEXT REFERENCES ruta_local(id),
    vehiculo_id TEXT REFERENCES vehiculo_local(id),
    recurso_id TEXT REFERENCES recurso_local(id),
    cantidad REAL,
    costo_calculado REAL NOT NULL DEFAULT 0,
    entrega_a_tiempo INTEGER CHECK (entrega_a_tiempo IN (0,1)),
    payload TEXT NOT NULL DEFAULT '{}',
    fecha_creacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    UNIQUE (partida_id, turno)
);
CREATE INDEX idx_decision_local_partida ON decision_local(partida_id);

CREATE TABLE evento_local (
    id TEXT PRIMARY KEY,
    partida_id TEXT NOT NULL REFERENCES partida_local(id) ON DELETE RESTRICT,
    categoria TEXT NOT NULL CHECK (categoria IN ('logistico','aduanero')),
    tipo TEXT NOT NULL,
    costo REAL,
    tiempo_dias INTEGER,
    estado TEXT,
    detalle TEXT NOT NULL DEFAULT '{}',
    fecha_creacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE INDEX idx_evento_local_partida ON evento_local(partida_id);

CREATE TABLE kpi_local (
    id TEXT PRIMARY KEY,
    partida_id TEXT NOT NULL UNIQUE REFERENCES partida_local(id) ON DELETE RESTRICT,
    costo_total REAL NOT NULL CHECK (costo_total >= 0),
    pct_entregas_a_tiempo REAL NOT NULL CHECK (pct_entregas_a_tiempo BETWEEN 0 AND 100),
    utilizacion_recursos REAL NOT NULL CHECK (utilizacion_recursos BETWEEN 0 AND 100),
    eficiencia_rutas REAL NOT NULL CHECK (eficiencia_rutas BETWEEN 0 AND 100),
    puntuacion_global REAL NOT NULL,
    fecha_creacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE TABLE resultado_local (
    id TEXT PRIMARY KEY,
    kpi_id TEXT NOT NULL UNIQUE REFERENCES kpi_local(id) ON DELETE RESTRICT,
    generado_en TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

INSERT INTO version_esquema (version, descripcion) VALUES (2, 'Tablas de partidas, decisiones, eventos, KPI y resultados');
