import React, { useState, useMemo } from 'react';
import {
  CheckSquare, Square, Copy, Trash2, Users, Settings,
  CheckCircle, XCircle, AlertTriangle, Filter, ChevronDown,
  Zap, Clock, Ban, CalendarOff, Building2, GraduationCap,
  Layers
} from 'lucide-react';
import type { EntityConstraints } from './PropertiesPanel';

interface EntityItem {
  id: string;
  name: string;
  type: 'teacher' | 'room' | 'group' | 'subject';
  subLabel?: string;
  departmentId?: string;
  instituteId?: string;
  buildingId?: string;
}

interface BulkActionsPanelProps {
  entities: EntityItem[];
  savedConstraints: Record<string, EntityConstraints>;
  onApplyBulk: (entityIds: string[], template: Partial<EntityConstraints>) => void;
  onClose: () => void;
}

type BulkActionType = 
  | 'saturday_off'
  | 'morning_shift'
  | 'afternoon_shift'
  | 'no_first_period'
  | 'max_hours_6'
  | 'max_hours_8'
  | 'no_gaps'
  | 'building_preference';

interface BulkAction {
  id: BulkActionType;
  label: string;
  description: string;
  icon: React.FC<{ className?: string }>;
  category: 'time' | 'shift' | 'load' | 'logistics';
  template: Partial<EntityConstraints>;
}

const BULK_ACTIONS: BulkAction[] = [
  {
    id: 'saturday_off',
    label: 'Выходной в субботу',
    description: 'Запретить все слоты в субботу',
    icon: CalendarOff,
    category: 'time',
    template: {
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        [1,2,3,4,5,6,7,8].forEach(p => {
          grid[`5-${p}`] = 'prohibited';
        });
        return grid;
      })(),
    },
  },
  {
    id: 'morning_shift',
    label: 'Только утренняя смена',
    description: 'Разрешены только 1-4 пары',
    icon: Clock,
    category: 'shift',
    template: {
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        [0,1,2,3,4].forEach(di => {
          [5,6,7,8].forEach(p => {
            grid[`${di}-${p}`] = 'prohibited';
          });
        });
        return grid;
      })(),
    },
  },
  {
    id: 'afternoon_shift',
    label: 'Только дневная смена',
    description: 'Разрешены только 3-6 пары',
    icon: Clock,
    category: 'shift',
    template: {
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        [0,1,2,3,4].forEach(di => {
          [1,2,7,8].forEach(p => {
            grid[`${di}-${p}`] = 'prohibited';
          });
        });
        return grid;
      })(),
    },
  },
  {
    id: 'no_first_period',
    label: 'Без 1-й пары',
    description: 'Запретить первую пару во все дни',
    icon: Ban,
    category: 'time',
    template: {
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        [0,1,2,3,4,5].forEach(di => {
          grid[`${di}-1`] = 'prohibited';
        });
        return grid;
      })(),
    },
  },
  {
    id: 'max_hours_6',
    label: 'Макс. 6 часов/день',
    description: 'Установить предел в 3 пары в день',
    icon: Settings,
    category: 'load',
    template: {
      maxHoursPerDay: 6,
    },
  },
  {
    id: 'max_hours_8',
    label: 'Макс. 8 часов/день',
    description: 'Установить предел в 4 пары в день',
    icon: Settings,
    category: 'load',
    template: {
      maxHoursPerDay: 8,
    },
  },
  {
    id: 'no_gaps',
    label: 'Без «окон»',
    description: 'Запретить разрывы между парами',
    icon: Layers,
    category: 'time',
    template: {
      maxGapsPerDay: 0,
    },
  },
];

const CATEGORY_LABELS: Record<string, string> = {
  time: 'Время',
  shift: 'Смены',
  load: 'Нагрузка',
  logistics: 'Логистика',
};

export function BulkActionsPanel({ entities, savedConstraints, onApplyBulk, onClose }: BulkActionsPanelProps) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [selectedAction, setSelectedAction] = useState<BulkActionType | null>(null);
  const [filterType, setFilterType] = useState<'all' | 'teacher' | 'room' | 'group'>('all');
  const [showConfirm, setShowConfirm] = useState(false);

  const filteredEntities = useMemo(() => {
    if (filterType === 'all') return entities;
    return entities.filter(e => e.type === filterType);
  }, [entities, filterType]);

  const toggleSelect = (id: string) => {
    const newSet = new Set(selectedIds);
    if (newSet.has(id)) {
      newSet.delete(id);
    } else {
      newSet.add(id);
    }
    setSelectedIds(newSet);
  };

  const toggleAll = () => {
    if (selectedIds.size === filteredEntities.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(filteredEntities.map(e => e.id)));
    }
  };

  const selectByDepartment = (deptId: string) => {
    const deptEntities = entities.filter(e => e.departmentId === deptId);
    setSelectedIds(new Set(deptEntities.map(e => e.id)));
  };

  const handleApply = () => {
    if (!selectedAction || selectedIds.size === 0) return;
    
    const action = BULK_ACTIONS.find(a => a.id === selectedAction);
    if (!action) return;

    onApplyBulk(Array.from(selectedIds), action.template);
    setShowConfirm(false);
    setSelectedIds(new Set());
    setSelectedAction(null);
  };

  const groupedActions = useMemo(() => {
    const grouped: Record<string, BulkAction[]> = {};
    BULK_ACTIONS.forEach(action => {
      if (!grouped[action.category]) {
        grouped[action.category] = [];
      }
      grouped[action.category].push(action);
    });
    return grouped;
  }, []);

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-8">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-6xl h-[90vh] flex flex-col overflow-hidden">
        
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-200 bg-gradient-to-r from-[rgb(26,77,156)] to-[rgb(40,95,180)]">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur-sm flex items-center justify-center">
                <Zap className="w-5 h-5 text-white" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-white">Массовые действия</h2>
                <p className="text-xs text-blue-100">
                  {selectedIds.size > 0 
                    ? `Выбрано объектов: ${selectedIds.size}` 
                    : 'Выберите объекты для применения правил'}
                </p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="w-8 h-8 rounded-lg bg-white/20 hover:bg-white/30 flex items-center justify-center text-white transition-colors"
            >
              <XCircle className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="flex-1 flex overflow-hidden">
          
          {/* Left: Entity selection */}
          <div className="w-1/2 border-r border-slate-200 flex flex-col">
            <div className="px-4 py-3 border-b border-slate-100 bg-slate-50">
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-600">Выберите объекты</span>
                <div className="flex items-center gap-2">
                  <Filter className="w-3.5 h-3.5 text-slate-400" />
                  <select
                    value={filterType}
                    onChange={e => setFilterType(e.target.value as any)}
                    className="text-xs border border-slate-200 rounded-md px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30"
                  >
                    <option value="all">Все типы</option>
                    <option value="teacher">Преподаватели</option>
                    <option value="room">Аудитории</option>
                    <option value="group">Группы</option>
                  </select>
                </div>
              </div>
              
              <button
                onClick={toggleAll}
                className="w-full flex items-center gap-2 px-3 py-2 rounded-lg bg-white border border-slate-200 hover:border-[rgb(26,77,156)] text-xs font-medium text-slate-700 transition-all"
              >
                {selectedIds.size === filteredEntities.length && filteredEntities.length > 0 ? (
                  <CheckSquare className="w-4 h-4 text-[rgb(26,77,156)]" />
                ) : (
                  <Square className="w-4 h-4 text-slate-400" />
                )}
                <span>
                  {selectedIds.size === filteredEntities.length && filteredEntities.length > 0
                    ? 'Снять выделение со всех'
                    : 'Выбрать все'}
                </span>
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-2">
              {filteredEntities.map(entity => {
                const isSelected = selectedIds.has(entity.id);
                const hasSaved = !!savedConstraints[entity.id];
                
                return (
                  <button
                    key={entity.id}
                    onClick={() => toggleSelect(entity.id)}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg border transition-all ${
                      isSelected
                        ? 'bg-[rgb(26,77,156)]/10 border-[rgb(26,77,156)] shadow-sm'
                        : 'bg-white border-slate-200 hover:border-slate-300'
                    }`}
                  >
                    {isSelected ? (
                      <CheckSquare className="w-4 h-4 text-[rgb(26,77,156)] shrink-0" />
                    ) : (
                      <Square className="w-4 h-4 text-slate-400 shrink-0" />
                    )}
                    
                    <div className="flex-1 text-left min-w-0">
                      <div className="text-sm font-medium text-slate-700 truncate">{entity.name}</div>
                      {entity.subLabel && (
                        <div className="text-xs text-slate-400 truncate">{entity.subLabel}</div>
                      )}
                    </div>

                    {hasSaved && (
                      <div className="shrink-0 flex items-center gap-1">
                        <div className="w-1.5 h-1.5 rounded-full bg-green-500" />
                        <span className="text-[10px] text-green-600">Настроено</span>
                      </div>
                    )}
                  </button>
                );
              })}

              {filteredEntities.length === 0 && (
                <div className="text-center py-12 text-slate-400">
                  <Users className="w-12 h-12 mx-auto mb-3 text-slate-300" />
                  <p className="text-sm">Нет объектов для выбора</p>
                </div>
              )}
            </div>
          </div>

          {/* Right: Action templates */}
          <div className="w-1/2 flex flex-col">
            <div className="px-4 py-3 border-b border-slate-100 bg-slate-50">
              <span className="text-xs font-semibold text-slate-600">Выберите действие</span>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {Object.entries(groupedActions).map(([category, actions]) => (
                <div key={category}>
                  <h4 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2 flex items-center gap-2">
                    {CATEGORY_LABELS[category]}
                  </h4>
                  
                  <div className="space-y-2">
                    {actions.map(action => {
                      const isSelected = selectedAction === action.id;
                      const ActionIcon = action.icon;
                      
                      return (
                        <button
                          key={action.id}
                          onClick={() => setSelectedAction(action.id)}
                          className={`w-full text-left p-3 rounded-xl border transition-all ${
                            isSelected
                              ? 'bg-[rgb(26,77,156)] border-[rgb(26,77,156)] text-white shadow-md'
                              : 'bg-white border-slate-200 hover:border-[rgb(26,77,156)] hover:shadow-sm'
                          }`}
                        >
                          <div className="flex items-start gap-3">
                            <div className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 ${
                              isSelected ? 'bg-white/20' : 'bg-slate-100'
                            }`}>
                              <ActionIcon className={`w-4 h-4 ${isSelected ? 'text-white' : 'text-[rgb(26,77,156)]'}`} />
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className={`text-sm font-semibold mb-0.5 ${isSelected ? 'text-white' : 'text-slate-700'}`}>
                                {action.label}
                              </div>
                              <div className={`text-xs ${isSelected ? 'text-blue-100' : 'text-slate-400'}`}>
                                {action.description}
                              </div>
                            </div>
                          </div>
                        </button>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-200 bg-slate-50 flex items-center justify-between">
          <div className="text-xs text-slate-500">
            {selectedIds.size > 0 && selectedAction ? (
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 text-amber-500" />
                <span>
                  Будет применено к <b>{selectedIds.size}</b> объектам: <b>{BULK_ACTIONS.find(a => a.id === selectedAction)?.label}</b>
                </span>
              </div>
            ) : (
              <span>Выберите объекты и действие для приме��ения</span>
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-800 transition-colors"
            >
              Отмена
            </button>
            <button
              onClick={() => setShowConfirm(true)}
              disabled={selectedIds.size === 0 || !selectedAction}
              className={`px-5 py-2 rounded-lg text-sm font-semibold transition-all ${
                selectedIds.size === 0 || !selectedAction
                  ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                  : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'
              }`}
            >
              Применить
            </button>
          </div>
        </div>

        {/* Confirmation Modal */}
        {showConfirm && (
          <div className="absolute inset-0 bg-black/30 flex items-center justify-center p-8">
            <div className="bg-white rounded-xl shadow-2xl max-w-md w-full p-6">
              <div className="flex items-start gap-4 mb-4">
                <div className="w-12 h-12 rounded-xl bg-amber-100 flex items-center justify-center shrink-0">
                  <AlertTriangle className="w-6 h-6 text-amber-600" />
                </div>
                <div className="flex-1">
                  <h3 className="text-base font-bold text-slate-800 mb-1">Подтвердите действие</h3>
                  <p className="text-sm text-slate-600">
                    Вы собираетесь применить правило <b>«{BULK_ACTIONS.find(a => a.id === selectedAction)?.label}»</b> к <b>{selectedIds.size}</b> объектам.
                  </p>
                  <p className="text-xs text-slate-500 mt-2">
                    Это действие изменит существующие настройки. Вы уверены?
                  </p>
                </div>
              </div>
              
              <div className="flex items-center gap-3 justify-end">
                <button
                  onClick={() => setShowConfirm(false)}
                  className="px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-800 transition-colors"
                >
                  Отмена
                </button>
                <button
                  onClick={handleApply}
                  className="px-5 py-2 rounded-lg text-sm font-semibold bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] transition-all"
                >
                  Подтвердить
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
