BEGIN TRANSACTION;

INSERT INTO perfil_local (id, correo, nombre_completo, sincronizado_en) VALUES
    ('30000000-0000-0000-0000-000000000003', 'estudiante3.prueba@udla.edu.ec', 'Estudiante Tres de Prueba',
     strftime('%Y-%m-%dT%H:%M:%fZ','now'));

INSERT INTO sesion_local (id, perfil_id, refresh_token_cifrado, expira_en) VALUES
    (1, '30000000-0000-0000-0000-000000000003', NULL, NULL);

INSERT INTO vehiculo_local (id, nombre, tipo, capacidad, costo_operativo) VALUES
    ('60000000-0000-0000-0000-000000000001', 'Camión 20T', 'terrestre', 20, 150.00),
    ('60000000-0000-0000-0000-000000000003', 'Carga Aérea', 'aereo', 10, 3000.00);

INSERT INTO recurso_local (id, nombre, unidad_medida, costo_unitario) VALUES
    ('61000000-0000-0000-0000-000000000001', 'Contenedor estándar', 'unidad', 25.00);

INSERT INTO ruta_local (id, origen, destino, distancia_km, ventana_horas) VALUES
    ('62000000-0000-0000-0000-000000000001', 'Guayaquil', 'Quito', 420, 12);

INSERT INTO escenario_local (id, nombre, dificultad, rutas_habilitadas, actualizado_en) VALUES
    ('70000000-0000-0000-0000-000000000001', 'Escenario Nacional - Prueba', 'facil',
     '["62000000-0000-0000-0000-000000000001"]', strftime('%Y-%m-%dT%H:%M:%fZ','now'));

INSERT INTO configuracion_local (escenario_id, clave, valor) VALUES
    ('70000000-0000-0000-0000-000000000001', 'eventosHabilitados', 'true');

INSERT INTO partida_local (id, nivel_id, idempotency_key, finalizada, estado_sincronizacion, fecha_sincronizacion, fecha_creacion, fecha_modificacion) VALUES
    ('90000000-0000-0000-0000-000000000004', '80000000-0000-0000-0000-000000000001',
     '90000000-0000-0000-0000-000000000004', 1, 'sincronizado',
     strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days','+3 minutes'),
     strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days'), strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days')),
    ('90000000-0000-0000-0000-000000000005', '80000000-0000-0000-0000-000000000003',
     '90000000-0000-0000-0000-000000000005', 1, 'pendiente', NULL,
     strftime('%Y-%m-%dT%H:%M:%fZ','now','-1 days'), strftime('%Y-%m-%dT%H:%M:%fZ','now','-1 days'));

INSERT INTO decision_local (id, partida_id, turno, ruta_id, vehiculo_id, recurso_id, cantidad, costo_calculado, entrega_a_tiempo, fecha_creacion) VALUES
    ('a0000000-0000-0000-0000-000000000005', '90000000-0000-0000-0000-000000000004', 1,
     '62000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000001',
     '61000000-0000-0000-0000-000000000001', 8, 1200.00, 1, strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days'));

INSERT INTO kpi_local (id, partida_id, costo_total, pct_entregas_a_tiempo, utilizacion_recursos, eficiencia_rutas, puntuacion_global, fecha_creacion) VALUES
    ('d0000000-0000-0000-0000-000000000004', '90000000-0000-0000-0000-000000000004', 1600.00, 95.00, 79.00, 88.00, 90.40, strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days')),
    ('d0000000-0000-0000-0000-000000000005', '90000000-0000-0000-0000-000000000005', 8200.00, 70.00, 65.00, 60.00, 65.20, strftime('%Y-%m-%dT%H:%M:%fZ','now','-1 days'));

INSERT INTO resultado_local (id, kpi_id, generado_en) VALUES
    ('e0000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000004', strftime('%Y-%m-%dT%H:%M:%fZ','now','-2 days')),
    ('e0000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000005', strftime('%Y-%m-%dT%H:%M:%fZ','now','-1 days'));

INSERT INTO cola_sincronizacion (id, entidad, entidad_id, idempotency_key, payload_json, estado, numero_intentos) VALUES
    ('f0000000-0000-0000-0000-000000000001', 'partida', '90000000-0000-0000-0000-000000000005',
     '90000000-0000-0000-0000-000000000005',
     '{"id":"90000000-0000-0000-0000-000000000005","kpi":{"costoTotal":8200.00,"puntuacionGlobal":65.20}}',
     'pendiente', 0);

INSERT INTO registro_sincronizacion (entidad, entidad_id, resultado, codigo_http) VALUES
    ('partida', '90000000-0000-0000-0000-000000000004', 'creado', 201);

COMMIT;
