import React, { useState } from 'react';
import { Calendar, Lock, User, Eye, EyeOff, GraduationCap } from 'lucide-react';

interface LoginPageProps {
  onLogin: (username: string, password: string) => void;
}

export function LoginPage({ onLogin }: LoginPageProps) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!username || !password) {
      setError('Заполните все поля');
      return;
    }

    // Простая проверка для демонстрации
    // В реальном приложении здесь будет запрос к серверу
    if (username.length >= 3 && password.length >= 3) {
      onLogin(username, password);
    } else {
      setError('Неверное имя пользователя или пароль');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-[rgb(26,77,156)] via-[rgb(35,95,180)] to-[rgb(20,60,130)] flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Logo and Title */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-20 h-20 bg-white rounded-2xl shadow-2xl mb-4">
            <Calendar className="w-10 h-10 text-[rgb(26,77,156)]" />
          </div>
          <h1 className="text-3xl font-bold text-white mb-2">
            Система расписаний
          </h1>
          <p className="text-blue-100 text-sm flex items-center justify-center gap-2">
            <GraduationCap className="w-4 h-4" />
            Бюро расписаний университета
          </p>
        </div>

        {/* Login Form */}
        <div className="bg-white rounded-2xl shadow-2xl p-8">
          <h2 className="text-2xl font-bold text-slate-800 mb-6">Вход в систему</h2>
          
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Username Field */}
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-slate-700 mb-2">
                Имя пользователя
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-slate-400" />
                </div>
                <input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => {
                    setUsername(e.target.value);
                    setError('');
                  }}
                  className="block w-full pl-10 pr-3 py-3 border border-slate-200 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)] focus:border-transparent transition-all"
                  placeholder="Введите имя пользователя"
                  autoComplete="username"
                />
              </div>
            </div>

            {/* Password Field */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-slate-700 mb-2">
                Пароль
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Lock className="h-5 w-5 text-slate-400" />
                </div>
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    setError('');
                  }}
                  className="block w-full pl-10 pr-12 py-3 border border-slate-200 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)] focus:border-transparent transition-all"
                  placeholder="Введите пароль"
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 transition-colors"
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5" />
                  ) : (
                    <Eye className="h-5 w-5" />
                  )}
                </button>
              </div>
            </div>

            {/* Error Message */}
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm animate-in slide-in-from-top-2">
                {error}
              </div>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              className="w-full bg-[rgb(26,77,156)] text-white py-3 px-4 rounded-lg font-medium hover:bg-[rgb(20,60,130)] focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)] focus:ring-offset-2 transition-all shadow-lg hover:shadow-xl active:scale-[0.98] transform"
            >
              Войти
            </button>
          </form>

          {/* Demo Credentials */}
          <div className="mt-6 pt-6 border-t border-slate-100">
            <div className="bg-blue-50 rounded-lg p-4 border border-blue-100">
              <p className="text-xs text-blue-800 font-medium mb-2">
                💡 Демо-доступ:
              </p>
              <div className="text-xs text-blue-700 space-y-1">
                <p>• Логин: <span className="font-mono bg-white px-2 py-0.5 rounded">admin</span></p>
                <p>• Пароль: <span className="font-mono bg-white px-2 py-0.5 rounded">admin</span></p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center mt-6 text-blue-100 text-xs">
          <p>© 2026 Бюро расписаний университета</p>
        </div>
      </div>
    </div>
  );
}
