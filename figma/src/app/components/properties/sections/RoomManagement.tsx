import React, { useState } from 'react';
import {
  MapPin, Tag, Lock, Unlock, Clock, Building2,
  Plus, X, Info, AlertTriangle, Star, Shield,
  Users, Zap, Calendar
} from 'lucide-react';

// ─── Room Tags System ─────────────────────────────────────────────────────────

export interface RoomTag {
  id: string;
  name: string;
  category: 'equipment' | 'type' | 'feature' | 'restriction';
  color: string;
  icon: React.FC<{ className?: string }>;
}

export const ROOM_TAGS: RoomTag[] = [
  // Equipment
  { id: 'projector', name: 'Проектор', category: 'equipment', color: 'bg-blue-100 text-blue-700 border-blue-200', icon: Zap },
  { id: 'computer_lab', name: 'Компьютерный класс', category: 'equipment', color: 'bg-purple-100 text-purple-700 border-purple-200', icon: Zap },
  { id: 'interactive_board', name: 'Интерактивная доска', category: 'equipment', color: 'bg-cyan-100 text-cyan-700 border-cyan-200', icon: Zap },
  { id: 'chemistry_lab', name: 'Химическая лаборатория', category: 'equipment', color: 'bg-orange-100 text-orange-700 border-orange-200', icon: Shield },
  { id: 'physics_lab', name: 'Физическая лаборатория', category: 'equipment', color: 'bg-green-100 text-green-700 border-green-200', icon: Shield },
  
  // Type
  { id: 'lecture_hall', name: 'Лекторий', category: 'type', color: 'bg-indigo-100 text-indigo-700 border-indigo-200', icon: Users },
  { id: 'seminar_room', name: 'Семинарская', category: 'type', color: 'bg-teal-100 text-teal-700 border-teal-200', icon: Users },
  { id: 'conference', name: 'Конференц-зал', category: 'type', color: 'bg-violet-100 text-violet-700 border-violet-200', icon: Star },
  
  // Features
  { id: 'audio_system', name: 'Аудиосистема', category: 'feature', color: 'bg-pink-100 text-pink-700 border-pink-200', icon: Zap },
  { id: 'video_recording', name: 'Видеозапись', category: 'feature', color: 'bg-fuchsia-100 text-fuchsia-700 border-fuchsia-200', icon: Zap },
  { id: 'air_conditioning', name: 'Кондиционер', category: 'feature', color: 'bg-sky-100 text-sky-700 border-sky-200', icon: Zap },
  
  // Restrictions
  { id: 'priority_access', name: 'Приоритетный доступ', category: 'restriction', color: 'bg-amber-100 text-amber-700 border-amber-200', icon: Star },
  { id: 'department_only', name: 'Только для кафедры', category: 'restriction', color: 'bg-red-100 text-red-700 border-red-200', icon: Lock },
];

// ─── Soft Lock Configuration ──────────────────────────────────────────────────

export interface SoftLock {
  id: string;
  department: string;
  institute?: string;
  timeSlots: string[]; // e.g., ["0-1", "0-2", "1-1"] (dayIndex-period)
  priority: 'high' | 'medium' | 'low';
  override: boolean; // Can others use if room is free?
  notes: string;
}

const DEPARTMENTS_PRESET = [
  'Кафедра программирования',
  'Кафедра информационных систем',
  'Кафедра высшей математики',
  'Кафедра экономики',
  'Кафедра менеджмента',
  'Кафедра гражданского права',
];

const INSTITUTES_PRESET = [
  'Институт информационных технологий',
  'Институт экономики и торговли',
  'Юридический институт',
];

const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
const PERIODS = [1, 2, 3, 4, 5, 6, 7, 8];

// ─── Main Component ───────────────────────────────────────────────────────────

interface RoomManagementProps {
  roomId: string;
  tags: string[];
  softLocks: SoftLock[];
  onTagsChange: (tags: string[]) => void;
  onSoftLocksChange: (locks: SoftLock[]) => void;
}

export function RoomManagement({
  roomId,
  tags,
  softLocks,
  onTagsChange,
  onSoftLocksChange,
}: RoomManagementProps) {
  const [showTagPicker, setShowTagPicker] = useState(false);
  const [showLockEditor, setShowLockEditor] = useState(false);
  const [editingLock, setEditingLock] = useState<SoftLock | null>(null);

  const addTag = (tagId: string) => {
    if (!tags.includes(tagId)) {
      onTagsChange([...tags, tagId]);
    }
  };

  const removeTag = (tagId: string) => {
    onTagsChange(tags.filter(t => t !== tagId));
  };

  const addSoftLock = () => {
    const newLock: SoftLock = {
      id: Date.now().toString(),
      department: DEPARTMENTS_PRESET[0],
      timeSlots: [],
      priority: 'medium',
      override: true,
      notes: '',
    };
    setEditingLock(newLock);
    setShowLockEditor(true);
  };

  const saveSoftLock = (lock: SoftLock) => {
    const existing = softLocks.find(l => l.id === lock.id);
    if (existing) {
      onSoftLocksChange(softLocks.map(l => (l.id === lock.id ? lock : l)));
    } else {
      onSoftLocksChange([...softLocks, lock]);
    }
    setShowLockEditor(false);
    setEditingLock(null);
  };

  const removeSoftLock = (id: string) => {
    onSoftLocksChange(softLocks.filter(l => l.id !== id));
  };

  const selectedTags = ROOM_TAGS.filter(t => tags.includes(t.id));
  const availableTags = ROOM_TAGS.filter(t => !tags.includes(t.id));

  const tagsByCategory = selectedTags.reduce((acc, tag) => {
    if (!acc[tag.category]) acc[tag.category] = [];
    acc[tag.category].push(tag);
    return acc;
  }, {} as Record<string, RoomTag[]>);

  const categoryLabels: Record<string, string> = {
    equipment: 'Оборудование',
    type: 'Тип аудитории',
    feature: 'Особенности',
    restriction: 'Ограничения',
  };

  return (
    <div className="space-y-6">
      
      {/* ═══ Room Tags ═══ */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <Tag className="w-4 h-4 text-[rgb(26,77,156)]" />
            <h4 className="text-sm font-semibold text-slate-700">Теги и метки аудитории</h4>
          </div>
          <button
            onClick={() => setShowTagPicker(!showTagPicker)}
            className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            Добавить тег
          </button>
        </div>

        {/* Tag picker dropdown */}
        {showTagPicker && (
          <div className="mb-3 p-4 bg-slate-50 border border-slate-200 rounded-xl">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-semibold text-slate-600">Доступные теги</span>
              <button
                onClick={() => setShowTagPicker(false)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
            
            <div className="grid grid-cols-2 gap-2">
              {availableTags.map(tag => {
                const TagIcon = tag.icon;
                return (
                  <button
                    key={tag.id}
                    onClick={() => { addTag(tag.id); setShowTagPicker(false); }}
                    className={`flex items-center gap-2 p-2 rounded-lg border transition-all hover:shadow-sm ${tag.color}`}
                  >
                    <TagIcon className="w-3.5 h-3.5" />
                    <span className="text-xs font-medium">{tag.name}</span>
                  </button>
                );
              })}
            </div>
            
            {availableTags.length === 0 && (
              <div className="text-center py-4 text-sm text-slate-400">
                Все доступные теги уже добавлены
              </div>
            )}
          </div>
        )}

        {/* Selected tags by category */}
        {Object.keys(tagsByCategory).length > 0 ? (
          <div className="space-y-3">
            {Object.entries(tagsByCategory).map(([category, categoryTags]) => (
              <div key={category}>
                <div className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
                  {categoryLabels[category]}
                </div>
                <div className="flex flex-wrap gap-2">
                  {categoryTags.map(tag => {
                    const TagIcon = tag.icon;
                    return (
                      <div
                        key={tag.id}
                        className={`flex items-center gap-2 px-3 py-1.5 rounded-lg border ${tag.color}`}
                      >
                        <TagIcon className="w-3.5 h-3.5" />
                        <span className="text-xs font-medium">{tag.name}</span>
                        <button
                          onClick={() => removeTag(tag.id)}
                          className="ml-1 hover:text-red-600 transition-colors"
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-8 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
            <Tag className="w-8 h-8 mx-auto mb-2 text-slate-300" />
            <p>Теги не добавлены</p>
            <p className="text-xs mt-1 text-slate-300">Добавьте теги для описания характеристик аудитории</p>
          </div>
        )}
      </div>

      {/* ═══ Soft Locks ═══ */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <Lock className="w-4 h-4 text-amber-600" />
            <h4 className="text-sm font-semibold text-slate-700">Мягкая резервация (Soft Lock)</h4>
          </div>
          <button
            onClick={addSoftLock}
            className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg bg-amber-600 text-white hover:bg-amber-700 transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            Добавить резервацию
          </button>
        </div>

        <div className="mb-3 p-3 bg-amber-50 border border-amber-100 rounded-xl flex items-start gap-2">
          <Info className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
          <div className="text-xs text-amber-700 leading-relaxed">
            <b>Soft Lock</b> — приоритетная резервация аудитории для кафедры/института в определенные часы.
            Другие подразделения могут использовать аудиторию, если она свободна и установлен флаг «Разрешить переопределение».
          </div>
        </div>

        {softLocks.length > 0 ? (
          <div className="space-y-2">
            {softLocks.map(lock => {
              const priorityColors = {
                high: 'border-red-200 bg-red-50',
                medium: 'border-amber-200 bg-amber-50',
                low: 'border-green-200 bg-green-50',
              };
              
              return (
                <div
                  key={lock.id}
                  className={`p-3 rounded-xl border ${priorityColors[lock.priority]}`}
                >
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <Building2 className="w-3.5 h-3.5 text-slate-600" />
                        <span className="text-sm font-semibold text-slate-800">{lock.department}</span>
                        {lock.institute && (
                          <span className="text-xs text-slate-500">· {lock.institute}</span>
                        )}
                      </div>
                      
                      <div className="flex items-center gap-3 text-xs text-slate-600">
                        <span className="flex items-center gap-1">
                          <Calendar className="w-3 h-3" />
                          {lock.timeSlots.length} слотов
                        </span>
                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-medium ${
                          lock.priority === 'high' ? 'bg-red-100 text-red-700' :
                          lock.priority === 'medium' ? 'bg-amber-100 text-amber-700' :
                          'bg-green-100 text-green-700'
                        }`}>
                          {lock.priority === 'high' ? 'Высокий' : lock.priority === 'medium' ? 'Средний' : 'Низкий'} приоритет
                        </span>
                        {lock.override ? (
                          <span className="flex items-center gap-1 text-green-600">
                            <Unlock className="w-3 h-3" />
                            Можно переопределить
                          </span>
                        ) : (
                          <span className="flex items-center gap-1 text-red-600">
                            <Lock className="w-3 h-3" />
                            Строгая резервация
                          </span>
                        )}
                      </div>
                      
                      {lock.notes && (
                        <p className="text-xs text-slate-500 mt-2 italic">{lock.notes}</p>
                      )}
                    </div>
                    
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => { setEditingLock(lock); setShowLockEditor(true); }}
                        className="p-1.5 rounded-lg hover:bg-white transition-colors text-slate-500 hover:text-[rgb(26,77,156)]"
                        title="Редактировать"
                      >
                        <Zap className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => removeSoftLock(lock.id)}
                        className="p-1.5 rounded-lg hover:bg-white transition-colors text-slate-500 hover:text-red-600"
                        title="Удалить"
                      >
                        <X className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                  
                  {/* Time slots preview */}
                  {lock.timeSlots.length > 0 && (
                    <div className="mt-2 pt-2 border-t border-slate-200/50">
                      <div className="flex flex-wrap gap-1">
                        {lock.timeSlots.slice(0, 12).map(slot => {
                          const [day, period] = slot.split('-');
                          return (
                            <span
                              key={slot}
                              className="text-[10px] px-2 py-0.5 rounded bg-white/60 text-slate-600 font-mono"
                            >
                              {DAYS[parseInt(day)]}-{period}п
                            </span>
                          );
                        })}
                        {lock.timeSlots.length > 12 && (
                          <span className="text-[10px] px-2 py-0.5 text-slate-400">
                            +{lock.timeSlots.length - 12} ещё
                          </span>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        ) : (
          <div className="text-center py-8 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
            <Lock className="w-8 h-8 mx-auto mb-2 text-slate-300" />
            <p>Резервации не созданы</p>
            <p className="text-xs mt-1 text-slate-300">Добавьте soft lock для приоритетного доступа</p>
          </div>
        )}
      </div>

      {/* Soft Lock Editor Modal */}
      {showLockEditor && editingLock && (
        <SoftLockEditor
          lock={editingLock}
          onSave={saveSoftLock}
          onCancel={() => { setShowLockEditor(false); setEditingLock(null); }}
        />
      )}
    </div>
  );
}

// ─── Soft Lock Editor Modal ───────────────────────────────────────────────────

function SoftLockEditor({
  lock,
  onSave,
  onCancel,
}: {
  lock: SoftLock;
  onSave: (lock: SoftLock) => void;
  onCancel: () => void;
}) {
  const [editedLock, setEditedLock] = useState<SoftLock>(lock);
  const [selectedSlots, setSelectedSlots] = useState<Set<string>>(new Set(lock.timeSlots));

  const toggleSlot = (day: number, period: number) => {
    const key = `${day}-${period}`;
    const newSet = new Set(selectedSlots);
    if (newSet.has(key)) {
      newSet.delete(key);
    } else {
      newSet.add(key);
    }
    setSelectedSlots(newSet);
  };

  const handleSave = () => {
    onSave({ ...editedLock, timeSlots: Array.from(selectedSlots) });
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-8">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-3xl max-h-[90vh] flex flex-col overflow-hidden">
        
        <div className="px-6 py-4 border-b border-slate-200 bg-gradient-to-r from-amber-600 to-orange-600">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Lock className="w-5 h-5 text-white" />
              <h3 className="text-base font-bold text-white">Настройка Soft Lock</h3>
            </div>
            <button onClick={onCancel} className="text-white/80 hover:text-white">
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          
          {/* Department */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">Кафедра</label>
            <select
              value={editedLock.department}
              onChange={e => setEditedLock({ ...editedLock, department: e.target.value })}
              className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/30"
            >
              {DEPARTMENTS_PRESET.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>

          {/* Institute (optional) */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">Институт (опционально)</label>
            <select
              value={editedLock.institute || ''}
              onChange={e => setEditedLock({ ...editedLock, institute: e.target.value || undefined })}
              className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/30"
            >
              <option value="">Не указан</option>
              {INSTITUTES_PRESET.map(i => <option key={i} value={i}>{i}</option>)}
            </select>
          </div>

          {/* Priority */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">Приоритет</label>
            <div className="grid grid-cols-3 gap-2">
              {(['high', 'medium', 'low'] as const).map(p => (
                <button
                  key={p}
                  onClick={() => setEditedLock({ ...editedLock, priority: p })}
                  className={`py-2 px-3 rounded-lg text-sm font-medium border transition-all ${
                    editedLock.priority === p
                      ? p === 'high' ? 'bg-red-100 border-red-500 text-red-700'
                        : p === 'medium' ? 'bg-amber-100 border-amber-500 text-amber-700'
                        : 'bg-green-100 border-green-500 text-green-700'
                      : 'bg-slate-50 border-slate-200 text-slate-600 hover:border-slate-300'
                  }`}
                >
                  {p === 'high' ? 'Высокий' : p === 'medium' ? 'Средний' : 'Низкий'}
                </button>
              ))}
            </div>
          </div>

          {/* Override toggle */}
          <div className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
            <div>
              <div className="text-sm font-medium text-slate-700">Разрешить переопределение</div>
              <div className="text-xs text-slate-500">Другие могут использовать аудиторию, если она свободна</div>
            </div>
            <button
              onClick={() => setEditedLock({ ...editedLock, override: !editedLock.override })}
              className={`w-12 h-6 rounded-full transition-colors ${
                editedLock.override ? 'bg-green-500' : 'bg-slate-300'
              }`}
            >
              <div className={`w-5 h-5 rounded-full bg-white shadow-sm transition-transform ${
                editedLock.override ? 'translate-x-6' : 'translate-x-0.5'
              }`} />
            </button>
          </div>

          {/* Time slots picker */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">
              Временные слоты ({selectedSlots.size} выбрано)
            </label>
            <div className="border border-slate-200 rounded-lg p-3 bg-white">
              <div className="overflow-x-auto">
                <div className="min-w-max">
                  <div className="flex mb-1">
                    <div className="w-12 shrink-0" />
                    {DAYS.map(d => (
                      <div key={d} className="w-10 text-center text-[10px] font-semibold text-slate-500">{d}</div>
                    ))}
                  </div>
                  {PERIODS.map(p => (
                    <div key={p} className="flex items-center mb-0.5">
                      <div className="w-12 shrink-0 text-[10px] font-medium text-slate-500 text-right pr-2">{p}п</div>
                      {DAYS.map((_, di) => {
                        const key = `${di}-${p}`;
                        const isSelected = selectedSlots.has(key);
                        return (
                          <button
                            key={key}
                            onClick={() => toggleSlot(di, p)}
                            className={`w-10 h-7 mx-0.5 rounded border transition-all ${
                              isSelected
                                ? 'bg-amber-500 border-amber-600 text-white'
                                : 'bg-slate-50 border-slate-200 hover:bg-slate-100'
                            }`}
                          />
                        );
                      })}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Notes */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-2">Примечания</label>
            <textarea
              value={editedLock.notes}
              onChange={e => setEditedLock({ ...editedLock, notes: e.target.value })}
              rows={3}
              placeholder="Дополнительная информация о резервации..."
              className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/30 resize-none"
            />
          </div>
        </div>

        <div className="px-6 py-4 border-t border-slate-200 bg-slate-50 flex items-center justify-end gap-2">
          <button
            onClick={onCancel}
            className="px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-800 transition-colors"
          >
            Отмена
          </button>
          <button
            onClick={handleSave}
            className="px-5 py-2 rounded-lg text-sm font-semibold bg-amber-600 text-white hover:bg-amber-700 transition-all"
          >
            Сохранить
          </button>
        </div>
      </div>
    </div>
  );
}
