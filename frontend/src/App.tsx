import { useEffect, useState } from 'react';

interface Camera { id: number; cameraId: string; name: string; isActive: boolean; imageUrl: string | null; lastUpdated: string; }
interface NfcLog { id: number; personId: string; fullName: string; type: string; timestamp: string; }
interface SystemNode { id: number; hostname: string; isActive: boolean; lastHeartbeat: string; }
interface AdminLog { id: number; adminUsername: string; action: string; method: string; payload: string; targetUser: string; timestamp: string; }

export default function App() {

  const [user, setUser] = useState<{ username: string; role: string } | null>(() => {
    const saved = localStorage.getItem('user');
    return saved ? JSON.parse(saved) : null;
  });
  

  const [loginUser, setLoginUser] = useState('');
  const [loginPass, setLoginPass] = useState('');
  const [authError, setAuthError] = useState('');


  const [regUser, setRegUser] = useState('');
  const [regPass, setRegPass] = useState('');
  const [regMessage, setRegMessage] = useState({ text: '', isError: false });


  const [cameras, setCameras] = useState<Camera[]>([]);
  const [nfcLogs, setNfcLogs] = useState<NfcLog[]>([]);
  const [systemNodes, setSystemNodes] = useState<SystemNode[]>([]);
  const [adminLogs, setAdminLogs] = useState<AdminLog[]>([]);
  const [insideCount, setInsideCount] = useState<number>(0);
  

  const [activeTab, setActiveTab] = useState<'dashboard' | 'admin-logs' | 'register'>('dashboard');

  const API_URL = 'http://localhost:8080/api';


  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setAuthError('');
    try {
      const res = await fetch('http://localhost:8080/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: loginUser, password: loginPass })
      });
      if (!res.ok) {
        const errData = await res.json();
        throw new Error(errData.message || "Giriş başarısız!");
      }
      const data = await res.json();
      localStorage.setItem('user', JSON.stringify(data));
      setUser(data);
    } catch (err: any) {
      setAuthError(err.message);
    }
  };


  const handleLogout = () => {
    localStorage.removeItem('user');
    setUser(null);
    setActiveTab('dashboard');
  };


  const handleRegisterYetkili = async (e: React.FormEvent) => {
    e.preventDefault();
    setRegMessage({ text: '', isError: false });
    try {
      const res = await fetch('http://localhost:8080/api/auth/register-yetkili', {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'X-User-Role': user?.role || '',
          'X-Admin-User': user?.username || ''
        },
        body: JSON.stringify({ username: regUser, password: regPass })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Kayıt başarısız!");
      
      setRegMessage({ text: data.message, isError: false });
      setRegUser('');
      setRegPass('');
    } catch (err: any) {
      setRegMessage({ text: err.message, isError: true });
    }
  };

  // Panel Verilerini Çekme
  const fetchData = async () => {
    if (!user) return;
    try {
      const headers: HeadersInit = {
        'X-User-Role': user.role,
        'X-Admin-User': user.username
      };

      const camRes = await fetch(`${API_URL}/cameras`, { headers });
      setCameras(await camRes.json());

      const nfcRes = await fetch(`${API_URL}/nfc-logs`, { headers });
      setNfcLogs(await nfcRes.json());

      const insideRes = await fetch(`${API_URL}/nfc/inside-count`, { headers });
      const insideData = await insideRes.json();
      setInsideCount(insideData.totalInside);

      const sysRes = await fetch(`${API_URL}/system-nodes`, { headers });
      setSystemNodes(await sysRes.json());

      const adminRes = await fetch(`${API_URL}/admin-logs`, { headers });
      setAdminLogs(await adminRes.json());
    } catch (err) {
      console.error("Veri senkronizasyon hatası:", err);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 3000);
    return () => clearInterval(interval);
  }, [user]);


  if (!user) {
    return (
      <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4 font-mono">
        <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-xl p-8 shadow-2xl relative overflow-hidden">
          <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r stream-gradient from-emerald-500 via-sky-500 to-emerald-500"></div>
          <div className="text-center mb-8">
            <h2 className="text-xl font-bold tracking-widest text-emerald-400">🛡️ CYBER-WATCHER // SECURE AUTH</h2>
            <p className="text-xs text-slate-500 mt-2">Siber Simülasyon SOC Paneline Giriş Yapın</p>
          </div>

          <form onSubmit={handleLogin} className="space-y-5">
            <div>
              <label className="block text-xs text-slate-400 uppercase mb-2 font-bold">Kullanıcı Adı</label>
              <input 
                type="text" required value={loginUser} onChange={e => setLoginUser(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded px-4 py-2.5 text-sm text-emerald-400 focus:outline-none focus:border-emerald-500/50 font-mono"
                placeholder="admin"
              />
            </div>
            <div>
              <label className="block text-xs text-slate-400 uppercase mb-2 font-bold">Parola</label>
              <input 
                type="password" required value={loginPass} onChange={e => setLoginPass(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded px-4 py-2.5 text-sm text-emerald-400 focus:outline-none focus:border-emerald-500/50 font-mono"
                placeholder="••••••••"
              />
            </div>

            {authError && (
              <div className="p-3 bg-rose-500/10 border border-rose-500/20 rounded text-rose-400 text-xs text-center">
                ⚠️ {authError}
              </div>
            )}

            <button type="submit" className="w-full bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-bold py-3 rounded text-sm transition font-mono uppercase tracking-wider">
              Sisteme Bağlan
            </button>
          </form>
          <div className="text-center mt-4"><span className="text-[10px] text-slate-600">Default Admin: admin / admin!</span></div>
        </div>
      </div>
    );
  }

  // --- ANA SOC PANELİ ---
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans">
      {/* Üst Menü Barı */}
      <header className="border-b border-slate-800 bg-slate-900/50 p-4 backdrop-blur">
        <div className="max-w-7xl mx-auto flex flex-col md:flex-row justify-between items-center gap-4">
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold tracking-wider text-emerald-400 font-mono">🛡️ CYBER-WATCHER // SOC PANEL</h1>
            <span className="px-2 py-0.5 bg-slate-800 border border-slate-700 rounded text-xs font-mono text-slate-400 uppercase">
              {user.role} MODE
            </span>
          </div>
          <div className="flex flex-wrap gap-3 font-mono text-xs">
            <button onClick={() => setActiveTab('dashboard')} className={`px-3 py-1.5 rounded transition ${activeTab === 'dashboard' ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/50' : 'text-slate-400 hover:text-white'}`}>
              [ İzleme Paneli ]
            </button>
            <button onClick={() => setActiveTab('admin-logs')} className={`px-3 py-1.5 rounded transition ${activeTab === 'admin-logs' ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/50' : 'text-slate-400 hover:text-white'}`}>
              [ Admin Audit Logları ]
            </button>
            
            {/* Sadece ADMIN rolündekiler bu sekmeyi görebilir (RBAC Front-End Guard) */}
            {user.role === 'ADMIN' && (
              <button onClick={() => setActiveTab('register')} className={`px-3 py-1.5 rounded transition ${activeTab === 'register' ? 'bg-amber-500/20 text-amber-400 border border-amber-500/50' : 'text-slate-400 hover:text-white'}`}>
                [ + Yetkili Personel Ekle ]
              </button>
            )}

            <button onClick={handleLogout} className="px-3 py-1.5 rounded bg-rose-950/40 text-rose-400 border border-rose-900/50 hover:bg-rose-900/40 transition">
              [ Güvenli Çıkış ({user.username}) ]
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto p-6">
        {activeTab === 'dashboard' ? (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            
            {/* SOL BLOK: Kameralar ve UDP Makineler */}
            <div className="lg:col-span-2 space-y-6">
              <h2 className="text-lg font-semibold font-mono border-b border-slate-800 pb-2 text-slate-400">📹 Kamera Kontrol Sistemi</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {cameras.map(cam => (
                  <div key={cam.id} className="bg-slate-900 border border-slate-800 rounded-lg p-4 flex flex-col justify-between">
                    <div className="flex justify-between items-center mb-3">
                      <span className="font-mono text-sm font-bold text-slate-300">{cam.name}</span>
                      <span className={`px-2 py-0.5 rounded text-[10px] font-mono font-bold ${cam.isActive ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-rose-500/10 text-rose-400 border border-rose-500/20'}`}>
                        {cam.isActive ? 'ONLINE' : 'OFFLINE'}
                      </span>
                    </div>
                    {cam.isActive && cam.imageUrl ? (
                      <img src={cam.imageUrl} alt={cam.name} className="w-full h-32 object-cover rounded border border-slate-800 brightness-75" />
                    ) : (
                      <div className="w-full h-32 bg-slate-950 rounded border border-slate-800/80 flex items-center justify-center text-slate-600 font-mono text-xs">
                        [ SİNYAL YOK // KANAL KESİLDİ ]
                      </div>
                    )}
                  </div>
                ))}
              </div>

              <h2 className="text-lg font-semibold font-mono border-b border-slate-800 pt-4 pb-2 text-slate-400">💻 Linux Sunucu Altyapısı (UDP Sinyalleri)</h2>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {systemNodes.map(node => (
                  <div key={node.id} className="bg-slate-900 border border-slate-800 rounded-lg p-3 text-center transition hover:border-slate-700">
                    <div className="font-mono text-xs text-slate-400 mb-1">{node.hostname}</div>
                    <div className={`text-xs font-bold font-mono ${node.isActive ? 'text-emerald-400' : 'text-slate-600'}`}>
                      {node.isActive ? '● ACTIVE' : '○ DEAD'}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* SAĞ BLOK: NFC Geçiş Logları ve Kişi Sayacı */}
            <div className="bg-slate-900 border border-slate-800 rounded-lg p-4 h-[calc(100vh-140px)] flex flex-col">
              <div className="flex justify-between items-center border-b border-slate-800 pb-3 mb-4">
                <h2 className="font-semibold font-mono text-slate-400">📟 NFC Geçiş Kontrol</h2>
                <div className="text-right">
                  <span className="text-[10px] font-mono text-slate-500 block">İçerideki Kişi</span>
                  <span className="text-2xl font-bold font-mono text-emerald-400">{insideCount}</span>
                </div>
              </div>
              <div className="overflow-y-auto flex-1 space-y-2 pr-1 font-mono text-xs">
                {nfcLogs.map(log => (
                  <div key={log.id} className="p-2 rounded bg-slate-950 border border-slate-800/60 flex justify-between items-center">
                    <div>
                      <span className="text-slate-300 block font-medium">{log.fullName}</span>
                      <span className="text-[10px] text-slate-500 font-bold">ID: {log.personId}</span>
                    </div>
                    <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${log.type === 'GIRIS' ? 'bg-emerald-500/10 text-emerald-400' : 'bg-amber-500/10 text-amber-400'}`}>
                      {log.type}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        ) : activeTab === 'admin-logs' ? (
          /* AUDIT LOG TABLOSU */
          <div className="bg-slate-900 border border-slate-800 rounded-lg p-6">
            <h2 className="text-lg font-semibold font-mono border-b border-slate-800 pb-3 mb-4 text-slate-400">🛡️ Middleware Denetim Günlüğü (Audit Logs)</h2>
            <div className="overflow-x-auto font-mono text-xs">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-800 text-slate-400">
                    <th className="p-3">Zaman</th>
                    <th className="p-3">Kullanıcı</th>
                    <th className="p-3">Metot</th>
                    <th className="p-3">İşlem / Endpoint</th>
                    <th className="p-3">Hedef</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/50">
                  {adminLogs.map(log => (
                    <tr key={log.id} className="hover:bg-slate-950/40 transition">
                      <td className="p-3 text-slate-500">{new Date(log.timestamp).toLocaleTimeString()}</td>
                      <td className="p-3 text-amber-400 font-bold">{log.adminUsername}</td>
                      <td className="p-3"><span className={`px-1.5 py-0.5 rounded font-bold ${log.method === 'GET' ? 'bg-sky-500/10 text-sky-400' : 'bg-amber-500/10 text-amber-400'}`}>{log.method}</span></td>
                      <td className="p-3 text-slate-300">{log.action}</td>
                      <td className="p-3 text-slate-400">{log.targetUser}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ) : (
          /* SADECE ADMIN'E ÖZEL YETKİLİ EKLEME FORMU */
          <div className="max-w-md mx-auto bg-slate-900 border border-slate-800 rounded-xl p-6 shadow-xl font-mono">
            <h2 className="text-md font-bold text-amber-400 border-b border-slate-800 pb-3 mb-5">⚙️ YENİ YETKİLİ PERSONEL TANIMLAMA</h2>
            <form onSubmit={handleRegisterYetkili} className="space-y-4">
              <div>
                <label className="block text-xs text-slate-400 uppercase mb-2 font-bold">Yeni Kullanıcı Adı</label>
                <input 
                  type="text" required value={regUser} onChange={e => setRegUser(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded px-3 py-2 text-sm text-emerald-400 focus:outline-none focus:border-amber-500/50"
                  placeholder="isim"
                />
              </div>
              <div>
                <label className="block text-xs text-slate-400 uppercase mb-2 font-bold">Geçici Parola</label>
                <input 
                  type="password" required value={regPass} onChange={e => setRegPass(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded px-3 py-2 text-sm text-emerald-400 focus:outline-none focus:border-amber-500/50"
                  placeholder="••••••••"
                />
              </div>

              {regMessage.text && (
                <div className={`p-3 border rounded text-xs text-center ${regMessage.isError ? 'bg-rose-500/10 border-rose-500/20 text-rose-400' : 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'}`}>
                  {regMessage.text}
                </div>
              )}

              <button type="submit" className="w-full bg-amber-600 hover:bg-amber-500 text-slate-950 font-bold py-2.5 rounded text-xs transition uppercase tracking-wider">
                [ Hesap Oluştur ve Yetkilendir ]
              </button>
            </form>
          </div>
        )}
      </main>
    </div>
  );
}
