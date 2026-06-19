import React, { useState, useRef, useEffect } from 'react';
import { Calendar, ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from './SearchableSelect';

interface DatePickerProps {
  value: Date;
  onChange: (date: Date) => void;
  weekColor?: 'red' | 'blue';
}

export function DatePicker({ value, onChange, weekColor = 'blue' }: DatePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [currentMonth, setCurrentMonth] = useState(new Date(value.getFullYear(), value.getMonth(), 1));
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const formatDate = (date: Date) => {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  };

  const daysInMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 0).getDate();
  const firstDayOfMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1).getDay();
  const adjustedFirstDay = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1; // Monday = 0

  const daysArray: (number | null)[] = [];
  for (let i = 0; i < adjustedFirstDay; i++) {
    daysArray.push(null);
  }
  for (let i = 1; i <= daysInMonth; i++) {
    daysArray.push(i);
  }

  const getWeekNumber = (date: Date) => {
    const startOfYear = new Date(date.getFullYear(), 0, 1);
    const days = Math.floor((date.getTime() - startOfYear.getTime()) / (24 * 60 * 60 * 1000));
    return Math.ceil((days + startOfYear.getDay() + 1) / 7);
  };

  const currentWeek = getWeekNumber(value);

  const handlePrevMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  };

  const handleNextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));
  };

  const handleSelectDay = (day: number) => {
    const newDate = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    onChange(newDate);
    setIsOpen(false);
  };

  const monthNames = [
    'Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь',
    'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'
  ];

  const dayNames = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-200 rounded-lg hover:border-[rgb(26,77,156)] transition-colors"
      >
        <Calendar className="w-4 h-4 text-slate-500" />
        <span className="text-sm font-medium text-slate-700">{formatDate(value)}</span>
        <span className={cn(
          "text-xs px-2 py-0.5 rounded font-medium",
          weekColor === 'red' ? "bg-red-100 text-red-700" : "bg-blue-100 text-blue-700"
        )}>
          Неделя {currentWeek}
        </span>
      </button>

      {isOpen && (
        <div className="absolute top-full mt-2 left-0 z-50 bg-white rounded-xl shadow-2xl border border-slate-200 p-4 w-80 animate-in fade-in slide-in-from-top-2 duration-200">
          {/* Month Navigation */}
          <div className="flex items-center justify-between mb-4">
            <button
              onClick={handlePrevMonth}
              className="p-1 hover:bg-slate-100 rounded transition-colors"
            >
              <ChevronLeft className="w-5 h-5 text-slate-600" />
            </button>
            <span className="font-bold text-slate-800">
              {monthNames[currentMonth.getMonth()]} {currentMonth.getFullYear()}
            </span>
            <button
              onClick={handleNextMonth}
              className="p-1 hover:bg-slate-100 rounded transition-colors"
            >
              <ChevronRight className="w-5 h-5 text-slate-600" />
            </button>
          </div>

          {/* Day Names */}
          <div className="grid grid-cols-7 gap-1 mb-2">
            {dayNames.map((day, idx) => (
              <div
                key={day}
                className={cn(
                  "text-center text-xs font-bold py-1 rounded",
                  weekColor === 'red' && idx < 5 ? "text-red-600 bg-red-50" :
                  weekColor === 'blue' && idx < 5 ? "text-blue-600 bg-blue-50" :
                  "text-slate-500"
                )}
              >
                {day}
              </div>
            ))}
          </div>

          {/* Calendar Days */}
          <div className="grid grid-cols-7 gap-1">
            {daysArray.map((day, idx) => {
              if (day === null) {
                return <div key={`empty-${idx}`} />;
              }

              const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
              const isSelected = date.toDateString() === value.toDateString();
              const isToday = date.toDateString() === new Date().toDateString();
              const weekNum = getWeekNumber(date);

              return (
                <button
                  key={day}
                  onClick={() => handleSelectDay(day)}
                  className={cn(
                    "aspect-square flex items-center justify-center text-sm rounded transition-all",
                    isSelected && "bg-[rgb(26,77,156)] text-white font-bold shadow-md",
                    !isSelected && isToday && "border-2 border-[rgb(26,77,156)] font-bold",
                    !isSelected && !isToday && "hover:bg-slate-100",
                    !isSelected && weekNum % 2 === 0 && weekColor === 'red' && "bg-red-50",
                    !isSelected && weekNum % 2 === 1 && weekColor === 'blue' && "bg-blue-50"
                  )}
                >
                  {day}
                </button>
              );
            })}
          </div>

          {/* Quick Actions */}
          <div className="mt-4 pt-4 border-t border-slate-100">
            <button
              onClick={() => {
                onChange(new Date());
                setIsOpen(false);
              }}
              className="w-full text-xs text-[rgb(26,77,156)] hover:bg-blue-50 py-2 rounded transition-colors font-medium"
            >
              Сегодня
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
