CREATE TABLE cola_sincronizacion (
    id TEXT PRIMARY KEY,
    entidad TEXT NOT NULL CHECK (entidad IN ('partida','decision','evento','kpi')),
    entidad_id TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    estado TEXT NOT NULL DEFAULT 'pendiente'
        CHECK (estado IN ('pendiente','en_proceso','sincronizado','error','conflicto')),
    numero_intentos INTEGER NOT NULL DEFAULT 0,
    fecha_ultimo_intento TEXT,
    fecha_creacion TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    fecha_sincronizacion TEXT,
    mensaje_error TEXT,
    UNIQUE (entidad, entidad_id)
);
CREATE INDEX idx_cola_sync_estado ON cola_sincronizacion(estado);

CREATE TABLE registro_sincronizacion (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entidad TEXT NOT NULL,
    entidad_id TEXT NOT NULL,
    resultado TEXT NOT NULL CHECK (resultado IN ('creado','ya_existia')),
    codigo_http INTEGER,
    ocurrido_en TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE TABLE error_sincronizacion (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cola_id TEXT REFERENCES cola_sincronizacion(id),
    codigo_http INTEGER,
    mensaje TEXT NOT NULL,
    ocurrido_en TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

INSERT INTO version_esquema (version, descripcion) VALUES (3, 'Cola y registros de sincronización');
