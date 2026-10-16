import { describe, expect, it } from 'vitest';
import { migrate, type MigrationMap } from './migrations';
import { buildExport, parseImport } from './exportImport';
import { loadState, saveConfig, STORAGE_KEYS, type KeyValueStore } from './storage';
import { createSeed } from './seed';
import { DataError } from './validate';
import { topFrequent } from '../domain/frequent';
import { searchEntries } from '../domain/search';
import { SCHEMA_VERSION, type Entry } from '../types/model';

/** Almacén en memoria con la misma interfaz que localStorage. */
function memoryStore(initial: Record<string, string> = {}): KeyValueStore & { data: Map<string, string> } {
  const data = new Map(Object.entries(initial));
  return {
    data,
    getItem: (k) => data.get(k) ?? null,
    setItem: (k, val) => void data.set(k, val),
  };
}

describe('migrate', () => {
  it('aplica los pasos en orden hasta la versión objetivo', () => {
    const steps: MigrationMap = {
      1: (d) => ({ ...d, a: 'v2' }),
      2: (d) => ({ ...d, b: `${String(d.a)}+v3` }),
    };
    expect(migrate({ schemaVersion: 1 }, steps, 3)).toEqual({ schemaVersion: 3, a: 'v2', b: 'v2+v3' });
  });

  it('rechaza datos de una versión más nueva', () => {
    expect(() => migrate({ schemaVersion: 99 })).toThrow(DataError);
  });

  it('v0 → v1 aplana las entradas anidadas en los tabs', () => {
    const v0 = { tabs: [{ id: 't1', name: 'Docker', entries: [{ id: 'e1', command: 'docker ps' }] }] };
    const v1 = migrate(v0);
    expect(v1.schemaVersion).toBe(1);
    expect(v1.entries).toEqual([{ order: 0, id: 'e1', command: 'docker ps', tabId: 't1' }]);
    expect((v1.tabs as object[])[0]).toEqual({ order: 0, id: 't1', name: 'Docker' });
  });
});

describe('persistencia', () => {
  it('primer arranque: carga los ejemplos', () => {
    const r = loadState(memoryStore());
    expect(r.seeded).toBe(true);
    expect(r.config.entries).toHaveLength(3);
  });

  it('nunca guarda valores sensibles aunque lleguen en memoria', () => {
    const store = memoryStore();
    const config = createSeed();
    config.entries[2]!.variables[1]!.value = 'filtrado';
    saveConfig(store, config);
    expect(store.data.get(STORAGE_KEYS.config)).not.toContain('filtrado');
  });

  it('ida y vuelta: lo guardado se vuelve a cargar igual', () => {
    const store = memoryStore();
    const config = createSeed('2026-01-01T00:00:00.000Z');
    saveConfig(store, config);
    expect(loadState(store).config).toEqual(config);
  });

  it('datos corruptos: respalda y arranca con ejemplos sin lanzar', () => {
    const store = memoryStore({ [STORAGE_KEYS.config]: '{no es json' });
    const r = loadState(store);
    expect(r.seeded).toBe(true);
    expect(r.warnings[0]).toContain('respaldo');
    const backups = [...store.data.keys()].filter((k) => k.startsWith(STORAGE_KEYS.corruptPrefix));
    expect(backups).toHaveLength(1);
  });

  it('descarta entradas que apuntan a un tab inexistente', () => {
    const config = createSeed();
    const raw = { ...config, entries: [...config.entries, { ...config.entries[0], id: 'x', tabId: 'nope' }] };
    const r = loadState(memoryStore({ [STORAGE_KEYS.config]: JSON.stringify(raw) }));
    expect(r.config.entries).toHaveLength(3);
    expect(r.warnings.length).toBeGreaterThan(0);
  });
});

describe('exportar / importar', () => {
  it('exporta sin valores sensibles pero conserva nombre y flag', () => {
    const config = createSeed();
    config.entries[2]!.variables[1]!.value = 'secreto';
    const file = buildExport(config, null);
    const json = JSON.stringify(file);
    expect(json).not.toContain('secreto');
    const pwd = file.config.entries[2]!.variables[1]!;
    expect(pwd).toMatchObject({ name: 'DB_PASSWORD', sensitive: true });
    expect('value' in pwd).toBe(false);
    expect(file.history).toBeUndefined();
  });

  it('importa lo exportado (ida y vuelta)', () => {
    const config = createSeed('2026-01-01T00:00:00.000Z');
    const r = parseImport(JSON.stringify(buildExport(config, [])));
    expect(r.config).toEqual(config);
    expect(r.history).toEqual([]);
    expect(r.config.schemaVersion).toBe(SCHEMA_VERSION);
  });

  it('ignora valores sensibles en un archivo manipulado', () => {
    const config = createSeed();
    const file = buildExport(config, null);
    (file.config.entries[2]!.variables[1] as { value?: string }).value = 'inyectado';
    const r = parseImport(JSON.stringify(file));
    expect(JSON.stringify(r.config)).not.toContain('inyectado');
  });

  it('rechaza archivos que no son de CommandVault', () => {
    expect(() => parseImport('[1,2,3]')).toThrow(DataError);
    expect(() => parseImport('{"foo": 1}')).toThrow(DataError);
    expect(() => parseImport('nada')).toThrow(DataError);
  });
});

describe('frecuentes y búsqueda', () => {
  const base = createSeed().entries;
  const withCounts: Entry[] = base.map((e, i) => ({ ...e, copyCount: [5, 0, 9][i] ?? 0 }));

  it('ordena por número de copias y omite las nunca copiadas', () => {
    expect(topFrequent(withCounts).map((e) => e.id)).toEqual(['entry-psql', 'entry-flushdns']);
  });

  it('busca sin tildes en descripción y comando', () => {
    expect(searchEntries(base, 'cache dns').map((e) => e.id)).toEqual(['entry-flushdns']);
    expect(searchEntries(base, 'psql').map((e) => e.id)).toEqual(['entry-psql']);
    expect(searchEntries(base, '   ')).toEqual([]);
  });
});
