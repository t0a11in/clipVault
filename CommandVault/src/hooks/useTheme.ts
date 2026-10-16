import { useEffect } from 'react';
import type { ThemeMode } from '../types/model';

/** Aplica el tema como atributo de <html>; las variables CSS hacen el resto. */
export function useTheme(theme: ThemeMode): void {
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
  }, [theme]);
}
