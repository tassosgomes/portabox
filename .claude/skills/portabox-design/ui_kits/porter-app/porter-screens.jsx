// Porter App — screens for the PortaBox Capture mobile app
// Uses IOSDevice from ios-frame.jsx for chrome; content is custom PortaBox.
const { useState } = React;

// Brand tokens (mirror colors_and_type.css, inlined so each screen is self-contained)
const PB = {
  navy900: '#0B2B47',
  navy700: '#1E3A8A',
  navy500: '#4B5563',
  orange500: '#F97316',
  orange700: '#EA580C',
  paper: '#F9FAFB',
  ice: '#E0E7FF',
  gray200: '#D1D5DB',
  gray500: '#6B7280',
  ink: '#1F2937',
  success: '#16A34A',
  white: '#FFFFFF',
};

const fontDisplay = "'Plus Jakarta Sans', system-ui, sans-serif";
const fontBody = "'Inter', system-ui, sans-serif";
const fontMono = "'JetBrains Mono', ui-monospace, monospace";

// ─── Shared chrome ───────────────────────────────────────
function PorterTopBar({ title, subtitle }) {
  return (
    <div style={{
      background: PB.navy900, color: 'white',
      padding: '56px 20px 20px', // below status bar
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <img src="../../assets/logo-portabox-mark.svg" style={{ width: 28, height: 28, filter: 'brightness(0) invert(1)' }} />
        <div style={{ fontFamily: fontDisplay, fontWeight: 800, fontSize: 18, letterSpacing: '-0.01em' }}>PortaBox</div>
        <div style={{ marginLeft: 'auto', width: 36, height: 36, borderRadius: '50%', background: 'rgba(255,255,255,0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: fontDisplay, fontWeight: 700, fontSize: 13 }}>JM</div>
      </div>
      <div style={{ marginTop: 16, fontFamily: fontDisplay, fontWeight: 800, fontSize: 26, letterSpacing: '-0.02em', lineHeight: 1.1 }}>{title}</div>
      {subtitle && <div style={{ marginTop: 4, fontFamily: fontBody, fontSize: 13, color: 'rgba(255,255,255,0.7)' }}>{subtitle}</div>}
    </div>
  );
}

function PorterTabBar({ active, onChange }) {
  const tabs = [
    { id: 'home', label: 'Início', icon: 'home' },
    { id: 'capture', label: 'Capturar', icon: 'camera', primary: true },
    { id: 'retrieve', label: 'Retirar', icon: 'key' },
    { id: 'history', label: 'Histórico', icon: 'list' },
  ];
  const Icon = ({ kind, color, size = 22 }) => {
    const common = { width: size, height: size, fill: 'none', stroke: color, strokeWidth: 2, strokeLinecap: 'round', strokeLinejoin: 'round' };
    if (kind === 'home') return <svg {...common} viewBox="0 0 24 24"><path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/></svg>;
    if (kind === 'camera') return <svg {...common} viewBox="0 0 24 24"><path d="M4 8h3l2-3h6l2 3h3v11H4z"/><circle cx="12" cy="13" r="4"/></svg>;
    if (kind === 'key') return <svg {...common} viewBox="0 0 24 24"><circle cx="8" cy="15" r="4"/><path d="M10.5 12.5L20 3m-4 4l3 3m-6-1l2 2"/></svg>;
    if (kind === 'list') return <svg {...common} viewBox="0 0 24 24"><path d="M4 6h16M4 12h16M4 18h16"/></svg>;
  };
  return (
    <div style={{
      position: 'absolute', bottom: 0, left: 0, right: 0,
      height: 88, background: 'white', borderTop: `1px solid ${PB.gray200}`,
      paddingBottom: 24, display: 'flex', alignItems: 'flex-start', justifyContent: 'space-around', paddingTop: 10,
    }}>
      {tabs.map(t => {
        const isActive = active === t.id;
        if (t.primary) {
          return (
            <button key={t.id} onClick={() => onChange(t.id)} style={{
              width: 58, height: 58, borderRadius: '50%',
              background: PB.orange500, color: 'white', border: 'none',
              boxShadow: '0 6px 16px rgba(249,115,22,0.4)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              marginTop: -18, cursor: 'pointer',
            }}><Icon kind={t.icon} color="white" size={26} /></button>
          );
        }
        return (
          <button key={t.id} onClick={() => onChange(t.id)} style={{
            background: 'none', border: 'none', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 3,
            color: isActive ? PB.navy700 : PB.gray500, fontFamily: fontDisplay, fontSize: 10, fontWeight: 600, cursor: 'pointer',
          }}>
            <Icon kind={t.icon} color={isActive ? PB.navy700 : PB.gray500} />
            <span>{t.label}</span>
          </button>
        );
      })}
    </div>
  );
}

// ─── Screens ───────────────────────────────────────────────
function HomeScreen({ onNav }) {
  const pkgs = [
    { apt: '0701', name: 'João M.', carrier: 'Amazon', pin: '4829', status: 'waiting', time: '14:32' },
    { apt: '0402', name: 'Ana L.', carrier: 'Mercado Livre', pin: '7731', status: 'waiting', time: '14:18' },
    { apt: '1105', name: 'Pedro S.', carrier: 'Shopee', pin: '2204', status: 'notified', time: '13:55' },
  ];
  return (
    <div style={{ height: '100%', background: PB.paper, position: 'relative' }}>
      <PorterTopBar title="Olá, João" subtitle="Condomínio Vista Verde · hoje, 14 de abril" />
      <div style={{ padding: '18px 20px 110px' }}>
        {/* KPI row */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 18 }}>
          <div style={{ background: 'white', borderRadius: 14, padding: 14, boxShadow: '0 6px 16px rgba(11,43,71,0.08)' }}>
            <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500, letterSpacing: '0.08em', textTransform: 'uppercase' }}>Aguardando</div>
            <div style={{ fontFamily: fontDisplay, fontWeight: 800, fontSize: 32, color: PB.orange500, letterSpacing: '-0.02em' }}>12</div>
            <div style={{ fontFamily: fontBody, fontSize: 11, color: PB.gray500 }}>retirada pelo morador</div>
          </div>
          <div style={{ background: 'white', borderRadius: 14, padding: 14, boxShadow: '0 6px 16px rgba(11,43,71,0.08)' }}>
            <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500, letterSpacing: '0.08em', textTransform: 'uppercase' }}>Entregues hoje</div>
            <div style={{ fontFamily: fontDisplay, fontWeight: 800, fontSize: 32, color: PB.success, letterSpacing: '-0.02em' }}>27</div>
            <div style={{ fontFamily: fontBody, fontSize: 11, color: PB.gray500 }}>concluídas</div>
          </div>
        </div>

        {/* Primary CTA */}
        <button onClick={() => onNav('capture')} style={{
          width: '100%', padding: '16px 20px', borderRadius: 999,
          background: PB.orange500, color: 'white', border: 'none',
          fontFamily: fontDisplay, fontWeight: 700, fontSize: 16,
          boxShadow: '0 8px 20px rgba(249,115,22,0.35)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10, cursor: 'pointer',
        }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 8h3l2-3h6l2 3h3v11H4z"/><circle cx="12" cy="13" r="4"/></svg>
          Fotografar nova encomenda
        </button>

        <div style={{ marginTop: 22, marginBottom: 10, fontFamily: fontDisplay, fontWeight: 700, fontSize: 14, color: PB.navy900, letterSpacing: '-0.01em' }}>Aguardando retirada</div>

        {pkgs.map((p, i) => (
          <div key={i} style={{
            background: 'white', borderRadius: 14, padding: '14px 16px', marginBottom: 8,
            boxShadow: '0 2px 4px rgba(11,43,71,0.06)',
            display: 'flex', alignItems: 'center', gap: 12,
          }}>
            <div style={{ width: 44, height: 44, borderRadius: 10, background: '#FEF3E9', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke={PB.orange700} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 8l-9-5-9 5 9 5 9-5z"/><path d="M3 8v8l9 5 9-5V8"/><path d="M12 13v8"/></svg>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontFamily: fontDisplay, fontWeight: 700, fontSize: 14, color: PB.navy900 }}>Apto {p.apt} · {p.name}</div>
              <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500, marginTop: 2 }}>{p.carrier} · {p.time}</div>
            </div>
            <div style={{ fontFamily: fontMono, fontWeight: 700, fontSize: 15, color: PB.navy900, letterSpacing: '0.12em' }}>{p.pin}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

function CaptureScreen({ onNav }) {
  return (
    <div style={{ height: '100%', background: PB.navy900, position: 'relative', color: 'white' }}>
      <PorterTopBar title="Capturar etiqueta" subtitle="Alinhe a etiqueta dentro da moldura" />
      <div style={{ padding: '20px 20px 110px', display: 'flex', flexDirection: 'column', gap: 18 }}>
        {/* Camera viewfinder */}
        <div style={{
          aspectRatio: '1', borderRadius: 20, background: '#111', position: 'relative', overflow: 'hidden',
          boxShadow: 'inset 0 0 0 2px rgba(255,255,255,0.08)',
        }}>
          {/* dashed guide */}
          <div style={{ position: 'absolute', inset: 24, border: `2px dashed ${PB.orange500}`, borderRadius: 14 }} />
          {/* corner brackets */}
          {[['top left','0 0 auto auto'], ['top right','0 auto auto 0'], ['bottom left','auto 0 0 auto'], ['bottom right','auto auto 0 0']].map((_, i) => (
            <div key={i} style={{ position: 'absolute', width: 24, height: 24,
              [['top','right','bottom','left'][i]]: 12,
              [i === 0 || i === 2 ? 'left' : 'right']: 12,
              borderTop: i < 2 ? `3px solid ${PB.orange500}` : 'none',
              borderBottom: i >= 2 ? `3px solid ${PB.orange500}` : 'none',
              borderLeft: i === 0 || i === 2 ? `3px solid ${PB.orange500}` : 'none',
              borderRight: i === 1 || i === 3 ? `3px solid ${PB.orange500}` : 'none',
              borderRadius: 4,
            }} />
          ))}
          {/* fake label */}
          <div style={{
            position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%,-50%) rotate(-3deg)',
            background: 'white', color: '#333', padding: '12px 14px', borderRadius: 4,
            fontFamily: fontMono, fontSize: 11, lineHeight: 1.4, width: '62%',
            boxShadow: '0 8px 20px rgba(0,0,0,0.4)',
          }}>
            <div style={{ fontWeight: 700 }}>APT 07-01</div>
            <div>João Mendes · Amazon</div>
            <div style={{ height: 18, marginTop: 4, background: 'repeating-linear-gradient(90deg,#000 0 2px,transparent 2px 3px,#000 3px 5px,transparent 5px 7px)' }} />
            <div style={{ fontSize: 9, marginTop: 2 }}>PB-2026-00341</div>
          </div>
        </div>

        {/* OCR status */}
        <div style={{ background: 'rgba(22,163,74,0.15)', border: '1px solid rgba(22,163,74,0.4)', borderRadius: 14, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 32, height: 32, borderRadius: '50%', background: PB.success, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'white', fontWeight: 700, fontSize: 16 }}>✓</div>
          <div style={{ flex: 1 }}>
            <div style={{ fontFamily: fontDisplay, fontWeight: 700, fontSize: 14 }}>OCR concluído</div>
            <div style={{ fontFamily: fontBody, fontSize: 11, color: 'rgba(255,255,255,0.7)' }}>Extração feita pela IA em 0.8s</div>
          </div>
        </div>

        {/* Action row */}
        <div style={{ display: 'flex', gap: 10 }}>
          <button onClick={() => onNav('home')} style={{
            flex: 1, padding: '14px', borderRadius: 999,
            background: 'rgba(255,255,255,0.08)', color: 'white', border: '1.5px solid rgba(255,255,255,0.25)',
            fontFamily: fontDisplay, fontWeight: 700, fontSize: 14, cursor: 'pointer',
          }}>Cancelar</button>
          <button onClick={() => onNav('confirm')} style={{
            flex: 2, padding: '14px', borderRadius: 999,
            background: PB.orange500, color: 'white', border: 'none',
            fontFamily: fontDisplay, fontWeight: 700, fontSize: 14, cursor: 'pointer',
            boxShadow: '0 6px 16px rgba(249,115,22,0.4)',
          }}>Confirmar dados →</button>
        </div>
      </div>
    </div>
  );
}

function ConfirmScreen({ onNav }) {
  return (
    <div style={{ height: '100%', background: PB.paper, position: 'relative' }}>
      <PorterTopBar title="Confirmar encomenda" subtitle="Revise os dados extraídos pela IA" />
      <div style={{ padding: '18px 20px 110px' }}>
        {/* Thumbnail */}
        <div style={{ background: 'white', borderRadius: 14, padding: 14, boxShadow: '0 6px 16px rgba(11,43,71,0.08)', marginBottom: 14, display: 'flex', gap: 12, alignItems: 'center' }}>
          <div style={{ width: 70, height: 70, borderRadius: 10, background: '#FEF3E9', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: fontMono, fontSize: 9, color: PB.orange700, padding: 4, textAlign: 'center' }}>
            APT 07-01<br/>João M.
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: 4, background: '#DCFCE7', color: '#166534', padding: '3px 8px', borderRadius: 999, fontFamily: fontDisplay, fontWeight: 700, fontSize: 10 }}>
              <span style={{ width: 5, height: 5, borderRadius: '50%', background: PB.success }} /> IA 98% confiança
            </div>
            <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500, marginTop: 6 }}>PB-2026-00341</div>
          </div>
        </div>

        {/* Fields */}
        <div style={{ background: 'white', borderRadius: 14, overflow: 'hidden', boxShadow: '0 6px 16px rgba(11,43,71,0.08)' }}>
          {[
            { lbl: 'Apartamento', val: '0701', hint: 'Torre A' },
            { lbl: 'Morador', val: 'João Mendes', hint: 'confirmado no cadastro' },
            { lbl: 'Transportadora', val: 'Amazon', hint: 'Prime · entrega padrão' },
            { lbl: 'Canal de aviso', val: 'WhatsApp', hint: '+55 11 9••••-4829' },
          ].map((f, i, arr) => (
            <div key={i} style={{ padding: '14px 16px', borderBottom: i < arr.length - 1 ? `1px solid ${PB.gray200}` : 'none' }}>
              <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500, letterSpacing: '0.06em', textTransform: 'uppercase' }}>{f.lbl}</div>
              <div style={{ fontFamily: fontDisplay, fontWeight: 700, fontSize: 16, color: PB.navy900, marginTop: 2 }}>{f.val}</div>
              <div style={{ fontFamily: fontBody, fontSize: 12, color: PB.gray500, marginTop: 1 }}>{f.hint}</div>
            </div>
          ))}
        </div>

        <button onClick={() => onNav('home')} style={{
          width: '100%', padding: '16px', borderRadius: 999,
          background: PB.orange500, color: 'white', border: 'none', marginTop: 16,
          fontFamily: fontDisplay, fontWeight: 700, fontSize: 16,
          boxShadow: '0 8px 20px rgba(249,115,22,0.35)', cursor: 'pointer',
        }}>Registrar e notificar morador</button>
        <button onClick={() => onNav('capture')} style={{
          width: '100%', padding: '14px', borderRadius: 999,
          background: 'transparent', color: PB.navy700, border: 'none', marginTop: 4,
          fontFamily: fontDisplay, fontWeight: 600, fontSize: 14, cursor: 'pointer',
        }}>Voltar e refazer foto</button>
      </div>
    </div>
  );
}

function RetrieveScreen({ onNav }) {
  const [pin, setPin] = useState(['', '', '', '']);
  const filled = pin.filter(Boolean).length;
  return (
    <div style={{ height: '100%', background: PB.paper, position: 'relative' }}>
      <PorterTopBar title="Retirar encomenda" subtitle="Peça o PIN ao morador" />
      <div style={{ padding: '30px 20px 110px' }}>
        <div style={{ textAlign: 'center', marginBottom: 26 }}>
          <div className="eyebrow" style={{ fontFamily: fontDisplay, fontWeight: 800, fontSize: 11, letterSpacing: '0.12em', textTransform: 'uppercase', color: PB.orange500 }}>Token de retirada</div>
          <div style={{ fontFamily: fontBody, fontSize: 13, color: PB.gray500, marginTop: 6 }}>Digite os 4 dígitos informados pelo morador</div>
        </div>
        <div style={{ display: 'flex', gap: 10, justifyContent: 'center', marginBottom: 22 }}>
          {[0,1,2,3].map(i => (
            <div key={i} style={{
              width: 58, height: 72, borderRadius: 14,
              background: 'white',
              border: `2px solid ${i < filled ? PB.orange500 : PB.gray200}`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontFamily: fontMono, fontWeight: 700, fontSize: 28, color: PB.navy900,
              boxShadow: i < filled ? '0 0 0 3px rgba(249,115,22,0.15)' : 'none',
            }}>{['4','8','2','9'][i]}</div>
          ))}
        </div>
        {/* Numpad */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10, marginBottom: 14 }}>
          {['1','2','3','4','5','6','7','8','9','','0','⌫'].map((n, i) => n === '' ? <div key={i}/> : (
            <button key={i} style={{
              padding: '18px 0', borderRadius: 14, background: 'white', border: 'none',
              fontFamily: fontDisplay, fontWeight: 700, fontSize: 22, color: PB.navy900,
              boxShadow: '0 2px 4px rgba(11,43,71,0.06)', cursor: 'pointer',
            }}>{n}</button>
          ))}
        </div>
        <button onClick={() => onNav('home')} style={{
          width: '100%', padding: '16px', borderRadius: 999,
          background: PB.success, color: 'white', border: 'none',
          fontFamily: fontDisplay, fontWeight: 700, fontSize: 16, cursor: 'pointer',
          boxShadow: '0 6px 16px rgba(22,163,74,0.35)',
        }}>Validar PIN</button>
      </div>
    </div>
  );
}

function HistoryScreen() {
  const items = [
    { t: '14:32', apt: '0701', name: 'João M.', carrier: 'Amazon', status: 'Entregue', ok: true },
    { t: '14:05', apt: '0305', name: 'Marina C.', carrier: 'Shopee', status: 'Entregue', ok: true },
    { t: '13:40', apt: '1202', name: 'Ricardo P.', carrier: 'Mercado Livre', status: 'Aguardando', ok: false },
    { t: '12:58', apt: '0807', name: 'Beatriz N.', carrier: 'Magalu', status: 'Entregue', ok: true },
    { t: '11:22', apt: '0402', name: 'Ana L.', carrier: 'Amazon', status: 'Entregue', ok: true },
  ];
  return (
    <div style={{ height: '100%', background: PB.paper, position: 'relative' }}>
      <PorterTopBar title="Histórico" subtitle="Últimas 24 horas · 27 eventos" />
      <div style={{ padding: '14px 20px 110px' }}>
        {items.map((it, i) => (
          <div key={i} style={{ background: 'white', borderRadius: 14, padding: '12px 14px', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 12, boxShadow: '0 2px 4px rgba(11,43,71,0.06)' }}>
            <div style={{ fontFamily: fontMono, fontSize: 11, color: PB.gray500, minWidth: 40 }}>{it.t}</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontFamily: fontDisplay, fontWeight: 700, fontSize: 14, color: PB.navy900 }}>Apto {it.apt} · {it.name}</div>
              <div style={{ fontFamily: fontMono, fontSize: 10, color: PB.gray500 }}>{it.carrier}</div>
            </div>
            <div style={{
              padding: '3px 10px', borderRadius: 999,
              background: it.ok ? '#DCFCE7' : '#FFEDD5',
              color: it.ok ? '#166534' : '#9A3412',
              fontFamily: fontDisplay, fontWeight: 700, fontSize: 10,
            }}>{it.status}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── App shell ────────────────────────────────────────────
function PorterApp() {
  const [screen, setScreen] = useState('home');
  const nav = s => setScreen(s);
  let content;
  if (screen === 'home') content = <HomeScreen onNav={nav} />;
  else if (screen === 'capture') content = <CaptureScreen onNav={nav} />;
  else if (screen === 'confirm') content = <ConfirmScreen onNav={nav} />;
  else if (screen === 'retrieve') content = <RetrieveScreen onNav={nav} />;
  else content = <HistoryScreen />;

  // Map screen → tab highlight
  const activeTab = ['capture', 'confirm'].includes(screen) ? 'capture' : screen === 'retrieve' ? 'retrieve' : screen === 'history' ? 'history' : 'home';

  return (
    <div style={{ position: 'relative', height: '100%' }}>
      {content}
      <PorterTabBar active={activeTab} onChange={nav} />
    </div>
  );
}

Object.assign(window, { PorterApp, HomeScreen, CaptureScreen, ConfirmScreen, RetrieveScreen, HistoryScreen });
