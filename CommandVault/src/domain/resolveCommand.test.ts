import { describe, expect, it } from 'vitest';
import { formatForCopy, MASK, resolveCommand } from './resolveCommand';
import { addMissingVariables, extractVariableNames, variableTokenAt } from './variables';
import type { Variable } from '../types/model';

const v = (name: string, value: string | undefined, sensitive = false): Variable => ({
  id: name,
  name,
  sensitive,
  ...(value === undefined ? {} : { value }),
});

describe('resolveCommand', () => {
  it('resuelve una variable simple (ejemplo de la especificación)', () => {
    const r = resolveCommand('rmdir /s /q "$ruta"', {
      variables: [v('ruta', 'D:\\2026\\proyectos\\SnapGasto\\frontend\\angular\\node_modules')],
    });
    expect(r.text).toBe('rmdir /s /q "D:\\2026\\proyectos\\SnapGasto\\frontend\\angular\\node_modules"');
    expect(r.unresolved).toEqual([]);
  });

  it('resuelve varias variables', () => {
    const r = resolveCommand('repopack "$origen" --output "$destino"', {
      variables: [v('origen', 'D:\\src'), v('destino', 'D:\\out.txt')],
    });
    expect(r.text).toBe('repopack "D:\\src" --output "D:\\out.txt"');
  });

  it('un comando sin variables queda igual', () => {
    expect(resolveCommand('ipconfig /flushdns', { variables: [] }).text).toBe('ipconfig /flushdns');
  });

  it('no toca variables desconocidas del shell como $HOME', () => {
    const r = resolveCommand('cd $HOME && echo $ruta', { variables: [v('ruta', 'x')] });
    expect(r.text).toBe('cd $HOME && echo x');
  });

  it('usa coincidencia exacta: $ruta no altera $ruta2', () => {
    const r = resolveCommand('$ruta $ruta2', { variables: [v('ruta', 'A'), v('ruta2', 'B')] });
    expect(r.text).toBe('A B');
    const solo = resolveCommand('$ruta2', { variables: [v('ruta', 'A')] });
    expect(solo.text).toBe('$ruta2');
  });

  it('deja $nombre y lo informa si la variable no sensible está vacía', () => {
    const r = resolveCommand('echo $a', { variables: [v('a', '')] });
    expect(r.text).toBe('echo $a');
    expect(r.unresolved).toEqual(['a']);
  });

  it('las variables de la entrada tienen prioridad sobre las globales', () => {
    const r = resolveCommand('$x $y', {
      variables: [v('x', 'local')],
      globals: [v('x', 'global'), v('y', 'G')],
    });
    expect(r.text).toBe('local G');
  });

  it('mantiene las sensibles como $nombre por defecto', () => {
    const r = resolveCommand('psql -U $user -W $DB_PASSWORD', {
      variables: [v('user', 'postgres'), v('DB_PASSWORD', undefined, true)],
    });
    expect(r.text).toBe('psql -U postgres -W $DB_PASSWORD');
    expect(r.masked).toBe(`psql -U postgres -W ${MASK}`);
    expect(r.replay).toBe('psql -U postgres -W $DB_PASSWORD');
    expect(r.sensitivePending).toEqual(['DB_PASSWORD']);
  });

  it('resuelve sensibles solo con el ajuste activo y un valor de sesión', () => {
    const opts = { variables: [v('TOKEN', undefined, true)], sessionSecrets: { TOKEN: 's3cr3t' } };
    expect(resolveCommand('x $TOKEN', opts).text).toBe('x $TOKEN');
    const r = resolveCommand('x $TOKEN', { ...opts, resolveSensitive: true });
    expect(r.text).toBe('x s3cr3t');
    // El historial nunca ve el secreto.
    expect(r.masked).toBe(`x ${MASK}`);
    expect(r.replay).toBe('x $TOKEN');
    expect(r.sensitivePending).toEqual([]);
  });

  it('no re-expande variables dentro de los valores', () => {
    const r = resolveCommand('$a', { variables: [v('a', '$b'), v('b', 'NO')] });
    expect(r.text).toBe('$b');
  });

  it('trata literalmente patrones especiales de replace como $& en los valores', () => {
    const r = resolveCommand('echo $a', { variables: [v('a', 'x$&y$1')] });
    expect(r.text).toBe('echo x$&y$1');
  });
});

describe('formatForCopy', () => {
  it('modo solo comando', () => {
    expect(formatForCopy('ls', 'Listar', 'command', 'bash')).toBe('ls');
  });
  it('agrega la descripción como comentario según el shell', () => {
    expect(formatForCopy('ls', 'Listar', 'commandWithComment', 'bash')).toBe('# Listar\nls');
    expect(formatForCopy('dir', 'Listar', 'commandWithComment', 'cmd')).toBe('REM Listar\ndir');
    expect(formatForCopy('x', 'a\nb', 'commandWithComment', 'slashes')).toBe('// a\n// b\nx');
  });
  it('sin descripción no agrega comentario vacío', () => {
    expect(formatForCopy('ls', '  ', 'commandWithComment', 'bash')).toBe('ls');
  });
});

describe('variables', () => {
  it('extrae nombres únicos en orden', () => {
    expect(extractVariableNames('$b $a $b ${x} $_ok $9no')).toEqual(['b', 'a', '_ok']);
  });

  it('agrega las variables faltantes y marca como sensibles los nombres típicos', () => {
    const result = addMissingVariables('$ruta $API_KEY $global', [v('ruta', 'x')], [v('global', 'g')]);
    expect(result.map((x) => [x.name, x.sensitive])).toEqual([
      ['ruta', false],
      ['API_KEY', true],
    ]);
  });

  it('detecta el token $parcial en el cursor', () => {
    expect(variableTokenAt('echo $ru', 8)).toEqual({ prefix: 'ru', start: 5 });
    expect(variableTokenAt('echo $', 6)).toEqual({ prefix: '', start: 5 });
    expect(variableTokenAt('echo ru', 7)).toBeNull();
  });
});
