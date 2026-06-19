import React from 'react';
import {
  Briefcase, Clock, Moon, Sun, Sunset, Star,
  ChevronUp, ChevronDown, Shield, AlertTriangle, CalendarOff
} from 'lucide-react';

export type ContractType = 'full_time' | 'part_time' | 'external' | 'hourly';
export type ShiftType = 'morning' | 'afternoon' | 'evening';

export interface ContractConfig {
  contractType: ContractType;
  maxHoursPerDay: number;
  maxHoursPerWeek: number;
  minRestHours: number;
  priorityRank: number;     // 1–10: higher = preferred slots first
  allowedShifts: ShiftType[];
  fixedDaysOff: number[];   // 0=Mon .. 5=Sat
  notes: string;
}

export const DEFAULT_CONTRACT: ContractConfig = {
  contractType: 'full_time',
  maxHoursPerDay: 8,
  maxHoursPerWeek: 36,
  minRestHours: 12,
  priorityRank: 5,
  allowedShifts: ['morning', 'afternoon'],
  fixedDaysOff: [],
  notes: '',
};

const CONTRACT_META: Record<ContractType, { label: string; color: string; badge: string }> = {
  full_time: { label: 'Штатный (полная ставка)',       color: 'bg-green-100 text-green-700 border-green-200',   badge: '1.0 ст.' },
  part_time: { label: 'Совместитель (0.5 ставки)',     color: 'bg-blue-100 text-blue-700 border-blue-200',      badge: '0.5 ст.' },
  external:  { label: 'Внешний совместитель',          color: 'bg-amber-100 text-amber-700 border-amber-200',   badge: 'Внеш.' },
  hourly:    { label: 'Почасовая оплата',              color: 'bg-purple-100 text-purple-700 border-purple-200', badge: 'Час.' },
};

const SHIFT_META: Record<ShiftType, { label: string; icon: React.FC<{ className?: string }>; time: string }> = {
  morning:   { label: 'Утренняя',   icon: Sun,    time: '08:00–13:00' },
  afternoon: { label: 'Дневная',    icon: Sunset, time: '13:00–18:00' },
  evening:   { label: 'Вечерняя',   icon: Moon,   time: '18:00–21:00' },
};

const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];

const priorityLabel = (r: number) =>
  r >= 9 ? 'Заведующий кафедрой'
  : r >= 7 ? 'Ст. преподаватель'
  : r >= 5 ? 'Преподаватель'
  : r >= 3 ? 'Ассистент'
  : 'Почасовик';

function Counter({
  label,
  value,
  unit,
  min,
  max,
  onChange,
  warning,
}: {
  label: string;
  value: number;
  unit: string;
  min: number;
  max: number;
  onChange: (v: number) => void;
  warning?: string;
}) {
  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <span className="text-[11px] text-slate-600">{label}</span>
        {warning && (
          <span className="text-[10px] text-amber-600 flex items-center gap-0.5">
            <AlertTriangle className="w-3 h-3" />
            {warning}
          </span>
        )}
      </div>
      <div className="flex items-center gap-2">
        <button
          onClick={() => onChange(Math.max(min, value - 1))}
          className="w-7 h-7 rounded-lg bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-700 transition-colors"
        >
          <ChevronDown className="w-4 h-4" />
        </button>
        <span className="w-12 text-center text-sm font-semibold text-slate-800">
          {value}<span className="text-[10px] text-slate-400 font-normal ml-0.5">{unit}</span>
        </span>
        <button
          onClick={() => onChange(Math.min(max, value + 1))}
          className="w-7 h-7 rounded-lg bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-700 transition-colors"
        >
          <ChevronUp className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

interface Props {
  contract: ContractConfig;
  onChange: (c: ContractConfig) => void;
}

export function ContractModule({ contract: c, onChange }: Props) {
  const update = <K extends keyof ContractConfig>(key: K, value: ContractConfig[K]) =>
    onChange({ ...c, [key]: value });

  const toggleShift = (shift: ShiftType) => {
    const has = c.allowedShifts.includes(shift);
    if (has && c.allowedShifts.length <= 1) return; // keep at least one
    update('allowedShifts', has ? c.allowedShifts.filter(s => s !== shift) : [...c.allowedShifts, shift]);
  };

  const toggleDay = (day: number) => {
    const has = c.fixedDaysOff.includes(day);
    update('fixedDaysOff', has ? c.fixedDaysOff.filter(d => d !== day) : [...c.fixedDaysOff, day]);
  };

  const meta = CONTRACT_META[c.contractType];
  const isExternal = c.contractType === 'external' || c.contractType === 'hourly';
  const weekWarning = c.maxHoursPerWeek < 8 ? 'Очень мало' : isExternal && c.maxHoursPerWeek > 18 ? '> 18 ч — лимит совм.' : undefined;

  return (
    <div className="space-y-5">

      {/* Contract type */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-2">
          Тип контракта
        </label>
        <div className="grid grid-cols-2 gap-2">
          {(Object.entries(CONTRACT_META) as [ContractType, typeof CONTRACT_META[ContractType]][]).map(([type, m]) => (
            <button
              key={type}
              onClick={() => update('contractType', type)}
              className={`flex items-center gap-2 p-2.5 rounded-xl border text-left transition-all ${
                c.contractType === type
                  ? `${m.color} shadow-sm`
                  : 'bg-white border-slate-200 text-slate-500 hover:bg-slate-50'
              }`}
            >
              <Briefcase className="w-3.5 h-3.5 shrink-0" />
              <div>
                <div className="text-[11px] font-semibold leading-tight">{m.badge}</div>
                <div className="text-[10px] opacity-70 leading-tight">{m.label.split(' (')[0]}</div>
              </div>
            </button>
          ))}
        </div>
        <p className="text-[11px] text-slate-400 mt-1.5 flex items-center gap-1">
          <Shield className="w-3 h-3" />
          {meta.label}
          {isExternal && (
            <span className="ml-2 text-amber-600 font-medium">· Лимит: 18 ч/нед (ТК РФ ст. 284)</span>
          )}
        </p>
      </div>

      {/* Hours limits */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-3">
          Лимиты рабочего времени
        </label>
        <div className="grid grid-cols-3 gap-4 bg-slate-50 rounded-xl p-4 border border-slate-100">
          <Counter label="Макс. ч/день" value={c.maxHoursPerDay} unit="ч" min={1} max={16} onChange={v => update('maxHoursPerDay', v)} />
          <Counter label="Макс. ч/неделю" value={c.maxHoursPerWeek} unit="ч" min={1} max={60} onChange={v => update('maxHoursPerWeek', v)} warning={weekWarning} />
          <Counter label="Мин. отдых" value={c.minRestHours} unit="ч" min={8} max={24} onChange={v => update('minRestHours', v)} />
        </div>
        {isExternal && c.maxHoursPerWeek > 18 && (
          <div className="mt-2 p-2.5 bg-amber-50 border border-amber-200 rounded-lg flex items-start gap-2">
            <AlertTriangle className="w-3.5 h-3.5 text-amber-600 shrink-0 mt-0.5" />
            <p className="text-[11px] text-amber-700">
              Для внешних совместителей лимит 18 ч/нед по ТК РФ. Превышение будет помечено как нарушение.
            </p>
          </div>
        )}
      </div>

      {/* Priority rank */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-2">
          Приоритет при составлении расписания
        </label>
        <div className="bg-slate-50 rounded-xl p-3 border border-slate-100">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-1.5">
              <Star className="w-3.5 h-3.5 text-amber-400" />
              <span className="text-xs font-semibold text-slate-700">{priorityLabel(c.priorityRank)}</span>
            </div>
            <span className="text-[11px] bg-amber-50 text-amber-700 border border-amber-200 px-2 py-0.5 rounded-full font-bold">
              Ранг {c.priorityRank}/10
            </span>
          </div>
          <input
            type="range"
            min={1}
            max={10}
            value={c.priorityRank}
            onChange={e => update('priorityRank', Number(e.target.value))}
            className="w-full h-2 rounded-full appearance-none cursor-pointer"
            style={{
              background: `linear-gradient(to right, rgb(245,158,11) 0%, rgb(245,158,11) ${(c.priorityRank - 1) * 11.1}%, #e2e8f0 ${(c.priorityRank - 1) * 11.1}%, #e2e8f0 100%)`,
            }}
          />
          <div className="flex justify-between mt-1">
            <span className="text-[9px] text-slate-400">Почасовик</span>
            <span className="text-[9px] text-slate-400">Зав. кафедрой</span>
          </div>
          <p className="text-[10px] text-slate-400 mt-2">
            Преподаватели с более высоким рангом получают предпочтительные слоты при конкурентном назначении.
          </p>
        </div>
      </div>

      {/* Allowed shifts */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-2">
          Разрешённые смены
        </label>
        <div className="flex gap-2">
          {(Object.entries(SHIFT_META) as [ShiftType, typeof SHIFT_META[ShiftType]][]).map(([shift, m]) => {
            const active = c.allowedShifts.includes(shift);
            const ShiftIcon = m.icon;
            return (
              <button
                key={shift}
                onClick={() => toggleShift(shift)}
                className={`flex-1 flex flex-col items-center gap-1 py-3 rounded-xl border transition-all ${
                  active
                    ? 'bg-[rgb(26,77,156)] border-[rgb(26,77,156)] text-white shadow-sm'
                    : 'bg-white border-slate-200 text-slate-400 hover:bg-slate-50'
                }`}
              >
                <ShiftIcon className="w-4 h-4" />
                <span className="text-[11px] font-medium">{m.label}</span>
                <span className={`text-[9px] ${active ? 'text-blue-200' : 'text-slate-400'}`}>{m.time}</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Fixed days off */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
          <CalendarOff className="w-3.5 h-3.5" />
          Фиксированные выходные
        </label>
        <div className="flex gap-1.5">
          {DAYS.map((day, idx) => {
            const off = c.fixedDaysOff.includes(idx);
            return (
              <button
                key={idx}
                onClick={() => toggleDay(idx)}
                className={`flex-1 py-2 rounded-lg border text-[11px] font-medium transition-all ${
                  off
                    ? 'bg-red-100 border-red-300 text-red-700'
                    : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                {day}
              </button>
            );
          })}
        </div>
        {c.fixedDaysOff.length > 0 && (
          <p className="text-[10px] text-slate-400 mt-1">
            Занятия в эти дни будут заблокированы (жёсткое ограничение).
          </p>
        )}
      </div>

      {/* Notes */}
      <div>
        <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">
          Примечания к контракту
        </label>
        <textarea
          value={c.notes}
          onChange={e => update('notes', e.target.value)}
          rows={2}
          placeholder="Например: ставка 0.75, договор до 31 июля 2026..."
          className="w-full text-xs border border-slate-200 rounded-xl px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30 focus:border-[rgb(26,77,156)]/60 text-slate-700 placeholder-slate-300"
        />
      </div>

    </div>
  );
}
