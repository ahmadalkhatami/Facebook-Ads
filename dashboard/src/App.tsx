import { useEffect, useState } from 'react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area } from 'recharts';
import { LayoutDashboard, TrendingUp, DollarSign, Activity, Bell } from 'lucide-react';
import axios from 'axios';

interface Campaign {
  id: number;
  name: string;
  fbCampaignId: string;
  status: string;
  budget: number;
  spend: number;
  revenue: number;
  roas: number;
  ctr: number;
}

interface Stats {
  totalSpend: number;
  totalRevenue: number;
  averageRoas: number;
  averageCtr: number;
}

function App() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [stats, setStats] = useState<Stats>({
    totalSpend: 12450.50,
    totalRevenue: 34500.00,
    averageRoas: 2.77,
    averageCtr: 1.85
  });

  const chartData = [
    { name: 'Mon', spend: 1200, revenue: 2400 },
    { name: 'Tue', spend: 1500, revenue: 3500 },
    { name: 'Wed', spend: 1100, revenue: 2900 },
    { name: 'Thu', spend: 1800, revenue: 4200 },
    { name: 'Fri', spend: 2000, revenue: 5100 },
    { name: 'Sat', spend: 2200, revenue: 6000 },
    { name: 'Sun', spend: 1900, revenue: 4800 },
  ];

  const mockCampaigns: Campaign[] = [
    { id: 1, name: 'Product A - Viral', fbCampaignId: 'fb_123', status: 'ACTIVE', budget: 500, spend: 1200, revenue: 3500, roas: 2.9, ctr: 2.1 },
    { id: 2, name: 'Product B - Retargeting', fbCampaignId: 'fb_456', status: 'ACTIVE', budget: 200, spend: 450, revenue: 1200, roas: 2.6, ctr: 1.5 },
    { id: 3, name: 'Product C - Test', fbCampaignId: 'fb_789', status: 'KILLED', budget: 100, spend: 250, revenue: 0, roas: 0, ctr: 0.8 },
  ];

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [statsRes, campaignsRes] = await Promise.all([
          axios.get('/api/dashboard/stats'),
          axios.get('/api/dashboard/campaigns')
        ]);
        setStats(statsRes.data);
        setCampaigns(campaignsRes.data);
      } catch (err) {
        console.error("Using mock data due to API error:", err);
        setCampaigns(mockCampaigns);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="dashboard-container">
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <LayoutDashboard size={32} color="#60a5fa" />
          <h1 style={{ margin: 0, fontSize: '1.5rem' }}>SCALPING ENGINE <span style={{ color: '#60a5fa', fontSize: '0.8rem', fontWeight: 400 }}>v2.0</span></h1>
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <button style={{ background: 'rgba(255,255,255,0.1)', border: 'none', padding: '0.5rem', borderRadius: '0.5rem', cursor: 'pointer' }}>
            <Bell size={20} />
          </button>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <div style={{ width: '32px', height: '32px', borderRadius: '50%', background: 'linear-gradient(45deg, #60a5fa, #34d399)' }}></div>
            <span style={{ fontWeight: 500 }}>Admin</span>
          </div>
        </div>
      </header>

      <div className="stats-grid">
        <div className="stat-card">
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <h3>Total Spend</h3>
            <DollarSign size={16} color="#94a3b8" />
          </div>
          <div className="value">${stats.totalSpend.toLocaleString()}</div>
        </div>
        <div className="stat-card">
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <h3>Total Revenue</h3>
            <TrendingUp size={16} color="#34d399" />
          </div>
          <div className="value">${stats.totalRevenue.toLocaleString()}</div>
        </div>
        <div className="stat-card">
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <h3>Average ROAS</h3>
            <Activity size={16} color="#60a5fa" />
          </div>
          <div className="value">{stats.averageRoas}x</div>
        </div>
        <div className="stat-card">
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <h3>Average CTR</h3>
            <Activity size={16} color="#fbbf24" />
          </div>
          <div className="value">{stats.averageCtr}%</div>
        </div>
      </div>

      <div className="chart-container">
        <h3 style={{ marginBottom: '1.5rem', fontSize: '1rem', color: '#94a3b8' }}>Performance Overview</h3>
        <div style={{ width: '100%', height: 300 }}>
          <ResponsiveContainer>
            <AreaChart data={chartData}>
              <defs>
                <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#34d399" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#34d399" stopOpacity={0}/>
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} />
              <YAxis stroke="#94a3b8" fontSize={12} />
              <Tooltip 
                contentStyle={{ background: '#1e293b', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '0.5rem' }}
                itemStyle={{ color: '#fff' }}
              />
              <Area type="monotone" dataKey="revenue" stroke="#34d399" fillOpacity={1} fill="url(#colorRevenue)" />
              <Area type="monotone" dataKey="spend" stroke="#60a5fa" fill="transparent" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="table-container">
        <h3 style={{ padding: '1.5rem', margin: 0, fontSize: '1rem', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>Active Campaigns</h3>
        <table>
          <thead>
            <tr>
              <th>Campaign Name</th>
              <th>Status</th>
              <th>Budget</th>
              <th>Spend</th>
              <th>Revenue</th>
              <th>ROAS</th>
              <th>CTR</th>
            </tr>
          </thead>
          <tbody>
            {campaigns.map(c => (
              <tr key={c.id}>
                <td style={{ fontWeight: 500 }}>{c.name}</td>
                <td>
                  <span className={`status-badge ${c.status === 'ACTIVE' ? 'status-active' : 'status-killed'}`}>
                    {c.status}
                  </span>
                </td>
                <td>${c.budget}</td>
                <td>${c.spend}</td>
                <td style={{ color: '#34d399', fontWeight: 600 }}>${c.revenue}</td>
                <td>{c.roas}x</td>
                <td>{c.ctr}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default App
