import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll, beforeEach, vi } from 'vitest';
import { resetarBanco, servidor } from './servidor';

beforeAll(() => servidor.listen({ onUnhandledFrame: 'error' }));
// "Hoje" fixo às 22:30 de 15/06/2026 — horário em que o index.html (UTC) já mostrava o dia seguinte.
beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(2026, 5, 15, 22, 30));
  resetarBanco();
});
afterEach(() => {
  cleanup();
  servidor.resetHandlers();
  vi.useRealTimers();
});
afterAll(() => servidor.close());

// jsdom não implementa scrollTo.
window.scrollTo = vi.fn() as unknown as typeof window.scrollTo;
