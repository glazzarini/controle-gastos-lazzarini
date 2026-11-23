// SVGs idênticos aos do index.html.
type Props = { width?: number; height?: number; strokeWidth?: number };

const base = { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor' } as const;

export const IconeCalendario = () => (
  <svg {...base} width="14" height="14"><rect x="3" y="4" width="18" height="18" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" /></svg>
);

export const IconeAtualizar = () => (
  <svg {...base} width="16" height="16" strokeWidth="2">
    <path d="M23 4v6h-6" /><path d="M1 20v-6h6" />
    <path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" />
  </svg>
);

export const IconePainel = () => (
  <svg {...base}><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></svg>
);

export const IconeLancar = () => (
  <svg {...base}><circle cx="12" cy="12" r="9" /><path d="M12 8v8M8 12h8" /></svg>
);

export const IconeLancamentos = () => (
  <svg {...base}><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" /><rect x="9" y="3" width="6" height="4" rx="1" /><path d="M9 12h6M9 16h4" /></svg>
);

export const IconeOrcamento = () => (
  <svg {...base}><path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" /></svg>
);

export const IconeHistorico = () => (
  <svg {...base}><path d="M3 3v18h18" /><path d="M7 16l4-4 4 4 4-5" /></svg>
);

export const IconeLixeira = ({ width, height, strokeWidth }: Props) => (
  <svg {...base} width={width} height={height} strokeWidth={strokeWidth}>
    <polyline points="3 6 5 6 21 6" /><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6" /><path d="M10 11v6M14 11v6" /><path d="M9 6V4h6v2" />
  </svg>
);

export const IconeLapis = () => (
  <svg {...base}><path d="M17 3a2.85 2.83 0 114 4L7.5 20.5 2 22l1.5-5.5L17 3z" /></svg>
);

export const IconeEditar = () => (
  <svg {...base} width="16" height="16" strokeWidth="2">
    <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
  </svg>
);

export const IconeVoltar = () => (
  <svg {...base}><path d="M19 12H5M12 5l-7 7 7 7" /></svg>
);
