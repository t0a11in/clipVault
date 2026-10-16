import { useState } from 'react';
import { Modal } from './Modal';

interface Props {
  names: string[];
  onSubmit: (values: Record<string, string>) => void;
  /** Copiar dejando las sensibles como $nombre. */
  onSkip: () => void;
  onClose: () => void;
}

/**
 * Pide valores sensibles al copiar (solo si "Resolver sensibles al copiar" está activo).
 * Los valores viven en memoria durante la sesión: no se escriben en disco ni en el historial.
 */
export function SecretPrompt({ names, onSubmit, onSkip, onClose }: Props) {
  const [values, setValues] = useState<Record<string, string>>({});
  return (
    <Modal title="Valores sensibles" onClose={onClose} size="sm">
      <form
        className="modal__body stack"
        onSubmit={(e) => {
          e.preventDefault();
          onSubmit(values);
        }}
      >
        <p className="muted">
          Se usan solo para esta copia y se recuerdan hasta cerrar la app. No se guardan ni aparecen en el historial.
        </p>
        {names.map((name, i) => (
          <label key={name} className="editor__field">
            <span className="label mono">${name}</span>
            <input
              className="field field--mono"
              type="password"
              autoComplete="off"
              autoFocus={i === 0}
              value={values[name] ?? ''}
              onChange={(e) => setValues((v) => ({ ...v, [name]: e.target.value }))}
            />
          </label>
        ))}
        <footer className="modal__foot">
          <button type="button" className="btn btn--ghost" onClick={onSkip}>
            Copiar sin rellenar
          </button>
          <button type="submit" className="btn btn--primary">
            Copiar
          </button>
        </footer>
      </form>
    </Modal>
  );
}
