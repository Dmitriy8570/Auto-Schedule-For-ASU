import React, { useState } from 'react';
import { MemoryRouter as Router, Routes, Route, Navigate } from 'react-router';
import { Sidebar } from './components/Sidebar';
import { ScheduleEditor } from './components/schedule/ScheduleEditor';
import { WorkloadView } from './components/workload/WorkloadView';
import { HistoryLog } from './components/history/HistoryLog';
import { PropertiesManager } from './components/properties/PropertiesManager';
import { LoginPage } from './components/auth/LoginPage';

// Layout wrapper
function Layout({ children, onLogout }: { children: React.ReactNode; onLogout: () => void }) {
  return (
    <div className="flex min-h-screen bg-slate-50">
      <Sidebar onLogout={onLogout} />
      <main className="flex-1 p-8 h-screen overflow-y-auto">
        {children}
      </main>
    </div>
  );
}

export default function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [currentUser, setCurrentUser] = useState<string | null>(null);

  const handleLogin = (username: string, password: string) => {
    // В реальном приложении здесь будет запрос к серверу
    // Для демонстрации просто устанавливаем аутентификацию
    setIsAuthenticated(true);
    setCurrentUser(username);
  };

  const handleLogout = () => {
    setIsAuthenticated(false);
    setCurrentUser(null);
  };

  // Показываем страницу входа, если пользователь не аутентифицирован
  if (!isAuthenticated) {
    return <LoginPage onLogin={handleLogin} />;
  }

  return (
    <Router>
      <Routes>
        <Route path="/" element={<Navigate to="/schedule" replace />} />
        <Route path="/schedule" element={
          <Layout onLogout={handleLogout}>
            <div className="h-[calc(100vh-4rem)]">
              <ScheduleEditor />
            </div>
          </Layout>
        } />
        <Route path="/workload" element={
          <Layout onLogout={handleLogout}>
            <div className="h-[calc(100vh-4rem)]">
              <WorkloadView />
            </div>
          </Layout>
        } />
        <Route path="/history" element={
          <Layout onLogout={handleLogout}>
            <div className="h-[calc(100vh-4rem)]">
              <HistoryLog />
            </div>
          </Layout>
        } />
        <Route path="/properties" element={
          <Layout onLogout={handleLogout}>
            <div className="h-[calc(100vh-4rem)]">
              <PropertiesManager />
            </div>
          </Layout>
        } />
        {/* Fallback */}
        <Route path="*" element={<Navigate to="/schedule" replace />} />
      </Routes>
    </Router>
  );
}