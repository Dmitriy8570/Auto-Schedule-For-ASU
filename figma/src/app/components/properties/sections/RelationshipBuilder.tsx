import React, { useState } from 'react';
import {
  Plus, Trash2, Link2, ArrowRight, Clock,
  Layers, ArrowDown, Users, BookOpen, SwitchCamera,
} from 'lucide-react';

export type RelType =
  | 'must_after'
  | 'must_before'
  | 'simultaneous'
  | 'same_day_after'
  | 'not_concurrent';

export interface ActivityLink {
  id: string;
  parentName: string;
  childName: string;
  relationship: RelType;
  gapMinutes: number;
  simultaneousGroups: string[];
  sharedRooms: boolean;
}

const REL_META: Record<RelType, { label: string; color: string; icon: React.FC<{ className?: string }> }> = {
  must_after:     { label: 'Только после…',       color: 'bg-blue-100 text-blue-700 border-blue-200',   icon: ArrowDown },
  must_before:    { label: 'Только до…',          color: 'bg-indigo-100 text-indigo-700 border-indigo-200', icon: ArrowRight },
  simultaneous:   { label: 'Одновременно с…',     color: 'bg-purple-100 text-purple-700 border-purple-200', icon: Layers },
  same_day_after: { label: 'В тот же день, после…', color: 'bg-teal-100 text-teal-700 border-teal-200', icon: Clock },
  not_concurrent: { label: 'Не одновременно с…',  color: 'bg-red-100 text-red-700 border-red-200',     icon: SwitchCamera },
};

const GROUPS_PRESET = ['ИВТ-301', 'ИВТ-302', 'ПИ-201', 'КБ-101', 'ЭК-101', 'ИВТ-М101'];

interface Props {
  links: ActivityLink[];
  onChange: (links: ActivityLink[]) => void;
}

export function RelationshipBuilder({ links, onChange }: Props) {
  const [newGroupInput, setNewGroupInput] = useState<Record<string, string>>({});

  const add = () =>
    onChange([
      ...links,
      {
        id: Date.now().toString(),
        parentName: '',
        childName: '',
        relationship: 'must_after',
        gapMinutes: 0,
        simultaneousGroups: [],
        sharedRooms: false,
      },
    ]);

  const remove = (id: string) => onChange(links.filter(l => l.id !== id));
  const update = (id: string, patch: Partial<ActivityLink>) =>
    onChange(links.map(l => (l.id === id ? { ...l, ...patch } : l)));

  const addGroup = (id: string, link: ActivityLink) => {
    const g = (newGroupInput[id] ?? '').trim();
    if (!g || link.simultaneousGroups.includes(g)) return;
    update(id, { simultaneousGroups: [...link.simultaneousGroups, g] });
    setNewGroupInput(prev => ({ ...prev, [id]: '' }));
  };

  const removeGroup = (linkId: string, g: string, link: ActivityLink) =>
    update(linkId, { simultaneousGroups: link.simultaneousGroups.filter(x => x !== g) });

  return (
    <div className="space-y-4">
      {links.length === 0 && (
        <div className="text-center py-8 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
          <Link2 className="w-7 h-7 mx-auto mb-2 text-slate-300" />
          <p>Нет связок активностей</p>
          <p className="text-[11px] mt-1 text-slate-300">
            Добавьте зависимость «родитель → потомок» между занятиями
          </p>
        </div>
      )}

      {links.map(link => {
        const meta = REL_META[link.relationship];
        const RelIcon = meta.icon;
        const isSimultaneous = link.relationship === 'simultaneous';

        return (
          <div
            key={link.id}
            className="border border-slate-200 rounded-xl overflow-hidden bg-white hover:shadow-sm transition-shadow"
          >
            {/* Header */}
            <div className={`px-3 py-2 flex items-center gap-2 border-b border-slate-100`}>
              <RelIcon className="w-3.5 h-3.5 text-slate-500 shrink-0" />
              <select
                value={link.relationship}
                onChange={e => update(link.id, { relationship: e.target.value as RelType })}
                className="flex-1 text-xs border border-slate-200 rounded-md px-2 py-1.5 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/40"
              >
                {(Object.entries(REL_META) as [RelType, typeof REL_META[RelType]][]).map(([v, m]) => (
                  <option key={v} value={v}>{m.label}</option>
                ))}
              </select>
              <span className={`text-[10px] px-2 py-0.5 rounded-full border font-medium shrink-0 ${meta.color}`}>
                {isSimultaneous ? 'Параллельно' : 'Последоват.'}
              </span>
              <button
                onClick={() => remove(link.id)}
                className="p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded transition-colors shrink-0"
              >
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            </div>

            <div className="p-3 space-y-3">
              {/* Parent → Child flow */}
              <div className="flex items-center gap-2">
                <div className="flex-1">
                  <label className="block text-[10px] text-slate-400 mb-1 uppercase tracking-wider">
                    {isSimultaneous ? 'Занятие A' : 'Родитель (A)'}
                  </label>
                  <input
                    value={link.parentName}
                    onChange={e => update(link.id, { parentName: e.target.value })}
                    placeholder="Лекция по алгоритмам…"
                    className="w-full text-xs border border-slate-200 rounded-lg px-2.5 py-2 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/40 text-slate-700 placeholder-slate-300"
                  />
                </div>

                <div className="flex flex-col items-center gap-1 shrink-0 pt-4">
                  <RelIcon className="w-4 h-4 text-slate-300" />
                </div>

                <div className="flex-1">
                  <label className="block text-[10px] text-slate-400 mb-1 uppercase tracking-wider">
                    {isSimultaneous ? 'Занятие B' : 'Потомок (B)'}
                  </label>
                  <input
                    value={link.childName}
                    onChange={e => update(link.id, { childName: e.target.value })}
                    placeholder="Семинар / практика…"
                    className="w-full text-xs border border-slate-200 rounded-lg px-2.5 py-2 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/40 text-slate-700 placeholder-slate-300"
                  />
                </div>
              </div>

              {/* Gap (non-simultaneous) */}
              {!isSimultaneous && (
                <div className="flex items-center gap-3 bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                  <Clock className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                  <span className="text-[11px] text-slate-600">Мин. промежуток после A:</span>
                  <input
                    type="number"
                    min={0}
                    max={480}
                    step={10}
                    value={link.gapMinutes}
                    onChange={e => update(link.id, { gapMinutes: Number(e.target.value) })}
                    className="w-16 text-xs border border-slate-200 rounded-md px-2 py-1 text-center focus:outline-none"
                  />
                  <span className="text-[11px] text-slate-400">мин.</span>
                </div>
              )}

              {/* Simultaneous groups */}
              {isSimultaneous && (
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Users className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                    <span className="text-[11px] text-slate-600">Группы, которые соединяются:</span>
                    <label className="flex items-center gap-1.5 ml-auto">
                      <input
                        type="checkbox"
                        checked={link.sharedRooms}
                        onChange={e => update(link.id, { sharedRooms: e.target.checked })}
                        className="w-3.5 h-3.5"
                      />
                      <span className="text-[10px] text-slate-500">Общая аудитория</span>
                    </label>
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {link.simultaneousGroups.map(g => (
                      <span
                        key={g}
                        className="inline-flex items-center gap-1 bg-purple-50 text-purple-700 border border-purple-200 text-[11px] px-2 py-0.5 rounded-full"
                      >
                        {g}
                        <button onClick={() => removeGroup(link.id, g, link)} className="hover:text-red-500">
                          <Plus className="w-3 h-3 rotate-45" />
                        </button>
                      </span>
                    ))}
                  </div>
                  <div className="flex gap-1.5">
                    <select
                      value={newGroupInput[link.id] ?? ''}
                      onChange={e => setNewGroupInput(prev => ({ ...prev, [link.id]: e.target.value }))}
                      className="flex-1 text-xs border border-slate-200 rounded-md px-2 py-1.5 bg-white focus:outline-none"
                    >
                      <option value="">Выбрать группу…</option>
                      {GROUPS_PRESET.filter(g => !link.simultaneousGroups.includes(g)).map(g => (
                        <option key={g} value={g}>{g}</option>
                      ))}
                    </select>
                    <button
                      onClick={() => addGroup(link.id, link)}
                      className="px-3 py-1.5 bg-purple-600 text-white rounded-md text-xs hover:bg-purple-700 transition-colors"
                    >
                      <Plus className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        );
      })}

      <button
        onClick={add}
        className="w-full flex items-center justify-center gap-2 border-2 border-dashed border-purple-200 text-purple-600 rounded-xl py-2.5 text-xs font-medium hover:bg-purple-50 hover:border-purple-300 transition-all"
      >
        <Plus className="w-3.5 h-3.5" />
        Добавить связку активностей
      </button>
    </div>
  );
}
