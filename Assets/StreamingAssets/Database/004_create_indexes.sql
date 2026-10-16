CREATE INDEX idx_partida_local_fecha ON partida_local(fecha_creacion DESC);
CREATE INDEX idx_cola_sync_fecha ON cola_sincronizacion(fecha_creacion) WHERE estado = 'pendiente';

INSERT INTO version_esquema (version, descripcion) VALUES (4, 'Índices de consulta y sincronización');
