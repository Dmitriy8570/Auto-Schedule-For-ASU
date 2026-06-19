import React from 'react';
import {
  Plus, Trash2, Info, Globe, Lock,
  AlignCenter, RefreshCw, ArrowLeftRight, Calendar,
  AlignJustify, Layers, Rows3, Waves
} from 'lucide-react';

export type DistributionPattern =
  | 'min_days_between'
  | 'max_days_between'
  | 'same_day'
  | 'consecutive'
  | 'evenly_spread'
  | 'front_loaded'
  | 'no_consecutive'
  | 'spread_week';

export interface DistributionRule {
  id: string;
  pattern: DistributionPattern;
  value: number;
  weight: number;   // 0–100%
  scope: 'global' | 'local';
}

const PATTERN_META: Record<
  DistributionPattern,
  { label: string; desc: string; hasValue: boolean; valueLabel?: string; icon: React.FC<{ className?: string }> }
> = {
  min_days_between: { label: 'Мин. дней между занятиями', desc: 'Не размещать повторно раньше чем через N дней', hasValue: true, valueLabel: 'дн.', icon: ArrowLeftRight },
  max_days_between: { label: 'Макс. дней между занятиями', desc: 'Не допускать разрыва более N дней', hasValue: true, valueLabel: 'дн.', icon: ArrowLeftRight },
  same_day:         { label: 'Всегда в один и тот же день', desc: 'Все занятия дисциплины — строго в один день недели', hasValue: false, icon: Calendar },
  consecutive:      { label: 'Только подряд (парами)', desc: 'Занятия идут без разрывов — пара за парой', hasValue: false, icon: AlignJustify },
  evenly_spread:    { label: 'Равномерно по неделе', desc: 'Равный интервал между всеми занятиями в недельной сетке', hasValue: false, icon: Waves },
  front_loaded:     { label: 'Фронтзагрузка (нач. недели)', desc: 'Концентрировать занятия в пн–ср', hasValue: false, icon: Rows3 },
  no_consecutive:   { label: 'Запрет двух пар подряд', desc: 'Между занятиями должен быть хотя бы один свободный слот', hasValue: false, icon: Layers },
  spread_week:      { label: 'Разнести по разным неделям', desc: 'Не более одного занятия в той же неделе семестра', hasValue: false, icon: AlignCenter },
};

const weightLabel = (w: number) =>
  w >= 85 ? 'Критично' : w >= 60 ? 'Важно' : w >= 35 ? 'Желательно' : 'Пожелание';

const weightColor = (w: number) =>
  w >= 85 ? { track: 'rgb(239,68,68)', pill: 'bg-red-100 text-red-700 border-red-200' }
  : w >= 60 ? { track: 'rgb(245,158,11)', pill: 'bg-amber-100 text-amber-700 border-amber-200' }
  : w >= 35 ? { track: 'rgb(59,130,246)', pill: 'bg-blue-100 text-blue-700 border-blue-200' }
  :           { track: 'rgb(34,197,94)',  pill: 'bg-green-100 text-green-700 border-green-200' };

interface Props {
  rules: DistributionRule[];
  onChange: (rules: DistributionRule[]) => void;
}

export function DistributionRules({ rules, onChange }: Props) {
  const add = () =>
    onChange([
      ...rules,
      { id: Date.now().toString(), pattern: 'min_days_between', value: 2, weight: 70, scope: 'local' },
    ]);

  const remove = (id: string) => onChange(rules.filter(r => r.id !== id));
  const update = (id: string, patch: Partial<DistributionRule>) =>
    onChange(rules.map(r => (r.id === id ? { ...r, ...patch } : r)));

  return (
    <div className="space-y-3">
      {rules.length === 0 && (
        <div className="text-center py-8 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
          <RefreshCw className="w-7 h-7 mx-auto mb-2 text-slate-300" />
          <p>Нет правил распределения</p>
          <p className="text-[11px] mt-1 text-slate-300">Добавьте паттерн, чтобы управлять расстановкой занятий</p>
        </div>
      )}

      {rules.map(rule => {
        const meta = PATTERN_META[rule.pattern];
        const PatternIcon = meta.icon;
        const wc = weightColor(rule.weight);

        return (
          <div
            key={rule.id}
            className="border border-slate-200 rounded-xl p-3 bg-white space-y-2.5 hover:border-[rgb(26,77,156)]/30 hover:shadow-sm transition-all"
          >
            {/* Row 1: pattern + scope + delete */}
            <div className="flex items-center gap-2">
              <span className="p-1.5 bg-[rgb(26,77,156)]/8 rounded-md shrink-0">
                <PatternIcon className="w-3.5 h-3.5 text-[rgb(26,77,156)]" />
              </span>
              <select
                value={rule.pattern}
                onChange={e => update(rule.id, { pattern: e.target.value as DistributionPattern })}
                className="flex-1 text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/40 text-slate-700"
              >
                {(Object.entries(PATTERN_META) as [DistributionPattern, typeof PATTERN_META[DistributionPattern]][]).map(
                  ([val, m]) => <option key={val} value={val}>{m.label}</option>
                )}
              </select>

              <button
                onClick={() => update(rule.id, { scope: rule.scope === 'global' ? 'local' : 'global' })}
                title={rule.scope === 'global' ? 'Глобальное (весь отдел)' : 'Локальное (только этот объект)'}
                className={`flex items-center gap-1 text-[10px] px-2 py-1 rounded-md border font-medium transition-all shrink-0 ${
                  rule.scope === 'global'
                    ? 'bg-indigo-50 border-indigo-200 text-indigo-600 hover:bg-indigo-100'
                    : 'bg-slate-50 border-slate-200 text-slate-500 hover:bg-slate-100'
                }`}
              >
                {rule.scope === 'global' ? <Globe className="w-3 h-3" /> : <Lock className="w-3 h-3" />}
                {rule.scope === 'global' ? 'Глоб.' : 'Лок.'}
              </button>

              <button
                onClick={() => remove(rule.id)}
                className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors shrink-0"
              >
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            </div>

            {/* Description */}
            <p className="text-[10px] text-slate-400 flex items-center gap-1 pl-8">
              <Info className="w-3 h-3 shrink-0 text-slate-300" />
              {meta.desc}
            </p>

            {/* Value if applicable */}
            {meta.hasValue && (
              <div className="flex items-center gap-2 pl-8">
                <span className="text-[11px] text-slate-500">Значение:</span>
                <input
                  type="number"
                  min={1}
                  max={14}
                  value={rule.value}
                  onChange={e => update(rule.id, { value: Number(e.target.value) })}
                  className="w-16 text-xs border border-slate-200 rounded-md px-2 py-1 text-center focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/40"
                />
                <span className="text-[11px] text-slate-400">{meta.valueLabel}</span>
              </div>
            )}

            {/* Weight slider */}
            <div className="pl-8">
              <div className="flex items-center justify-between mb-1.5">
                <span className="text-[10px] text-slate-500">Вес ограничения</span>
                <div className="flex items-center gap-1.5">
                  <span className={`text-[10px] font-semibold px-1.5 py-0.5 rounded border ${wc.pill}`}>
                    {rule.weight}% · {weightLabel(rule.weight)}
                  </span>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-[9px] text-slate-400 w-12 shrink-0">Пожелание</span>
                <input
                  type="range"
                  min={0}
                  max={100}
                  step={5}
                  value={rule.weight}
                  onChange={e => update(rule.id, { weight: Number(e.target.value) })}
                  className="flex-1 h-1.5 rounded-full appearance-none cursor-pointer"
                  style={{
                    background: `linear-gradient(to right, ${wc.track} 0%, ${wc.track} ${rule.weight}%, #e2e8f0 ${rule.weight}%, #e2e8f0 100%)`,
                  }}
                />
                <span className="text-[9px] text-slate-400 w-12 text-right shrink-0">Критично</span>
              </div>
            </div>
          </div>
        );
      })}

      <button
        onClick={add}
        className="w-full flex items-center justify-center gap-2 border-2 border-dashed border-[rgb(26,77,156)]/30 text-[rgb(26,77,156)] rounded-xl py-2.5 text-xs font-medium hover:bg-[rgb(26,77,156)]/5 hover:border-[rgb(26,77,156)]/50 transition-all"
      >
        <Plus className="w-3.5 h-3.5" />
        Добавить паттерн распределения
      </button>
    </div>
  );
}
