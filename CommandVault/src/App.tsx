import { useCallback, useMemo, useState } from 'react';
import type { Entry, HistoryItem } from './types/model';
import { entriesOfTab, StoreProvider, useStore } from './state/store';
import { topFrequent } from './domain/frequent';
import { newId, nowIso } from './domain/ids';
import { useKeybindings } from './hooks/useKeybindings';
import { useCopyEntry, useReplayHistory } from './hooks/useCopyEntry';
import { useTheme } from './hooks/useTheme';
import { Sidebar, type View } from './components/Sidebar';
import { TabPanel } from './components/TabPanel';
import { FrequentTab } from './components/FrequentTab';
import { SearchOverlay } from './components/SearchOverlay';
import { SettingsModal } from './components/SettingsModal';
import { SecretPrompt } from './components/SecretPrompt';
import { Toast, type ToastState } from './components/Toast';

export default function App() {
  return (
    <StoreProvider>
      <Shell />
    </StoreProvider>
  );
}

/** Orquesta la UI: vista activa, selección, edición, diálogos y atajos. */
function Shell() {
  const { config, history, dispatch, sortedTabs, notices, dismissNotice, sessionSecrets } = useStore();
  const { settings, entries, globalVariables } = config;
  useTheme(settings.theme);

  const [view, setView] = useState<View>(() =>
    history.length === 0 && sortedTabs[0] ? { type: 'tab', tabId: sortedTabs[0].id } : { type: 'frequent' },
  );
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [newEntryId, setNewEntryId] = useState<string | null>(null);
  const [renamingTabId, setRenamingTabId] = useState<string | null>(null);
  const [searchOpen, setSearchOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [inspectedId, setInspectedId] = useState<string | null>(null);
  const [secretPrompt, setSecretPrompt] = useState<{ entry: Entry; names: string[] } | null>(null);
  const [toast, setToast] = useState<ToastState | null>(null);

  const copyEntry = useCopyEntry();
  const replayHistory = useReplayHistory();

  const notify = useCallback((message: string, tone: ToastState['tone'] = 'ok', detail?: string) => {
    setToast({ id: Date.now(), message, tone, ...(detail ? { detail } : {}) });
  }, []);
  const closeToast = useCallback(() => setToast(null), []);

  // Si el tab activo desaparece (eliminado o import), volver a Frecuentes.
  const activeTab = view.type === 'tab' ? sortedTabs.find((t) => t.id === view.tabId) : undefined;
  const effectiveView: View = view.type === 'tab' && !activeTab ? { type: 'frequent' } : view;

  const tabEntries = useMemo(
    () => (activeTab ? entriesOfTab(entries, activeTab.id) : []),
    [entries, activeTab],
  );
  /** Lista que recorren ↑↓ en la vista actual. */
  const navList = effectiveView.type === 'frequent' ? topFrequent(entries) : tabEntries;

  /** Al salir de una entrada nueva sin contenido, se descarta. */
  const discardEmptyNew = useCallback(() => {
    if (!newEntryId) return;
    const e = entries.find((x) => x.id === newEntryId);
    if (e && !e.command.trim() && !e.description.trim()) dispatch({ type: 'entry/delete', id: e.id });
    setNewEntryId(null);
  }, [dispatch, entries, newEntryId]);

  const selectView = useCallback(
    (next: View) => {
      discardEmptyNew();
      setView(next);
      setSelectedId(null);
      setEditingId(null);
      setInspectedId(null);
    },
    [discardEmptyNew],
  );

  const doCopy = useCallback(
    async (entry: Entry, opts: { silent?: boolean; skipSecretPrompt?: boolean } = {}) => {
      const outcome = await copyEntry(entry, opts.skipSecretPrompt ? { skipSecretPrompt: true } : {});
      if (outcome.needsSecrets) {
        setSecretPrompt({ entry, names: outcome.needsSecrets });
        return;
      }
      if (!outcome.ok) {
        notify('No se pudo copiar', 'error', 'El navegador bloqueó el acceso al portapapeles.');
        return;
      }
      if (opts.silent) {
        setToast(null);
        return;
      }
      const pending = outcome.pending.map((n) => `$${n}`).join(', ');
      notify(
        `Copiado: ${entry.description || 'comando'}`,
        pending ? 'warn' : 'ok',
        pending ? `Rellena a mano: ${pending}` : undefined,
      );
    },
    [copyEntry, notify],
  );

  const replay = useCallback(
    async (item: HistoryItem) => {
      const res = await replayHistory(item);
      if (!res.ok) return notify('No se pudo copiar', 'error');
      setInspectedId(res.historyId ?? null);
      const pending = item.variables.filter((v) => v.sensitive).map((v) => `$${v.name}`);
      notify(
        `Copiado de nuevo: ${item.entryDescription || 'comando'}`,
        pending.length ? 'warn' : 'ok',
        pending.length ? `Rellena a mano: ${pending.join(', ')}` : undefined,
      );
    },
    [notify, replayHistory],
  );

  const newEntry = useCallback(() => {
    if (!activeTab) {
      notify('Abre un tab para crear una entrada', 'warn');
      return;
    }
    discardEmptyNew();
    const now = nowIso();
    const entry: Entry = {
      id: newId(),
      tabId: activeTab.id,
      order: 0,
      description: '',
      command: '',
      variables: [],
      copyCount: 0,
      createdAt: now,
      updatedAt: now,
    };
    dispatch({ type: 'entry/add', entry });
    setSelectedId(entry.id);
    setEditingId(entry.id);
    setNewEntryId(entry.id);
  }, [activeTab, discardEmptyNew, dispatch, notify]);

  const newTab = useCallback(() => {
    const names = new Set(sortedTabs.map((t) => t.name));
    let name = 'Nuevo tab';
    for (let i = 2; names.has(name); i++) name = `Nuevo tab ${i}`;
    const tab = { id: newId(), name, order: sortedTabs.length };
    dispatch({ type: 'tab/add', tab });
    selectView({ type: 'tab', tabId: tab.id });
    setRenamingTabId(tab.id);
  }, [dispatch, selectView, sortedTabs]);

  const goToEntry = useCallback(
    (entry: Entry) => {
      selectView({ type: 'tab', tabId: entry.tabId });
      setSelectedId(entry.id);
      setSearchOpen(false);
    },
    [selectView],
  );

  const moveSelection = (delta: number) => {
    if (navList.length === 0) return;
    const index = navList.findIndex((e) => e.id === selectedId);
    const next = index < 0 ? (delta > 0 ? 0 : navList.length - 1) : Math.min(navList.length - 1, Math.max(0, index + delta));
    setSelectedId(navList[next]?.id ?? null);
  };

  const cycleTab = (delta: number) => {
    const views: View[] = [{ type: 'frequent' }, ...sortedTabs.map((t) => ({ type: 'tab' as const, tabId: t.id }))];
    const current = views.findIndex((v) =>
      v.type === 'frequent' ? effectiveView.type === 'frequent' : effectiveView.type === 'tab' && effectiveView.tabId === v.tabId,
    );
    const next = views[(current + delta + views.length) % views.length];
    if (next) selectView(next);
  };

  const selectedEntry = entries.find((e) => e.id === selectedId) ?? null;
  const modalOpen = searchOpen || settingsOpen || secretPrompt !== null;

  useKeybindings(
    {
      goToTab: (n = 0) => {
        if (n === 0) return selectView({ type: 'frequent' });
        const tab = sortedTabs[n - 1];
        if (tab) selectView({ type: 'tab', tabId: tab.id });
      },
      newEntry,
      newTab,
      search: () => setSearchOpen(true),
      copySelected: () => {
        if (selectedEntry && editingId !== selectedEntry.id) void doCopy(selectedEntry);
        else if (!selectedEntry) notify('Selecciona una entrada con ↑↓ o un clic', 'warn');
      },
      copyAndDismiss: () => {
        if (selectedEntry && editingId !== selectedEntry.id) void doCopy(selectedEntry, { silent: true });
        else setToast(null);
      },
      nextEntry: () => moveSelection(1),
      prevEntry: () => moveSelection(-1),
      nextTab: () => cycleTab(1),
      prevTab: () => cycleTab(-1),
    },
    !modalOpen,
  );

  return (
    <div className="app">
      <Sidebar
        tabs={sortedTabs}
        entries={entries}
        view={effectiveView}
        theme={settings.theme}
        renamingId={renamingTabId}
        onRenamingDone={() => setRenamingTabId(null)}
        onSelectView={selectView}
        onAddTab={newTab}
        onRenameTab={(id, name) => dispatch({ type: 'tab/rename', id, name })}
        onDeleteTab={(id) => {
          const tab = sortedTabs.find((t) => t.id === id);
          dispatch({ type: 'tab/delete', id });
          notify(`Tab «${tab?.name ?? ''}» eliminado`);
        }}
        onMoveTab={(id, targetId, place) => dispatch({ type: 'tab/move', id, targetId, place })}
        onOpenSettings={() => setSettingsOpen(true)}
        onOpenSearch={() => setSearchOpen(true)}
        onToggleTheme={() =>
          dispatch({ type: 'settings/update', patch: { theme: settings.theme === 'light' ? 'dark' : 'light' } })
        }
      />

      <main className="content">
        {notices.length > 0 && (
          <div className="notices">
            {notices.map((n, i) => (
              <div key={n} className="notice" role="alert">
                <span>{n}</span>
                <button type="button" className="icon-btn icon-btn--sm" onClick={() => dismissNotice(i)} aria-label="Descartar aviso">
                  ×
                </button>
              </div>
            ))}
          </div>
        )}

        {effectiveView.type === 'frequent' || !activeTab ? (
          <FrequentTab
            entries={entries}
            tabs={sortedTabs}
            history={history}
            selectedId={selectedId}
            inspectedId={inspectedId}
            onSelect={setSelectedId}
            onCopy={(e) => void doCopy(e)}
            onReplay={(item) => void replay(item)}
            onCloseInspector={() => setInspectedId(null)}
            onGoToEntry={goToEntry}
          />
        ) : (
          <TabPanel
            tab={activeTab}
            tabIndex={sortedTabs.indexOf(activeTab)}
            entries={tabEntries}
            tabs={sortedTabs}
            globals={globalVariables}
            selectedId={selectedId}
            editingId={editingId}
            onSelect={(id) => {
              if (editingId && editingId !== id) {
                discardEmptyNew();
                setEditingId(null);
              }
              setSelectedId(id);
            }}
            onCopy={(e) => void doCopy(e)}
            onEdit={(id) => {
              discardEmptyNew();
              setSelectedId(id);
              setEditingId(id);
            }}
            onCancelEdit={(entry) => {
              setEditingId(null);
              if (entry.id === newEntryId) discardEmptyNew();
            }}
            onSave={(id, patch) => {
              dispatch({ type: 'entry/update', id, patch });
              setEditingId(null);
              setNewEntryId(null);
              if (patch.tabId !== activeTab.id) {
                setView({ type: 'tab', tabId: patch.tabId });
                notify('Entrada movida', 'ok', `Ahora está en «${sortedTabs.find((t) => t.id === patch.tabId)?.name ?? ''}».`);
              } else {
                notify('Entrada guardada');
              }
            }}
            onDelete={(id) => {
              dispatch({ type: 'entry/delete', id });
              setEditingId(null);
              setSelectedId(null);
              if (id === newEntryId) setNewEntryId(null);
              else notify('Entrada eliminada');
            }}
            onNewEntry={newEntry}
          />
        )}
      </main>

      {searchOpen && (
        <SearchOverlay
          entries={entries}
          tabs={sortedTabs}
          globals={globalVariables}
          onClose={() => setSearchOpen(false)}
          onGoTo={goToEntry}
          onCopy={(entry) => {
            setSearchOpen(false);
            void doCopy(entry);
          }}
        />
      )}

      {settingsOpen && <SettingsModal onClose={() => setSettingsOpen(false)} notify={notify} />}

      {secretPrompt && (
        <SecretPrompt
          names={secretPrompt.names}
          onClose={() => setSecretPrompt(null)}
          onSkip={() => {
            const { entry } = secretPrompt;
            setSecretPrompt(null);
            void doCopy(entry, { skipSecretPrompt: true });
          }}
          onSubmit={(values) => {
            const { entry } = secretPrompt;
            for (const [k, v] of Object.entries(values)) if (v) sessionSecrets.current[k] = v;
            setSecretPrompt(null);
            // Lo que quede vacío se copia como $nombre.
            void doCopy(entry, { skipSecretPrompt: true });
          }}
        />
      )}

      <Toast toast={toast} durationMs={settings.toastDurationMs} onClose={closeToast} />
    </div>
  );
}
