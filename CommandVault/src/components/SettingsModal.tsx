import { useRef, useState } from 'react';
import type { CommentStyle, CopyMode, Variable } from '../types/model';
import { useStore } from '../state/store';
import { buildExport, exportFileName, parseImport, type ImportResult } from '../storage/exportImport';
import { DataError } from '../storage/validate';
import { formatForCopy } from '../domain/resolveCommand';
import { SHORTCUTS } from '../hooks/useKeybindings';
import { Modal } from './Modal';
import { VariableEditor, validateVariables } from './VariableEditor';

interface Props {
  onClose: () => void;
  notify: (message: string, tone?: 'ok' | 'warn' | 'error', detail?: string) => void;
}

/** Ajustes globales: copiado, apariencia, variables globales, datos y atajos. */
export function SettingsModal({ onClose, notify }: Props) {
  const { config, history, dispatch, sessionSecrets } = useStore();
  const { settings } = config;
  const set = (patch: Partial<typeof settings>) => dispatch({ type: 'settings/update', patch });

  const [globals, setGlobals] = useState<Variable[]>(config.globalVariables);
  const globalsDirty = JSON.stringify(globals) !== JSON.stringify(config.globalVariables);
  const globalsValid = Object.keys(validateVariables(globals)).length === 0;

  const [includeHistory, setIncludeHistory] = useState(false);
  const [pendingImport, setPendingImport] = useState<ImportResult | null>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const [confirmClear, setConfirmClear] = useState(false);
  // Texto libre mientras se escribe; se aplica solo si es un valor válido.
  const [toastSec, setToastSec] = useState(String(settings.toastDurationMs / 1000));
  const fileRef = useRef<HTMLInputElement>(null);

  const doExport = () => {
    const file = buildExport(config, includeHistory ? history : null);
    const blob = new Blob([JSON.stringify(file, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = exportFileName();
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    notify('Configuración exportada', 'ok', 'Los valores sensibles no se incluyeron.');
  };

  const onPickFile = async (file: File | undefined) => {
    setImportError(null);
    setPendingImport(null);
    if (!file) return;
    try {
      setPendingImport(parseImport(await file.text()));
    } catch (error) {
      setImportError(error instanceof DataError ? error.message : 'No se pudo leer el archivo.');
    } finally {
      if (fileRef.current) fileRef.current.value = '';
    }
  };

  const confirmImport = () => {
    if (!pendingImport) return;
    dispatch({ type: 'data/replace', config: pendingImport.config, history: pendingImport.history });
    setGlobals(pendingImport.config.globalVariables);
    setPendingImport(null);
    notify('Configuración importada', pendingImport.warnings.length ? 'warn' : 'ok', pendingImport.warnings.join(' ') || undefined);
  };

  const preview = formatForCopy('npm run build', 'Compilar el proyecto', settings.copyMode, settings.commentStyle);

  return (
    <Modal title="Ajustes" onClose={onClose} size="lg">
      <div className="modal__body settings">
        <section className="settings__section">
          <h3>Copiado</h3>
          <label className="setting">
            <span className="setting__text">
              <strong>Resolver variables sensibles al copiar</strong>
              <span className="hint">
                Si está activo, al copiar se piden los valores y se recuerdan solo mientras la app esté abierta. Si no,
                se copian como <span className="mono">$nombre</span>.
              </span>
            </span>
            <input
              type="checkbox"
              className="switch"
              checked={settings.resolveSensitiveOnCopy}
              onChange={(e) => {
                set({ resolveSensitiveOnCopy: e.target.checked });
                if (!e.target.checked) sessionSecrets.current = {};
              }}
            />
          </label>

          <div className="setting">
            <span className="setting__text">
              <strong>Qué copiar</strong>
            </span>
            <div className="segmented" role="radiogroup" aria-label="Qué copiar">
              {(
                [
                  ['command', 'Solo el comando'],
                  ['commandWithComment', 'Comando + descripción'],
                ] as [CopyMode, string][]
              ).map(([value, label]) => (
                <label key={value} className={settings.copyMode === value ? 'is-on' : ''}>
                  <input type="radio" name="copyMode" checked={settings.copyMode === value} onChange={() => set({ copyMode: value })} />
                  {label}
                </label>
              ))}
            </div>
          </div>

          <label className="setting">
            <span className="setting__text">
              <strong>Estilo de comentario</strong>
              <span className="hint">Se usa con «Comando + descripción».</span>
            </span>
            <select
              className="field field--auto"
              value={settings.commentStyle}
              disabled={settings.copyMode === 'command'}
              onChange={(e) => set({ commentStyle: e.target.value as CommentStyle })}
            >
              <option value="bash"># bash / PowerShell</option>
              <option value="cmd">REM cmd</option>
              <option value="slashes">// otros</option>
            </select>
          </label>
          <pre className="settings__preview" aria-label="Vista previa">{preview}</pre>

          <label className="setting">
            <span className="setting__text">
              <strong>Cerrar notificación tras</strong>
            </span>
            <span className="inline">
              <input
                type="number"
                className="field field--num"
                min={0.5}
                max={30}
                step={0.5}
                value={toastSec}
                onChange={(e) => {
                  setToastSec(e.target.value);
                  const s = Number(e.target.value.replace(',', '.'));
                  if (e.target.value !== '' && s >= 0.5 && s <= 30) set({ toastDurationMs: Math.round(s * 1000) });
                }}
                onBlur={() => setToastSec(String(settings.toastDurationMs / 1000))}
              />
              segundos
            </span>
          </label>
        </section>

        <section className="settings__section">
          <h3>Apariencia</h3>
          <div className="setting">
            <span className="setting__text">
              <strong>Tema</strong>
            </span>
            <div className="segmented" role="radiogroup" aria-label="Tema">
              {(
                [
                  ['light', 'Claro'],
                  ['dark', 'Oscuro'],
                ] as const
              ).map(([value, label]) => (
                <label key={value} className={settings.theme === value ? 'is-on' : ''}>
                  <input type="radio" name="theme" checked={settings.theme === value} onChange={() => set({ theme: value })} />
                  {label}
                </label>
              ))}
            </div>
          </div>
        </section>

        <section className="settings__section">
          <h3>Variables globales</h3>
          <p className="hint">
            Disponibles en todas las entradas y en el autocompletado. Una variable de la entrada con el mismo nombre tiene
            prioridad.
          </p>
          <VariableEditor variables={globals} onChange={setGlobals} emptyHint="No hay variables globales." />
          {globalsDirty && (
            <div className="inline">
              <button
                type="button"
                className="btn btn--primary btn--sm"
                disabled={!globalsValid}
                onClick={() => {
                  dispatch({ type: 'globals/set', variables: globals });
                  notify('Variables globales guardadas');
                }}
              >
                Guardar variables globales
              </button>
              <button type="button" className="btn btn--ghost btn--sm" onClick={() => setGlobals(config.globalVariables)}>
                Descartar
              </button>
            </div>
          )}
        </section>

        <section className="settings__section">
          <h3>Datos</h3>
          <div className="setting">
            <span className="setting__text">
              <strong>Exportar configuración</strong>
              <span className="hint">Tabs, entradas, variables y ajustes en .json. Las sensibles se exportan sin valor.</span>
            </span>
            <span className="inline">
              <label className="check">
                <input type="checkbox" checked={includeHistory} onChange={(e) => setIncludeHistory(e.target.checked)} />
                Incluir historial
              </label>
              <button type="button" className="btn btn--ghost btn--sm" onClick={doExport}>
                Exportar .json
              </button>
            </span>
          </div>

          <div className="setting">
            <span className="setting__text">
              <strong>Importar configuración</strong>
              <span className="hint">Reemplaza tabs, entradas, variables y ajustes actuales.</span>
            </span>
            <span className="inline">
              <input
                ref={fileRef}
                type="file"
                accept="application/json,.json"
                hidden
                onChange={(e) => void onPickFile(e.target.files?.[0])}
              />
              <button type="button" className="btn btn--ghost btn--sm" onClick={() => fileRef.current?.click()}>
                Elegir archivo…
              </button>
            </span>
          </div>
          {importError && <p className="error">{importError}</p>}
          {pendingImport && (
            <div className="callout">
              <p>
                El archivo trae <strong>{pendingImport.config.tabs.length} tabs</strong> y{' '}
                <strong>{pendingImport.config.entries.length} entradas</strong>
                {pendingImport.history ? ` y ${pendingImport.history.length} registros de historial` : ''}. Tu
                configuración actual ({config.tabs.length} tabs, {config.entries.length} entradas) se reemplazará.
              </p>
              {pendingImport.warnings.length > 0 && (
                <ul className="callout__list">
                  {pendingImport.warnings.slice(0, 5).map((w) => (
                    <li key={w}>{w}</li>
                  ))}
                </ul>
              )}
              <div className="inline">
                <button type="button" className="btn btn--danger btn--sm" onClick={confirmImport}>
                  Reemplazar
                </button>
                <button type="button" className="btn btn--ghost btn--sm" onClick={() => setPendingImport(null)}>
                  Cancelar
                </button>
              </div>
            </div>
          )}

          <div className="setting">
            <span className="setting__text">
              <strong>Limpiar historial</strong>
              <span className="hint">{history.length} registros. Los contadores de «Frecuentes» se mantienen.</span>
            </span>
            {confirmClear ? (
              <span className="inline">
                <button
                  type="button"
                  className="btn btn--danger btn--sm"
                  onClick={() => {
                    dispatch({ type: 'history/clear' });
                    setConfirmClear(false);
                    notify('Historial limpiado');
                  }}
                >
                  Sí, limpiar
                </button>
                <button type="button" className="btn btn--ghost btn--sm" onClick={() => setConfirmClear(false)}>
                  No
                </button>
              </span>
            ) : (
              <button type="button" className="btn btn--ghost btn--sm" disabled={history.length === 0} onClick={() => setConfirmClear(true)}>
                Limpiar historial
              </button>
            )}
          </div>
        </section>

        <section className="settings__section">
          <h3>Atajos de teclado</h3>
          <p className="hint">
            El navegador reserva Ctrl+N, Ctrl+Shift+N y Ctrl+1…9: ahí usa la variante con Alt. En la app de escritorio
            funcionan ambas.
          </p>
          <table className="shortcuts">
            <tbody>
              {SHORTCUTS.map((s) => (
                <tr key={s.keys}>
                  <td>
                    <kbd>{s.keys}</kbd>
                    {s.alt && (
                      <>
                        {' '}
                        <span className="muted">o</span> <kbd>{s.alt}</kbd>
                      </>
                    )}
                  </td>
                  <td>{s.action}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>
    </Modal>
  );
}
