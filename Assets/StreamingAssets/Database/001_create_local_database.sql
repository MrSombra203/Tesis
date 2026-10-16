CREATE TABLE version_esquema (
    version INTEGER PRIMARY KEY,
    descripcion TEXT NOT NULL,
    aplicado_en TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE TABLE perfil_local (
    id TEXT PRIMARY KEY,
    correo TEXT NOT NULL,
    nombre_completo TEXT NOT NULL,
    sincronizado_en TEXT
);

CREATE TABLE sesion_local (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    perfil_id TEXT REFERENCES perfil_local(id),
    refresh_token_cifrado BLOB,
    expira_en TEXT,
    creado_en TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE TABLE vehiculo_local (
    id TEXT PRIMARY KEY,
    nombre TEXT NOT NULL,
    tipo TEXT NOT NULL CHECK (tipo IN ('terrestre','maritimo','aereo')),
    capacidad REAL NOT NULL,
    costo_operativo REAL NOT NULL
);

CREATE TABLE ruta_local (
    id TEXT PRIMARY KEY,
    origen TEXT NOT NULL,
    destino TEXT NOT NULL,
    distancia_km REAL NOT NULL,
    ventana_horas REAL NOT NULL
);

CREATE TABLE recurso_local (
    id TEXT PRIMARY KEY,
    nombre TEXT NOT NULL,
    unidad_medida TEXT NOT NULL,
    costo_unitario REAL NOT NULL
);

CREATE TABLE escenario_local (
    id TEXT PRIMARY KEY,
    nombre TEXT NOT NULL,
    dificultad TEXT NOT NULL CHECK (dificultad IN ('facil','intermedio','dificil','desafio')),
    rutas_habilitadas TEXT NOT NULL DEFAULT '[]',
    actualizado_en TEXT NOT NULL
);

CREATE TABLE configuracion_local (
    escenario_id TEXT NOT NULL REFERENCES escenario_local(id) ON DELETE CASCADE,
    clave TEXT NOT NULL,
    valor TEXT NOT NULL,
    PRIMARY KEY (escenario_id, clave)
);

INSERT INTO version_esquema (version, descripcion) VALUES (1, 'Tablas locales de perfil, sesión y catálogos');
