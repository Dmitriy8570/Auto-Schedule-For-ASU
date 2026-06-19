import React, { useState, useMemo } from 'react';
import {
  GitBranch, ChevronRight, Building2, GraduationCap,
  Users, Lock, Unlock, Eye, EyeOff, AlertCircle,
  CheckCircle, Info, Layers, ArrowDown, Globe
} from 'lucide-react';
import type { EntityConstraints, CellState } from './PropertiesPanel';

interface InheritanceLevel {
  id: string;
  name: string;
  level: 'global' | 'institute' | 'department' | 'local';
  icon: React.FC<{ className?: string }>;
  constraints?: Partial<EntityConstraints>;
  isActive: boolean;
  source: string;
}

interface InheritanceViewProps {
  entityId: string;
  entityName: string;
  entityType: 'teacher' | 'room' | 'group' | 'subject';
  localConstraints: EntityConstraints;
  departmentId?: string;
  instituteId?: string;
  onToggleOverride: (level: string, field: keyof EntityConstraints) => void;
}

const LEVEL_META: Record<string, { label: string; color: string; bgColor: string }> = {
  global: {
    label: 'Весь вуз',
    color: 'text-purple-700',
    bgColor: 'bg-purple-100',
  },
  institute: {
    label: 'Институт',
    color: 'text-blue-700',
    bgColor: 'bg-blue-100',
  },
  department: {
    label: 'Кафедра',
    color: 'text-indigo-700',
    bgColor: 'bg-indigo-100',
  },
  local: {
    label: 'Локально',
    color: 'text-green-700',
    bgColor: 'bg-green-100',
  },
};

// Mock global/department constraints (в реальном приложении это будет из базы)
const MOCK_GLOBAL_CONSTRAINTS: Partial<EntityConstraints> = {
  maxHoursPerDay: 10,
  maxGapsPerDay: 3,
  timeGrid: (() => {
    const grid: Record<string, CellState> = {};
    // Saturday prohibited globally
    [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
    return grid;
  })(),
};

const MOCK_INSTITUTE_CONSTRAINTS: Partial<EntityConstraints> = {
  maxHoursPerDay: 8,
  buildingPreference: 'Главный корпус (А)',
};

const MOCK_DEPARTMENT_CONSTRAINTS: Partial<EntityConstraints> = {
  maxGapsPerDay: 2,
  timeGrid: (() => {
    const grid: Record<string, CellState> = {};
    // Saturday prohibited
    [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
    // No first period
    [0,1,2,3,4].forEach(di => { grid[`${di}-1`] = 'discouraged'; });
    return grid;
  })(),
};

export function InheritanceView({
  entityId,
  entityName,
  entityType,
  localConstraints,
  departmentId,
  instituteId,
  onToggleOverride,
}: InheritanceViewProps) {
  const [expandedLevels, setExpandedLevels] = useState<Set<string>>(new Set(['local']));
  const [showConflicts, setShowConflicts] = useState(true);

  const levels: InheritanceLevel[] = useMemo(() => {
    const result: InheritanceLevel[] = [];

    // Global level
    result.push({
      id: 'global',
      name: 'Университетские стандарты',
      level: 'global',
      icon: Globe,
      constraints: MOCK_GLOBAL_CONSTRAINTS,
      isActive: true,
      source: 'Ректорат',
    });

    // Institute level (if applicable)
    if (instituteId && entityType === 'teacher') {
      result.push({
        id: 'institute',
        name: 'Институт информационных технологий',
        level: 'institute',
        icon: Building2,
        constraints: MOCK_INSTITUTE_CONSTRAINTS,
        isActive: true,
        source: 'Дирекция института',
      });
    }

    // Department level (if applicable)
    if (departmentId && (entityType === 'teacher' || entityType === 'group')) {
      result.push({
        id: 'department',
        name: 'Кафедра программирования',
        level: 'department',
        icon: GraduationCap,
        constraints: MOCK_DEPARTMENT_CONSTRAINTS,
        isActive: true,
        source: 'Заведующий кафедрой',
      });
    }

    // Local level
    result.push({
      id: 'local',
      name: entityName,
      level: 'local',
      icon: Users,
      constraints: localConstraints,
      isActive: true,
      source: 'Индивидуальные настройки',
    });

    return result;
  }, [entityId, entityName, entityType, departmentId, instituteId, localConstraints]);

  const toggleLevel = (id: string) => {
    setExpandedLevels(prev => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  // Analyze conflicts
  const conflicts = useMemo(() => {
    const result: { field: string; levels: string[]; severity: 'high' | 'medium' | 'low' }[] = [];

    // Check maxHoursPerDay
    const hoursValues = levels
      .filter(l => l.constraints?.maxHoursPerDay !== undefined)
      .map(l => ({ level: l.name, value: l.constraints!.maxHoursPerDay! }));
    
    if (hoursValues.length > 1) {
      const min = Math.min(...hoursValues.map(h => h.value));
      const max = Math.max(...hoursValues.map(h => h.value));
      if (max - min > 2) {
        result.push({
          field: 'maxHoursPerDay',
          levels: hoursValues.map(h => `${h.level} (${h.value}ч)`),
          severity: 'high',
        });
      }
    }

    // Check maxGapsPerDay
    const gapsValues = levels
      .filter(l => l.constraints?.maxGapsPerDay !== undefined)
      .map(l => ({ level: l.name, value: l.constraints!.maxGapsPerDay! }));
    
    if (gapsValues.length > 1) {
      const min = Math.min(...gapsValues.map(g => g.value));
      const max = Math.max(...gapsValues.map(g => g.value));
      if (max - min > 1) {
        result.push({
          field: 'maxGapsPerDay',
          levels: gapsValues.map(g => `${g.level} (${g.value})`),
          severity: 'medium',
        });
      }
    }

    return result;
  }, [levels]);

  const renderConstraintField = (
    label: string,
    field: keyof EntityConstraints,
    level: InheritanceLevel,
    value: any
  ) => {
    const isOverridden = level.level === 'local';
    
    return (
      <div className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-slate-50 transition-colors group">
        <div className="flex-1">
          <span className="text-xs font-medium text-slate-700">{label}</span>
          <span className="text-xs text-slate-500 ml-2">
            {typeof value === 'object' ? JSON.stringify(value).substring(0, 30) + '...' : String(value)}
          </span>
        </div>
        
        {isOverridden && (
          <button
            onClick={() => onToggleOverride(level.id, field)}
            className="opacity-0 group-hover:opacity-100 transition-opacity p-1 rounded hover:bg-white"
            title="Переключить переопределение"
          >
            <Unlock className="w-3.5 h-3.5 text-slate-400" />
          </button>
        )}
      </div>
    );
  };

  return (
    <div className="h-full flex flex-col bg-slate-50">
      
      {/* Header */}
      <div className="px-5 py-4 border-b border-slate-200 bg-white">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <GitBranch className="w-5 h-5 text-[rgb(26,77,156)]" />
            <h3 className="text-sm font-bold text-slate-800">Иерархия ограничений</h3>
          </div>
          <button
            onClick={() => setShowConflicts(!showConflicts)}
            className={`flex items-center gap-1.5 text-xs px-2.5 py-1.5 rounded-lg border transition-all ${
              showConflicts
                ? 'bg-amber-50 border-amber-200 text-amber-700'
                : 'bg-slate-100 border-slate-200 text-slate-600'
            }`}
          >
            {showConflicts ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
            Конфликты
          </button>
        </div>

        {/* Info */}
        <div className="flex items-start gap-2 p-3 bg-blue-50 border border-blue-100 rounded-lg">
          <Info className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
          <div className="text-xs text-blue-700 leading-relaxed">
            Ограничения применяются каскадно. Локальные настройки переопределяют вышестоящие уровни.
            <b> Более строгие правила имеют приоритет.</b>
          </div>
        </div>
      </div>

      {/* Conflicts panel */}
      {showConflicts && conflicts.length > 0 && (
        <div className="px-5 py-3 border-b border-slate-200 bg-amber-50">
          <div className="flex items-center gap-2 mb-2">
            <AlertCircle className="w-4 h-4 text-amber-600" />
            <span className="text-xs font-semibold text-amber-800">Обнаружены конфликты ({conflicts.length})</span>
          </div>
          <div className="space-y-2">
            {conflicts.map((conflict, idx) => (
              <div key={idx} className="p-2 bg-white border border-amber-200 rounded-lg">
                <div className="text-xs font-medium text-amber-900 mb-1">
                  {conflict.field === 'maxHoursPerDay' ? 'Макс. часов/день' : 'Макс. «окон»/день'}
                </div>
                <div className="text-[10px] text-amber-700">
                  {conflict.levels.join(' → ')}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Levels tree */}
      <div className="flex-1 overflow-y-auto p-5">
        <div className="space-y-3">
          {levels.map((level, idx) => {
            const isExpanded = expandedLevels.has(level.id);
            const LevelIcon = level.icon;
            const meta = LEVEL_META[level.level];
            const hasConstraints = level.constraints && Object.keys(level.constraints).length > 0;

            return (
              <div key={level.id}>
                {/* Connection line */}
                {idx > 0 && (
                  <div className="flex items-center gap-2 ml-7 mb-2">
                    <ArrowDown className="w-3.5 h-3.5 text-slate-300" />
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>
                )}

                {/* Level card */}
                <div className={`border-2 rounded-xl overflow-hidden transition-all ${
                  isExpanded ? 'border-[rgb(26,77,156)] shadow-md' : 'border-slate-200'
                }`}>
                  
                  {/* Header */}
                  <button
                    onClick={() => toggleLevel(level.id)}
                    className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${
                      isExpanded ? 'bg-[rgb(26,77,156)] text-white' : 'bg-white hover:bg-slate-50'
                    }`}
                  >
                    <div className={`w-9 h-9 rounded-lg flex items-center justify-center shrink-0 ${
                      isExpanded ? 'bg-white/20' : meta.bgColor
                    }`}>
                      <LevelIcon className={`w-4 h-4 ${isExpanded ? 'text-white' : meta.color}`} />
                    </div>
                    
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-0.5">
                        <span className="text-sm font-semibold truncate">{level.name}</span>
                        <span className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${
                          isExpanded ? 'bg-white/20 text-white' : `${meta.bgColor} ${meta.color}`
                        }`}>
                          {meta.label}
                        </span>
                      </div>
                      <span className={`text-xs ${isExpanded ? 'text-blue-100' : 'text-slate-500'}`}>
                        {level.source}
                      </span>
                    </div>

                    {hasConstraints && (
                      <div className={`flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-md ${
                        isExpanded ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'
                      }`}>
                        <Layers className="w-3 h-3" />
                        {Object.keys(level.constraints!).length}
                      </div>
                    )}

                    <ChevronRight className={`w-4 h-4 transition-transform ${
                      isExpanded ? 'rotate-90' : ''
                    }`} />
                  </button>

                  {/* Expanded content */}
                  {isExpanded && level.constraints && (
                    <div className="bg-white border-t border-slate-100 p-4">
                      <div className="space-y-1">
                        {level.constraints.maxHoursPerDay !== undefined &&
                          renderConstraintField('Макс. часов/день', 'maxHoursPerDay', level, level.constraints.maxHoursPerDay)
                        }
                        {level.constraints.maxGapsPerDay !== undefined &&
                          renderConstraintField('Макс. «окон»/день', 'maxGapsPerDay', level, level.constraints.maxGapsPerDay)
                        }
                        {level.constraints.buildingPreference &&
                          renderConstraintField('Предпочтение корпуса', 'buildingPreference', level, level.constraints.buildingPreference)
                        }
                        {level.constraints.requiredEquipment && level.constraints.requiredEquipment.length > 0 &&
                          renderConstraintField('Оборудование', 'requiredEquipment', level, level.constraints.requiredEquipment.join(', '))
                        }
                        {level.constraints.timeGrid && Object.keys(level.constraints.timeGrid).length > 0 && (
                          <div className="py-2 px-3">
                            <span className="text-xs font-medium text-slate-700">Временная сетка</span>
                            <div className="mt-2 grid grid-cols-6 gap-1">
                              {Object.entries(level.constraints.timeGrid).slice(0, 12).map(([key, state]) => (
                                <div
                                  key={key}
                                  className={`h-6 rounded border text-[9px] flex items-center justify-center ${
                                    state === 'prohibited' ? 'bg-red-400 border-red-500 text-white' :
                                    state === 'preferred' ? 'bg-green-200 border-green-400' :
                                    state === 'required' ? 'bg-green-500 border-green-600 text-white' :
                                    state === 'discouraged' ? 'bg-yellow-200 border-yellow-400' :
                                    'bg-slate-50 border-slate-200'
                                  }`}
                                  title={`${key}: ${state}`}
                                >
                                  {key.split('-')[1]}
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>

                      {level.level === 'local' && (
                        <div className="mt-3 pt-3 border-t border-slate-100">
                          <div className="flex items-center gap-2 text-xs text-slate-500">
                            <Lock className="w-3.5 h-3.5" />
                            <span>Локальные настройки переопределяют вышестоящие уровни</span>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        {/* Final resolution summary */}
        <div className="mt-6 p-4 bg-green-50 border border-green-200 rounded-xl">
          <div className="flex items-center gap-2 mb-2">
            <CheckCircle className="w-4 h-4 text-green-600" />
            <span className="text-xs font-semibold text-green-800">Итоговое разрешение</span>
          </div>
          <div className="text-xs text-green-700 leading-relaxed">
            Применяются <b>локальные настройки</b> с учетом более строгих ограничений вышестоящих уровней.
            Конфликты разрешаются в пользу более строгого правила.
          </div>
        </div>
      </div>
    </div>
  );
}
