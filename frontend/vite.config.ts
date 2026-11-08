/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    // Substitui o manifest.json e o sw.js escritos à mão.
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'script-defer',
      includeAssets: ['icons/favicon.png', 'icons/apple-touch-icon.png'],
      manifest: {
        name: 'Controle de Gastos — Lazzarini',
        short_name: 'Gastos',
        description: 'Controle de gastos do cartão de crédito',
        lang: 'pt-BR',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        background_color: '#0f1117',
        theme_color: '#0f1117',
        orientation: 'portrait-primary',
        icons: [
          { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Shell do app em cache (offline). /api/* sempre vai para a rede, como no sw.js atual.
        globPatterns: ['**/*.{js,css,html,png,svg,webmanifest}'],
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/healthz/, /^\/readyz/],
        cleanupOutdatedCaches: true,
      },
    }),
  ],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:8080' },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true,
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/main.tsx', 'src/test/**', 'src/**/*.test.{ts,tsx}', 'src/**/*.d.ts'],
      reporter: ['text', 'html'],
      thresholds: { statements: 100, branches: 100, functions: 100, lines: 100 },
    },
  },
});
