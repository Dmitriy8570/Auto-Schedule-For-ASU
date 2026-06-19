import React from 'react';
import { useLocation, useNavigate } from 'react-router';
import { 
  Calendar, 
  Settings, 
  History, 
  BarChart3, 
  LogOut
} from 'lucide-react';
import { cn } from './ui/SearchableSelect'; // reusing cn utility

interface SidebarProps {
  onLogout?: () => void;
}

export function Sidebar({ onLogout }: SidebarProps) {
  const location = useLocation();
  const navigate = useNavigate();

  const links = [
    { to: '/schedule', icon: Calendar, label: 'Расписание' },
    { to: '/workload', icon: BarChart3, label: 'Нагрузка' },
    { to: '/history', icon: History, label: 'История' },
    { to: '/properties', icon: Settings, label: 'Ограничения' },
  ];

  const handleLogout = () => {
    if (window.confirm('Вы уверены, что хотите выйти из системы?')) {
      onLogout?.();
    }
  };

  return (
    <aside className="w-64 bg-[rgb(26,77,156)] text-white flex flex-col h-screen sticky top-0 shadow-xl z-20">
      <div className="p-6 border-b border-white/10">
        <h1 className="text-xl font-bold flex items-center gap-2">
          <Calendar className="w-6 h-6" />
          <span>Бюро расписаний</span>
        </h1>
        <p className="text-xs text-white/60 mt-1 pl-8">Университетская система</p>
      </div>

      <nav className="flex-1 p-4 space-y-2 overflow-y-auto">
        {links.map((link) => {
          const isActive = location.pathname.startsWith(link.to);
          return (
            <button
              key={link.to}
              onClick={() => navigate(link.to)}
              className={cn(
                "w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 group text-left",
                isActive 
                  ? "bg-white text-[rgb(26,77,156)] shadow-md font-medium" 
                  : "text-white/80 hover:bg-white/10 hover:text-white"
              )}
            >
              <link.icon className={cn("w-5 h-5", isActive ? "text-[rgb(26,77,156)]" : "text-white/80 group-hover:text-white")} />
              <span>{link.label}</span>
            </button>
          );
        })}
      </nav>

      <div className="p-4 border-t border-white/10">
        <button 
          onClick={handleLogout}
          className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-white/80 hover:bg-red-500/20 hover:text-red-100 transition-all text-left"
        >
          <LogOut className="w-5 h-5" />
          <span>Выйти</span>
        </button>
      </div>
    </aside>
  );
}