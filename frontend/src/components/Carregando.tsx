export function Carregando() {
  return (
    <div className="loading"><div className="spinner" /><div className="loading-text">Carregando...</div></div>
  );
}

export function Vazio({ icone, texto }: { icone: string; texto: string }) {
  return (
    <div className="empty"><div className="empty-icon">{icone}</div><div className="empty-text">{texto}</div></div>
  );
}
