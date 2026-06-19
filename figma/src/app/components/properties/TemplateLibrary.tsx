import React, { useState } from 'react';
import {
  BookOpen, Star, Clock, GraduationCap, Briefcase,
  Coffee, Moon, Zap, ChevronRight, Copy, Download,
  Search, Filter, X, CheckCircle, Tag, Users
} from 'lucide-react';
import type { EntityConstraints } from './PropertiesPanel';

interface ConstraintTemplate {
  id: string;
  name: string;
  description: string;
  category: 'teacher' | 'room' | 'group' | 'general';
  tags: string[];
  icon: React.FC<{ className?: string }>;
  popular?: boolean;
  preset: Partial<EntityConstraints>;
}

const TEMPLATES: ConstraintTemplate[] = [
  {
    id: 'standard_lecturer',
    name: 'Стандартный лектор',
    description: 'Полная ставка, рабочие дни пн-пт, 8 часов/день, 36 часов/неделя',
    category: 'teacher',
    tags: ['преподаватель', 'штатный', 'полная ставка'],
    icon: GraduationCap,
    popular: true,
    preset: {
      maxHoursPerDay: 8,
      maxGapsPerDay: 2,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // Saturday all prohibited
        [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
        // Preferred morning slots
        [0,1,2,3,4].forEach(di => {
          grid[`${di}-1`] = 'preferred';
          grid[`${di}-2`] = 'preferred';
        });
        return grid;
      })(),
      contract: {
        contractType: 'full_time' as const,
        maxHoursPerDay: 8,
        maxHoursPerWeek: 36,
        minRestHours: 12,
        priorityRank: 5,
        allowedShifts: ['morning' as const, 'afternoon' as const],
        fixedDaysOff: [],
        notes: 'Стандартные условия штатного преподавателя',
      },
    },
  },
  {
    id: 'part_time_external',
    name: 'Внешний совместитель',
    description: 'Неполная ставка, 1-2 дня в неделю, вечерние часы приоритетны',
    category: 'teacher',
    tags: ['совместитель', 'внешний', 'почасовик'],
    icon: Briefcase,
    popular: true,
    preset: {
      maxHoursPerDay: 6,
      maxGapsPerDay: 1,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // Most days prohibited, only Thu-Fri allowed
        [0,1,2,5].forEach(di => {
          [1,2,3,4,5,6,7,8].forEach(p => { grid[`${di}-${p}`] = 'prohibited'; });
        });
        // Evening slots preferred
        [3,4].forEach(di => {
          grid[`${di}-5`] = 'preferred';
          grid[`${di}-6`] = 'preferred';
        });
        return grid;
      })(),
      contract: {
        contractType: 'external' as const,
        maxHoursPerDay: 6,
        maxHoursPerWeek: 12,
        minRestHours: 12,
        priorityRank: 3,
        allowedShifts: ['afternoon' as const, 'evening' as const],
        fixedDaysOff: [0, 1, 2, 5],
        notes: 'Внешний совместитель, работает 1-2 дня в неделю',
      },
    },
  },
  {
    id: 'senior_professor',
    name: 'Заведующий кафедрой',
    description: 'Высокий приоритет, без 1-й пары, макс. 6 часов/день, админ. день в среду',
    category: 'teacher',
    tags: ['заведующий', 'руководитель', 'высокий приоритет'],
    icon: Star,
    popular: false,
    preset: {
      maxHoursPerDay: 6,
      maxGapsPerDay: 1,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // No first period any day
        [0,1,2,3,4,5].forEach(di => { grid[`${di}-1`] = 'prohibited'; });
        // Wednesday admin day (only 2-3 pairs max)
        [1,4,5,6,7,8].forEach(p => { grid[`2-${p}`] = 'discouraged'; });
        // Saturday off
        [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
        return grid;
      })(),
      contract: {
        contractType: 'full_time' as const,
        maxHoursPerDay: 6,
        maxHoursPerWeek: 30,
        minRestHours: 14,
        priorityRank: 9,
        allowedShifts: ['morning' as const, 'afternoon' as const],
        fixedDaysOff: [],
        notes: 'Заведующий кафедрой, административный день - среда',
      },
    },
  },
  {
    id: 'computer_lab',
    name: 'Компьютерный класс',
    description: 'Стандартная аудитория с компьютерами, 30 мест, проектор',
    category: 'room',
    tags: ['компьютер', 'лаборатория', 'техника'],
    icon: Zap,
    popular: true,
    preset: {
      roomCapacity: 30,
      requiredEquipment: ['Проектор', 'iMac'],
      buildingPreference: 'Главный корпус (А)',
    },
  },
  {
    id: 'lecture_hall',
    name: 'Лекционная аудитория',
    description: 'Большая аудитория для лекций, 100 мест, проектор, микрофон',
    category: 'room',
    tags: ['лекция', 'большая', 'поток'],
    icon: Users,
    popular: true,
    preset: {
      roomCapacity: 100,
      requiredEquipment: ['Проектор', 'Интерактивная доска'],
      buildingPreference: 'Главный корпус (А)',
    },
  },
  {
    id: 'first_year_group',
    name: 'Группа 1-го курса',
    description: 'Молодая группа, обучение 5 дней, без окон, утренняя смена',
    category: 'group',
    tags: ['1 курс', 'бакалавриат', 'полный день'],
    icon: GraduationCap,
    popular: false,
    preset: {
      maxGapsPerDay: 1,
      maxHoursPerDay: 8,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // Saturday off
        [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
        // Evening periods discouraged
        [0,1,2,3,4].forEach(di => {
          grid[`${di}-7`] = 'discouraged';
          grid[`${di}-8`] = 'discouraged';
        });
        return grid;
      })(),
    },
  },
  {
    id: 'evening_group',
    name: 'Вечерняя группа',
    description: 'Заочное/вечернее обучение, только 5-8 пары, 2-3 дня в неделю',
    category: 'group',
    tags: ['вечерняя', 'заочное', 'работающие'],
    icon: Moon,
    popular: false,
    preset: {
      maxGapsPerDay: 0,
      maxHoursPerDay: 8,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // Only evening slots allowed
        [0,1,2,3,4,5].forEach(di => {
          [1,2,3,4].forEach(p => { grid[`${di}-${p}`] = 'prohibited'; });
        });
        // Only Thu-Sat
        [0,1,2].forEach(di => {
          [5,6,7,8].forEach(p => { grid[`${di}-${p}`] = 'prohibited'; });
        });
        // Saturday full day allowed
        [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'preferred'; });
        return grid;
      })(),
    },
  },
  {
    id: 'minimal_constraints',
    name: 'Минимальные ограничения',
    description: 'Только базовые запреты (суббота), максимальная гибкость',
    category: 'general',
    tags: ['базовый', 'гибкий', 'универсальный'],
    icon: Coffee,
    popular: true,
    preset: {
      maxGapsPerDay: 3,
      maxHoursPerDay: 10,
      timeGrid: (() => {
        const grid: Record<string, any> = {};
        // Only Saturday prohibited
        [1,2,3,4,5,6,7,8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
        return grid;
      })(),
    },
  },
];

const CATEGORY_LABELS: Record<string, string> = {
  teacher: 'Преподаватели',
  room: 'Аудитории',
  group: 'Группы',
  general: 'Общие',
};

interface TemplateLibraryProps {
  entityType?: 'teacher' | 'room' | 'group' | 'subject';
  onApplyTemplate: (template: Partial<EntityConstraints>) => void;
  onClose: () => void;
}

export function TemplateLibrary({ entityType, onApplyTemplate, onClose }: TemplateLibraryProps) {
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);
  const [showPreview, setShowPreview] = useState(false);

  const filteredTemplates = TEMPLATES.filter(t => {
    // Filter by entity type if specified
    if (entityType && t.category !== 'general' && t.category !== entityType) {
      return false;
    }
    
    // Filter by category
    if (categoryFilter !== 'all' && t.category !== categoryFilter) {
      return false;
    }

    // Search
    if (search) {
      const q = search.toLowerCase();
      return (
        t.name.toLowerCase().includes(q) ||
        t.description.toLowerCase().includes(q) ||
        t.tags.some(tag => tag.toLowerCase().includes(q))
      );
    }

    return true;
  });

  const popularTemplates = filteredTemplates.filter(t => t.popular);
  const otherTemplates = filteredTemplates.filter(t => !t.popular);

  const handleApply = (template: ConstraintTemplate) => {
    onApplyTemplate(template.preset);
    onClose();
  };

  const selected = TEMPLATES.find(t => t.id === selectedTemplate);

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-8">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-5xl h-[85vh] flex flex-col overflow-hidden">
        
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-200 bg-gradient-to-r from-indigo-600 to-purple-600">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur-sm flex items-center justify-center">
                <BookOpen className="w-5 h-5 text-white" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-white">Библиотека шаблонов</h2>
                <p className="text-xs text-indigo-100">
                  Готовые наборы ограничений для быстрой настройки
                </p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="w-8 h-8 rounded-lg bg-white/20 hover:bg-white/30 flex items-center justify-center text-white transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Toolbar */}
        <div className="px-6 py-4 border-b border-slate-100 bg-slate-50 flex items-center gap-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Поиск по названию, описанию или тегам..."
              className="w-full pl-10 pr-4 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500"
            />
          </div>

          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-slate-400" />
            <select
              value={categoryFilter}
              onChange={e => setCategoryFilter(e.target.value)}
              className="text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/30"
            >
              <option value="all">Все категории</option>
              {Object.entries(CATEGORY_LABELS).map(([key, label]) => (
                <option key={key} value={key}>{label}</option>
              ))}
            </select>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          
          {/* Popular templates */}
          {popularTemplates.length > 0 && (
            <div className="mb-8">
              <div className="flex items-center gap-2 mb-4">
                <Star className="w-4 h-4 text-amber-500" />
                <h3 className="text-sm font-bold text-slate-700">Популярные шаблоны</h3>
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                {popularTemplates.map(template => {
                  const TemplateIcon = template.icon;
                  const isSelected = selectedTemplate === template.id;
                  
                  return (
                    <button
                      key={template.id}
                      onClick={() => setSelectedTemplate(template.id)}
                      onDoubleClick={() => handleApply(template)}
                      className={`text-left p-4 rounded-xl border-2 transition-all ${
                        isSelected
                          ? 'border-indigo-500 bg-indigo-50 shadow-md'
                          : 'border-slate-200 bg-white hover:border-indigo-300 hover:shadow-sm'
                      }`}
                    >
                      <div className="flex items-start gap-3 mb-3">
                        <div className={`w-10 h-10 rounded-lg flex items-center justify-center shrink-0 ${
                          isSelected ? 'bg-indigo-500' : 'bg-slate-100'
                        }`}>
                          <TemplateIcon className={`w-5 h-5 ${isSelected ? 'text-white' : 'text-indigo-600'}`} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <h4 className="text-sm font-semibold text-slate-800 mb-0.5 truncate">{template.name}</h4>
                          <p className="text-xs text-slate-500 line-clamp-2">{template.description}</p>
                        </div>
                      </div>
                      
                      <div className="flex flex-wrap gap-1">
                        {template.tags.slice(0, 3).map(tag => (
                          <span key={tag} className="text-[10px] px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                            {tag}
                          </span>
                        ))}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Other templates */}
          {otherTemplates.length > 0 && (
            <div>
              <h3 className="text-sm font-bold text-slate-700 mb-4">Все шаблоны</h3>
              
              <div className="space-y-2">
                {otherTemplates.map(template => {
                  const TemplateIcon = template.icon;
                  const isSelected = selectedTemplate === template.id;
                  
                  return (
                    <button
                      key={template.id}
                      onClick={() => setSelectedTemplate(template.id)}
                      onDoubleClick={() => handleApply(template)}
                      className={`w-full text-left p-4 rounded-xl border transition-all ${
                        isSelected
                          ? 'border-indigo-500 bg-indigo-50 shadow-sm'
                          : 'border-slate-200 bg-white hover:border-slate-300'
                      }`}
                    >
                      <div className="flex items-center gap-3">
                        <div className={`w-9 h-9 rounded-lg flex items-center justify-center shrink-0 ${
                          isSelected ? 'bg-indigo-500' : 'bg-slate-100'
                        }`}>
                          <TemplateIcon className={`w-4 h-4 ${isSelected ? 'text-white' : 'text-indigo-600'}`} />
                        </div>
                        
                        <div className="flex-1 min-w-0">
                          <h4 className="text-sm font-semibold text-slate-800 mb-0.5">{template.name}</h4>
                          <p className="text-xs text-slate-500">{template.description}</p>
                        </div>

                        <div className="shrink-0">
                          <span className="text-[10px] px-2 py-1 rounded-md bg-slate-100 text-slate-600 font-medium">
                            {CATEGORY_LABELS[template.category]}
                          </span>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {filteredTemplates.length === 0 && (
            <div className="text-center py-16 text-slate-400">
              <BookOpen className="w-16 h-16 mx-auto mb-4 text-slate-300" />
              <p className="text-base font-medium mb-1">Шаблоны не найдены</p>
              <p className="text-sm">Попробуйте изменить параметры поиска или фильтры</p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-200 bg-slate-50 flex items-center justify-between">
          <div className="text-xs text-slate-500">
            {selectedTemplate ? (
              <div className="flex items-center gap-2">
                <CheckCircle className="w-4 h-4 text-green-600" />
                <span>Выбран: <b>{selected?.name}</b></span>
              </div>
            ) : (
              <span>Выберите шаблон для применения</span>
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
              onClick={() => selected && handleApply(selected)}
              disabled={!selectedTemplate}
              className={`flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-semibold transition-all ${
                !selectedTemplate
                  ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                  : 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-sm'
              }`}
            >
              <Download className="w-4 h-4" />
              Применить шаблон
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
