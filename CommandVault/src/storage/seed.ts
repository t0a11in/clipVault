import { DEFAULT_SETTINGS, SCHEMA_VERSION, type PersistedConfig } from '../types/model';

/**
 * Datos de ejemplo para el primer arranque: un caso por tipo.
 *  1. Sin variables.
 *  2. Con variables no sensibles (y una global).
 *  3. Con variables sensibles (no tienen valor guardado, por diseño).
 * IDs fijos para que los tests y las capturas sean reproducibles.
 */
export function createSeed(now: string = new Date().toISOString()): PersistedConfig {
  return {
    schemaVersion: SCHEMA_VERSION,
    tabs: [
      { id: 'tab-general', name: 'General', order: 0 },
      { id: 'tab-frontend', name: 'Frontend', order: 1 },
      { id: 'tab-bd', name: 'Base de datos', order: 2 },
    ],
    entries: [
      {
        id: 'entry-flushdns',
        tabId: 'tab-general',
        order: 0,
        description: 'Limpiar caché DNS de Windows',
        command: 'ipconfig /flushdns',
        variables: [],
        copyCount: 0,
        createdAt: now,
        updatedAt: now,
      },
      {
        id: 'entry-rmdir',
        tabId: 'tab-frontend',
        order: 0,
        description: 'Eliminar node_modules forzando desde CMD',
        command: 'rmdir /s /q "$proyectos\\$proyecto\\frontend\\angular\\node_modules"',
        variables: [{ id: 'var-proyecto', name: 'proyecto', sensitive: false, value: 'SnapGasto' }],
        copyCount: 0,
        createdAt: now,
        updatedAt: now,
      },
      {
        id: 'entry-psql',
        tabId: 'tab-bd',
        order: 0,
        description: 'Conectar a PostgreSQL local',
        command: 'psql "postgresql://$DB_USER:$DB_PASSWORD@localhost:5432/$DB_NAME"',
        variables: [
          { id: 'var-db-user', name: 'DB_USER', sensitive: false, value: 'postgres' },
          { id: 'var-db-password', name: 'DB_PASSWORD', sensitive: true },
          { id: 'var-db-name', name: 'DB_NAME', sensitive: false, value: 'snapgasto' },
        ],
        copyCount: 0,
        createdAt: now,
        updatedAt: now,
      },
    ],
    globalVariables: [
      { id: 'var-global-proyectos', name: 'proyectos', sensitive: false, value: 'D:\\2026\\proyectos' },
    ],
    settings: { ...DEFAULT_SETTINGS },
  };
}
