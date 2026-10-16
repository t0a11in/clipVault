import type { Variable } from '../types/model';
import { tokenizeTemplate } from '../domain/resolveCommand';

interface Props {
  command: string;
  variables: Variable[];
  globals: Variable[];
  /** template: muestra $nombre resaltado · example: muestra el valor resuelto */
  mode: 'template' | 'example';
}

/**
 * Pinta un comando con sus variables resaltadas.
 * En modo "example" las sensibles se ven como $nombre (lo que se copiará).
 */
export function CommandText({ command, variables, globals, mode }: Props) {
  const segments = tokenizeTemplate(command, variables, globals);
  return (
    <code className="cmd-text">
      {segments.map((seg, i) => {
        if (seg.kind === 'text') return <span key={i}>{seg.text}</span>;
        const title =
          seg.state === 'sensitive'
            ? `$${seg.name}: sensible, no se guarda`
            : seg.state === 'empty'
              ? `$${seg.name}: sin valor`
              : seg.state === 'unknown'
                ? `$${seg.name}: no definida (se copia tal cual)`
                : `$${seg.name}${seg.fromGlobal ? ' (global)' : ''}`;
        const text = mode === 'example' && seg.state === 'value' ? seg.value : `$${seg.name}`;
        return (
          <span key={i} className={`tok tok--${seg.state} tok--${mode}`} title={title}>
            {text}
          </span>
        );
      })}
    </code>
  );
}
