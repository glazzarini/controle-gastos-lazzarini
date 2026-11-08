import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { AppDataProvider } from './state/AppData';
import { UiProvider } from './state/Ui';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <UiProvider>
      <AppDataProvider>
        <App />
      </AppDataProvider>
    </UiProvider>
  </StrictMode>,
);
