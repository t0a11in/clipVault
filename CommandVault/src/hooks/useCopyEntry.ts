import { useCallback } from 'react';
import type { Entry, HistoryItem } from '../types/model';
import { formatForCopy, MASK, resolveCommand } from '../domain/resolveCommand';
import { newId, nowIso } from '../domain/ids';
import { useStore } from '../state/store';
import { copyToClipboard } from './useClipboard';

export interface CopyOutcome {
  ok: boolean;
  /** Variables que quedaron como $nombre en el texto copiado. */
  pending: string[];
  /** Si faltan secretos y el ajuste pide resolverlos: nombres a solicitar. */
  needsSecrets?: string[];
  historyId?: string;
}

/**
 * Caso de uso "copiar una entrada":
 *  1. Resuelve variables (entrada → globales; sensibles según ajuste).
 *  2. Aplica el modo de copia (con o sin comentario).
 *  3. Escribe en el portapapeles.
 *  4. Registra la copia en el historial (sin secretos) y suma el contador.
 */
export function useCopyEntry() {
  const { config, dispatch, sessionSecrets } = useStore();
  const { settings, globalVariables, tabs } = config;

  return useCallback(
    async (entry: Entry, options: { skipSecretPrompt?: boolean } = {}): Promise<CopyOutcome> => {
      const result = resolveCommand(entry.command, {
        variables: entry.variables,
        globals: globalVariables,
        resolveSensitive: settings.resolveSensitiveOnCopy,
        sessionSecrets: sessionSecrets.current,
      });

      // Con el ajuste activo, si faltan secretos se piden antes de copiar.
      if (settings.resolveSensitiveOnCopy && result.sensitivePending.length > 0 && !options.skipSecretPrompt) {
        return { ok: false, pending: [], needsSecrets: result.sensitivePending };
      }

      const fmt = (text: string) => formatForCopy(text, entry.description, settings.copyMode, settings.commentStyle);
      const ok = await copyToClipboard(fmt(result.text));
      if (!ok) return { ok: false, pending: [] };

      // Instantánea de variables usadas, con sensibles enmascaradas.
      const used = new Set([...result.sensitive, ...result.unresolved]);
      const lookup = new Map([...globalVariables, ...entry.variables].map((v) => [v.name, v]));
      for (const match of entry.command.matchAll(/\$([A-Za-z_][A-Za-z0-9_]*)/g)) {
        if (match[1] && lookup.has(match[1])) used.add(match[1]);
      }
      const item: HistoryItem = {
        id: newId(),
        at: nowIso(),
        entryId: entry.id,
        tabId: entry.tabId,
        entryDescription: entry.description,
        tabName: tabs.find((t) => t.id === entry.tabId)?.name ?? '',
        copiedText: fmt(result.masked),
        replayText: fmt(result.replay),
        variables: [...used].flatMap((name) => {
          const v = lookup.get(name);
          if (!v) return [];
          return [{ name, sensitive: v.sensitive, value: v.sensitive ? MASK : (v.value ?? '') }];
        }),
      };
      dispatch({ type: 'copy/record', item });
      return { ok: true, pending: [...result.sensitivePending, ...result.unresolved], historyId: item.id };
    },
    [dispatch, globalVariables, sessionSecrets, settings, tabs],
  );
}

/** Copia de nuevo un registro del historial (las sensibles vuelven como $nombre). */
export function useReplayHistory() {
  const { dispatch } = useStore();
  return useCallback(
    async (item: HistoryItem): Promise<{ ok: boolean; historyId?: string }> => {
      const ok = await copyToClipboard(item.replayText);
      if (!ok) return { ok };
      // Se registra como una copia más; si la entrada ya no existe, el reductor
      // simplemente no encuentra a quién sumarle el contador.
      const replay: HistoryItem = { ...item, id: newId(), at: nowIso() };
      dispatch({ type: 'copy/record', item: replay });
      return { ok, historyId: replay.id };
    },
    [dispatch],
  );
}
