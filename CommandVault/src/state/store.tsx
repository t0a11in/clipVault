import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from 'react';
import {
  HISTORY_LIMIT,
  type Entry,
  type HistoryItem,
  type PersistedConfig,
  type Settings,
  type Tab,
  type Variable,
} from '../types/model';
import { getBrowserStore, loadState, saveConfig, saveHistory } from '../storage/storage';
import { normalizeOrder } from '../storage/validate';
import { stripSensitive } from '../domain/sanitize';
import { nowIso } from '../domain/ids';

/** Estado de datos de la app: lo que se persiste. El estado de UI vive en App. */
export interface DataState {
  config: PersistedConfig;
  history: HistoryItem[];
}

export type Action =
  | { type: 'tab/add'; tab: Tab }
  | { type: 'tab/rename'; id: string; name: string }
  | { type: 'tab/delete'; id: string }
  /** Mueve `id` a la posición que ocupa `targetId` (antes o después de él). */
  | { type: 'tab/move'; id: string; targetId: string; place: 'before' | 'after' }
  | { type: 'entry/add'; entry: Entry }
  | { type: 'entry/update'; id: string; patch: Partial<Omit<Entry, 'id'>> }
  | { type: 'entry/delete'; id: string }
  | { type: 'settings/update'; patch: Partial<Settings> }
  | { type: 'globals/set'; variables: Variable[] }
  | { type: 'copy/record'; item: HistoryItem }
  | { type: 'history/clear' }
  | { type: 'data/replace'; config: PersistedConfig; history: HistoryItem[] | null };

/** Reductor puro: toda mutación de datos pasa por aquí (fácil de testear y razonar). */
export function reducer(state: DataState, action: Action): DataState {
  const { config } = state;
  const withConfig = (patch: Partial<PersistedConfig>): DataState => ({
    ...state,
    config: { ...config, ...patch },
  });

  switch (action.type) {
    case 'tab/add':
      return withConfig({ tabs: [...config.tabs, { ...action.tab, order: config.tabs.length }] });

    case 'tab/rename':
      return withConfig({
        tabs: config.tabs.map((t) => (t.id === action.id ? { ...t, name: action.name } : t)),
      });

    case 'tab/delete':
      // Eliminar un tab elimina sus entradas; el historial se conserva (tiene copia de los nombres).
      return withConfig({
        tabs: normalizeOrder(config.tabs.filter((t) => t.id !== action.id)),
        entries: config.entries.filter((e) => e.tabId !== action.id),
      });

    case 'tab/move': {
      if (action.id === action.targetId) return state;
      const sorted = normalizeOrder(config.tabs);
      const moving = sorted.find((t) => t.id === action.id);
      if (!moving) return state;
      const rest = sorted.filter((t) => t.id !== action.id);
      const targetIndex = rest.findIndex((t) => t.id === action.targetId);
      if (targetIndex < 0) return state;
      rest.splice(action.place === 'before' ? targetIndex : targetIndex + 1, 0, moving);
      return withConfig({ tabs: rest.map((t, i) => ({ ...t, order: i })) });
    }

    case 'entry/add': {
      const siblings = config.entries.filter((e) => e.tabId === action.entry.tabId);
      return withConfig({ entries: [...config.entries, { ...action.entry, order: siblings.length }] });
    }

    case 'entry/update':
      return withConfig({
        entries: config.entries.map((e) => {
          if (e.id !== action.id) return e;
          const next = { ...e, ...action.patch, updatedAt: nowIso() };
          // Si cambia de tab, va al final del tab destino.
          if (action.patch.tabId && action.patch.tabId !== e.tabId) {
            next.order = config.entries.filter((x) => x.tabId === action.patch.tabId).length;
          }
          // Nunca dejar en memoria un valor de una variable marcada sensible.
          next.variables = next.variables.map(stripSensitive);
          return next;
        }),
      });

    case 'entry/delete':
      return withConfig({ entries: config.entries.filter((e) => e.id !== action.id) });

    case 'settings/update':
      return withConfig({ settings: { ...config.settings, ...action.patch } });

    case 'globals/set':
      return withConfig({ globalVariables: action.variables.map(stripSensitive) });

    case 'copy/record':
      return {
        config: {
          ...config,
          entries: config.entries.map((e) =>
            e.id === action.item.entryId
              ? { ...e, copyCount: e.copyCount + 1, lastCopiedAt: action.item.at }
              : e,
          ),
        },
        history: [action.item, ...state.history].slice(0, HISTORY_LIMIT),
      };

    case 'history/clear':
      return { ...state, history: [] };

    case 'data/replace':
      return { config: action.config, history: action.history ?? state.history };
  }
}

interface StoreValue extends DataState {
  dispatch: (action: Action) => void;
  /** Tabs ordenados por `order`. */
  sortedTabs: Tab[];
  /** Avisos de carga o de guardado para mostrar al usuario. */
  notices: string[];
  dismissNotice: (index: number) => void;
  /** Secretos introducidos en esta sesión. Solo memoria: se pierden al cerrar. */
  sessionSecrets: RefObject<Record<string, string>>;
}

const StoreContext = createContext<StoreValue | null>(null);

export function StoreProvider({ children }: { children: ReactNode }) {
  // La carga se hace una sola vez (inicializador perezoso).
  const [boot] = useState(() => {
    const store = getBrowserStore();
    return { store, ...loadState(store) };
  });
  const [state, dispatch] = useReducer(reducer, { config: boot.config, history: boot.history });
  const [notices, setNotices] = useState<string[]>(boot.warnings);
  const sessionSecrets = useRef<Record<string, string>>({});

  const pushNotice = useCallback((msg: string | null) => {
    if (msg) setNotices((n) => (n.includes(msg) ? n : [...n, msg]));
  }, []);

  // Guardado con un pequeño retardo para agrupar cambios rápidos (p. ej. escribir).
  useEffect(() => {
    const handle = window.setTimeout(() => pushNotice(saveConfig(boot.store, state.config)), 200);
    return () => window.clearTimeout(handle);
  }, [state.config, boot.store, pushNotice]);

  useEffect(() => {
    const handle = window.setTimeout(() => pushNotice(saveHistory(boot.store, state.history)), 200);
    return () => window.clearTimeout(handle);
  }, [state.history, boot.store, pushNotice]);

  const sortedTabs = useMemo(() => normalizeOrder(state.config.tabs), [state.config.tabs]);

  const value = useMemo<StoreValue>(
    () => ({
      ...state,
      dispatch,
      sortedTabs,
      notices,
      dismissNotice: (index) => setNotices((n) => n.filter((_, i) => i !== index)),
      sessionSecrets,
    }),
    [state, sortedTabs, notices],
  );

  return <StoreContext.Provider value={value}>{children}</StoreContext.Provider>;
}

export function useStore(): StoreValue {
  const ctx = useContext(StoreContext);
  if (!ctx) throw new Error('useStore debe usarse dentro de <StoreProvider>.');
  return ctx;
}

/** Entradas de un tab ordenadas. */
export function entriesOfTab(entries: Entry[], tabId: string): Entry[] {
  return entries.filter((e) => e.tabId === tabId).sort((a, b) => a.order - b.order);
}
