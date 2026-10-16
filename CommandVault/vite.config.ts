/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Configuración de Vite.
// - base './' permite abrir el build desde file:// o empaquetarlo con Tauri/Electron.
// - Vitest reutiliza esta misma configuración (entorno node: la lógica de dominio es pura).
export default defineConfig({
  base: './',
  plugins: [react()],
  server: { port: 5173, strictPort: false },
  build: { target: 'es2022', outDir: 'dist' },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
});
