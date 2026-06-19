import React, { useState, useMemo, useRef, useEffect, useCallback } from 'react';
import {
  Plus, Trash2, Calendar, Save, X, Building2, GraduationCap,
  Users, User, MapPin, BookOpen, Sparkles, RotateCcw,
  ChevronDown, Download, ArrowLeftRight, CheckCircle,
  Info, Wrench, Clock, AlertTriangle, Moon, Sun,
} from 'lucide-react';
import { SearchableSelect } from '../ui/SearchableSelect';
import { teachers, rooms, groups, subjects, institutes, departments, buildings } from '../../store/mockData';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...i: (string | undefined | null | false)[]) { return twMerge(clsx(i)); }

// ─── Types & Constants ────────────────────────────────────────────────────────

type WeekType = 'red' | 'blue';
type ViewMode = 'teacher' | 'group' | 'room';
type LessonType = 'lecture' | 'practice' | 'lab' | 'exam' | 'consultation';
type EditPanelTab = 'lesson' | 'constraints';

const TIME_SLOTS = [
  { id: 1, start: '08:00', end: '09:30' }, { id: 2, start: '09:40', end: '11:10' },
  { id: 3, start: '11:20', end: '12:50' }, { id: 4, start: '13:20', end: '14:50' },
  { id: 5, start: '15:00', end: '16:30' }, { id: 6, start: '16:40', end: '18:10' },
  { id: 7, start: '18:20', end: '19:50' }, { id: 8, start: '20:00', end: '21:30' },
];
const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
const LESSON_COLORS: Record<LessonType, string> = {
  lecture: 'bg-blue-50 border-blue-500 text-blue-900',
  practice: 'bg-green-50 border-green-500 text-green-900',
  lab: 'bg-purple-50 border-purple-500 text-purple-900',
  exam: 'bg-red-50 border-red-500 text-red-900',
  consultation: 'bg-yellow-50 border-yellow-500 text-yellow-900',
};
const LESSON_LABELS: Record<LessonType, string> = {
  lecture: 'Лекция', practice: 'Практика', lab: 'Лаб.', exam: 'Экзамен', consultation: 'Консульт.',
};
const CURRENT_WEEK = 5; // Simulated current week
const TOTAL_WEEKS = 18;

interface ScheduleItem {
  id: string;
  subjectId: string;
  roomId: string;
  groupId: string;
  teacherId: string;
  type: LessonType;
}
type SlotItems = ScheduleItem[];
type DayGrid = Record<number, SlotItems>;
type WeekGrid = Record<string, DayGrid>;
type ScheduleData = Record<WeekType, WeekGrid>;

// ─── ScheduleEditor ───────────────────────────────────────────────────────────

export function ScheduleEditor() {
  // Schedule state (per week type)
  const [schedule, setSchedule] = useState<ScheduleData>({ red: {}, blue: {} });
  const [savedSchedule, setSavedSchedule] = useState<ScheduleData>({ red: {}, blue: {} });

  // Week
  const [weekNumber, setWeekNumber] = useState(CURRENT_WEEK);
  const weekType: WeekType = weekNumber % 2 === 1 ? 'red' : 'blue';
  const currentGrid = schedule[weekType];

  // View + entity selection (persist per tab)
  const [viewMode, setViewMode] = useState<ViewMode>('teacher');
  const [selectedTeacherId, setSelectedTeacherId] = useState('');
  const [selectedGroupId, setSelectedGroupId] = useState('');
  const [selectedRoomId, setSelectedRoomId] = useState('');

  // Filters (persist across tab switches)
  const [fTeachInst, setFTeachInst] = useState('');
  const [fTeachDept, setFTeachDept] = useState('');
  const [fGroupInst, setFGroupInst] = useState('');
  const [fGroupEdu, setFGroupEdu] = useState('');
  const [fGroupCourse, setFGroupCourse] = useState<number | ''>('');
  const [fRoomBuilding, setFRoomBuilding] = useState('');

  // Edit panel
  const [editPanel, setEditPanel] = useState<{ day: string; timeId: number; isNew: boolean } | null>(null);
  const [editForm, setEditForm] = useState<Partial<ScheduleItem>>({ type: 'lecture' });
  const [editDay, setEditDay] = useState(DAYS[0]);
  const [editTimeId, setEditTimeId] = useState(1);
  const [editPanelTab, setEditPanelTab] = useState<EditPanelTab>('lesson');

  // Selection & move
  const [selectedPair, setSelectedPair] = useState<{ day: string; timeId: number; item: ScheduleItem } | null>(null);
  const [moveMode, setMoveMode] = useState(false);
  const [movingPair, setMovingPair] = useState<{ day: string; timeId: number; item: ScheduleItem } | null>(null);

  // Auto-gen dropdown
  const [showAutoMenu, setShowAutoMenu] = useState(false);
  const autoMenuRef = useRef<HTMLDivElement>(null);

  // Export flash
  const [exportFlash, setExportFlash] = useState(false);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (autoMenuRef.current && !autoMenuRef.current.contains(e.target as Node)) setShowAutoMenu(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  // ── Derived ──
  const selectedEntityId = viewMode === 'teacher' ? selectedTeacherId : viewMode === 'group' ? selectedGroupId : selectedRoomId;

  const availableDepts = useMemo(() => departments.filter(d => !fTeachInst || d.instituteId === fTeachInst), [fTeachInst]);
  const availableTeachers = useMemo(() => teachers.filter(t => {
    if (fTeachDept) return t.departmentId === fTeachDept;
    if (fTeachInst) { const ids = departments.filter(d => d.instituteId === fTeachInst).map(d => d.id); return ids.includes(t.departmentId); }
    return true;
  }), [fTeachInst, fTeachDept]);
  const availableGroups = useMemo(() => groups.filter(g => {
    if (fGroupInst && g.instituteId !== fGroupInst) return false;
    if (fGroupEdu && (g.educationLevel ?? 'bachelor') !== fGroupEdu) return false;
    if (fGroupCourse && g.course !== fGroupCourse) return false;
    return true;
  }), [fGroupInst, fGroupEdu, fGroupCourse]);
  const availableRooms = useMemo(() => rooms.filter(r => !fRoomBuilding || r.buildingId === fRoomBuilding), [fRoomBuilding]);

  // ── Helpers ──
  const getItemAt = useCallback((day: string, timeId: number): ScheduleItem | null => {
    const items = currentGrid[day]?.[timeId] ?? [];
    if (!selectedEntityId) return null;
    if (viewMode === 'teacher') return items.find(i => i.teacherId === selectedEntityId) ?? null;
    if (viewMode === 'group') return items.find(i => i.groupId === selectedEntityId) ?? null;
    return items.find(i => i.roomId === selectedEntityId) ?? null;
  }, [currentGrid, selectedEntityId, viewMode]);

  const hasConflict = useCallback((day: string, timeId: number, item: ScheduleItem, excludeFrom?: { day: string; timeId: number }): boolean => {
    const allItems = (currentGrid[day]?.[timeId] ?? []).filter(x => {
      if (!excludeFrom) return true;
      return !(excludeFrom.day === day && excludeFrom.timeId === timeId && x.id === item.id);
    });
    return allItems.some(x => x.roomId === item.roomId || x.teacherId === item.teacherId || x.groupId === item.groupId);
  }, [currentGrid]);

  const updateSlot = (day: string, timeId: number, updater: (items: ScheduleItem[]) => ScheduleItem[]) => {
    setSchedule(prev => {
      const weekGrid = prev[weekType];
      const dayGrid = weekGrid[day] ?? {};
      const items = dayGrid[timeId] ?? [];
      return { ...prev, [weekType]: { ...weekGrid, [day]: { ...dayGrid, [timeId]: updater(items) } } };
    });
  };

  // ── Cell click ──
  const handleCellClick = (day: string, timeId: number) => {
    if (moveMode && movingPair) {
      if (!hasConflict(day, timeId, movingPair.item, movingPair)) {
        // Execute move
        updateSlot(movingPair.day, movingPair.timeId, items => items.filter(x => x.id !== movingPair.item.id));
        updateSlot(day, timeId, items => {
          const filtered = items.filter(x => x.teacherId !== movingPair.item.teacherId && x.groupId !== movingPair.item.groupId && x.roomId !== movingPair.item.roomId);
          return [...filtered, movingPair.item];
        });
        setMoveMode(false); setMovingPair(null); setSelectedPair({ day, timeId, item: movingPair.item });
      }
      return;
    }

    const item = getItemAt(day, timeId);
    if (item) {
      setSelectedPair({ day, timeId, item });
      setEditPanel(null);
    } else {
      if (!selectedEntityId) return;
      setSelectedPair(null);
      setEditDay(day); setEditTimeId(timeId);
      setEditPanel({ day, timeId, isNew: true });
      setEditForm({
        type: 'lecture',
        teacherId: viewMode === 'teacher' ? selectedEntityId : '',
        groupId: viewMode === 'group' ? selectedEntityId : '',
        roomId: viewMode === 'room' ? selectedEntityId : '',
      });
      setEditPanelTab('lesson');
    }
  };

  // ── Add pair button ──
  const handleAddPair = () => {
    if (!selectedEntityId) return;
    setSelectedPair(null);
    setEditPanel({ day: editDay, timeId: editTimeId, isNew: true });
    setEditForm({
      type: 'lecture',
      teacherId: viewMode === 'teacher' ? selectedEntityId : '',
      groupId: viewMode === 'group' ? selectedEntityId : '',
      roomId: viewMode === 'room' ? selectedEntityId : '',
    });
    setEditPanelTab('lesson');
  };

  // ── Save pair ──
  const handleSavePair = () => {
    const targetDay = editPanel?.day || editDay;
    const targetTimeId = editPanel?.timeId || editTimeId;
    if (!editForm.subjectId || !editForm.roomId || !editForm.groupId || !editForm.teacherId) return;

    const newItem: ScheduleItem = {
      id: editForm.id || `item_${Date.now()}`,
      subjectId: editForm.subjectId,
      roomId: editForm.roomId,
      groupId: editForm.groupId,
      teacherId: editForm.teacherId,
      type: editForm.type as LessonType,
    };

    updateSlot(targetDay, targetTimeId, items => {
      const filtered = items.filter(x => {
        if (viewMode === 'teacher') return x.teacherId !== selectedEntityId;
        if (viewMode === 'group') return x.groupId !== selectedEntityId;
        return x.roomId !== selectedEntityId;
      });
      return [...filtered, newItem];
    });
    setEditPanel(null);
    setSelectedPair({ day: targetDay, timeId: targetTimeId, item: newItem });
  };

  // ── Delete pair ──
  const handleDeletePair = () => {
    if (!selectedPair) return;
    if (!confirm('Удалить эту пару?')) return;
    updateSlot(selectedPair.day, selectedPair.timeId, items => items.filter(x => x.id !== selectedPair.item.id));
    setSelectedPair(null);
  };

  // ── Move mode ──
  const handleStartMove = () => {
    if (!selectedPair) return;
    setMoveMode(true);
    setMovingPair(selectedPair);
    setEditPanel(null);
  };

  // ── Reset to saved ──
  const handleResetToSaved = () => {
    if (!confirm('Сбросить текущее расписание до последнего выгруженного состояния?')) return;
    setSchedule({ ...savedSchedule });
    setSelectedPair(null); setEditPanel(null); setMoveMode(false); setMovingPair(null);
  };

  // ── Export ──
  const handleExport = () => {
    setSavedSchedule({ ...schedule });
    setExportFlash(true);
    setTimeout(() => setExportFlash(false), 2500);
  };

  // ── Auto-generate ──
  const handleAutoGen = (scope: string) => {
    setShowAutoMenu(false);
    const label = scope === 'university' ? 'всего университета' : scope === 'teacher' ? 'выбранного преподавателя' : 'выбранного института';
    alert(`Автоматическая генерация расписания для ${label}. Функция будет реализована с учётом всех ограничений и предпочтений.`);
  };

  // ── Cell move color class ──
  const getCellMoveClass = (day: string, timeId: number): string => {
    if (!moveMode || !movingPair) return '';
    if (movingPair.day === day && movingPair.timeId === timeId) return 'ring-2 ring-blue-400 bg-blue-50';
    const conflict = hasConflict(day, timeId, movingPair.item, movingPair);
    return conflict ? 'bg-red-100 border-red-300' : 'bg-green-100 border-green-400';
  };

  // ── Cell content ──
  const getCellContent = (day: string, timeId: number) => {
    const item = getItemAt(day, timeId);
    if (!item) return null;
    const subject = subjects.find(s => s.id === item.subjectId);
    const room = rooms.find(r => r.id === item.roomId);
    const group = groups.find(g => g.id === item.groupId);
    const teacher = teachers.find(t => t.id === item.teacherId);
    const isSelected = selectedPair?.day === day && selectedPair?.timeId === timeId;
    const isMovingFrom = movingPair?.day === day && movingPair?.timeId === timeId;
    return (
      <div className={cn('p-2 text-xs h-full rounded border-l-4 flex flex-col gap-0.5 overflow-hidden transition-all',
        LESSON_COLORS[item.type], isSelected && 'ring-2 ring-[rgb(26,77,156)] ring-offset-1 shadow-lg', isMovingFrom && 'opacity-40')}>
        <span className="font-bold truncate leading-snug">{subject?.name}</span>
        {viewMode !== 'teacher' && <span className="truncate opacity-80 text-[10px]">{teacher?.name}</span>}
        {viewMode !== 'group' && <span className="truncate opacity-80 text-[10px]">{group?.name}</span>}
        {viewMode !== 'room' && <span className="truncate opacity-80 text-[10px]">Ауд. {room?.name}</span>}
        <span className="text-[9px] opacity-60 mt-auto">{LESSON_LABELS[item.type]}</span>
      </div>
    );
  };

  // ── Constraints view (in edit panel) ──
  const renderConstraintsView = () => {
    const teacher = teachers.find(t => t.id === editForm.teacherId);
    const room = rooms.find(r => r.id === editForm.roomId);
    const group = groups.find(g => g.id === editForm.groupId);
    return (
      <div className="space-y-4 p-4">
        {!teacher && !room && !group && (
          <div className="flex items-center gap-2 p-3 bg-slate-50 border border-slate-200 rounded-lg">
            <Info className="w-4 h-4 text-slate-400 shrink-0" />
            <p className="text-sm text-slate-500">Выберите преподавателя, аудиторию и группу для просмотра ограничений.</p>
          </div>
        )}

        {teacher && (
          <div className="border border-blue-100 rounded-xl overflow-hidden">
            <div className="flex items-center gap-2 px-4 py-2.5 bg-blue-50 border-b border-blue-100">
              <User className="w-4 h-4 text-blue-600" />
              <span className="text-sm font-semibold text-blue-800">{teacher.name}</span>
            </div>
            <div className="p-3 space-y-2">
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Clock className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Доступность по времени: <span className="text-slate-400 italic">настраивается в разделе «Ограничения»</span></span>
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Info className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Кафедра: {departments.find(d => d.id === teacher.departmentId)?.name ?? '—'}</span>
              </div>
            </div>
          </div>
        )}

        {room && (
          <div className="border border-purple-100 rounded-xl overflow-hidden">
            <div className="flex items-center gap-2 px-4 py-2.5 bg-purple-50 border-b border-purple-100">
              <MapPin className="w-4 h-4 text-purple-600" />
              <span className="text-sm font-semibold text-purple-800">Ауд. {room.name}</span>
            </div>
            <div className="p-3 space-y-2">
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Users className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Вместимость: {room.capacity} мест</span>
              </div>
              {room.properties && room.properties.length > 0 && (
                <div className="flex items-start gap-2 text-xs text-slate-600">
                  <Wrench className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                  <div className="flex flex-wrap gap-1">{room.properties.map(p => <span key={p} className="px-2 py-0.5 bg-purple-50 text-purple-700 rounded-full border border-purple-100">{p}</span>)}</div>
                </div>
              )}
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Building2 className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>{buildings.find(b => b.id === room.buildingId)?.name ?? '—'}</span>
              </div>
            </div>
          </div>
        )}

        {group && (
          <div className="border border-teal-100 rounded-xl overflow-hidden">
            <div className="flex items-center gap-2 px-4 py-2.5 bg-teal-50 border-b border-teal-100">
              <Users className="w-4 h-4 text-teal-600" />
              <span className="text-sm font-semibold text-teal-800">{group.name}</span>
            </div>
            <div className="p-3 space-y-2">
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Info className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Курс: {group.course} · {group.size ?? '?'} студ.</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-600">
                <Sun className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Смена: <span className="text-slate-400 italic">настраивается в разделе «Ограничения»</span></span>
              </div>
            </div>
          </div>
        )}

        <div className="p-3 bg-amber-50 border border-amber-100 rounded-lg">
          <p className="text-[11px] text-amber-700 flex items-start gap-1.5"><AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />Детальные ограничения (сетка доступности, оборудование, смена) настраиваются в разделе «Ограничения» боковой панели.</p>
        </div>
      </div>
    );
  };

  // ── Edit panel ──
  const renderEditPanel = () => {
    if (!editPanel) return null;
    const fromCell = editPanel.day !== '';
    const targetDay = fromCell ? editPanel.day : editDay;
    const targetSlot = fromCell ? TIME_SLOTS.find(s => s.id === editPanel.timeId) : TIME_SLOTS.find(s => s.id === editTimeId);
    const isFormValid = !!editForm.subjectId && !!editForm.roomId && !!editForm.groupId && !!editForm.teacherId;

    return (
      <div className="w-96 shrink-0 border-l border-slate-200 flex flex-col bg-white overflow-hidden">
        {/* Panel header */}
        <div className="bg-[rgb(26,77,156)] text-white px-4 py-3 flex items-center gap-3 shrink-0">
          <div className="flex-1">
            <p className="text-xs opacity-75 mb-0.5">{editPanel.isNew ? 'Добавление пары' : 'Редактирование'}</p>
            {fromCell ? (
              <p className="font-semibold text-sm">{targetDay} · Пара {editPanel.timeId} ({targetSlot?.start}–{targetSlot?.end})</p>
            ) : (
              <div className="flex gap-2">
                <select value={editDay} onChange={e => setEditDay(e.target.value)} className="text-xs bg-white/20 border border-white/30 rounded-lg px-2 py-1 outline-none">{DAYS.map(d => <option key={d} value={d}>{d}</option>)}</select>
                <select value={editTimeId} onChange={e => setEditTimeId(Number(e.target.value))} className="text-xs bg-white/20 border border-white/30 rounded-lg px-2 py-1 outline-none">{TIME_SLOTS.map(s => <option key={s.id} value={s.id}>{s.id} пара ({s.start})</option>)}</select>
              </div>
            )}
          </div>
          <button onClick={() => setEditPanel(null)} className="text-white/70 hover:text-white p-1 rounded transition-colors"><X className="w-4 h-4" /></button>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-slate-100 shrink-0">
          {[{ val: 'lesson' as const, label: 'Занятие' }, { val: 'constraints' as const, label: 'Ограничения' }].map(t => (
            <button key={t.val} onClick={() => setEditPanelTab(t.val)}
              className={`flex-1 py-2.5 text-xs font-semibold border-b-2 transition-all ${editPanelTab === t.val ? 'border-[rgb(26,77,156)] text-[rgb(26,77,156)] bg-blue-50/40' : 'border-transparent text-slate-500 hover:text-slate-700 hover:bg-slate-50'}`}>
              {t.label}
            </button>
          ))}
        </div>

        {/* Tab content */}
        <div className="flex-1 overflow-y-auto">
          {editPanelTab === 'lesson' && (
            <div className="p-4 space-y-4">
              {/* Subject */}
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Дисциплина</label>
                <SearchableSelect options={subjects.map(s => ({ id: s.id, label: s.name }))} value={editForm.subjectId ?? ''} onChange={val => setEditForm(f => ({ ...f, subjectId: val }))} placeholder="Выберите предмет…" />
              </div>

              {/* Lesson type */}
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Тип занятия</label>
                <div className="grid grid-cols-5 gap-1">
                  {(['lecture', 'practice', 'lab', 'exam', 'consultation'] as LessonType[]).map(type => (
                    <button key={type} onClick={() => setEditForm(f => ({ ...f, type }))}
                      className={cn('py-1.5 text-[10px] font-medium rounded-md border transition-all', editForm.type === type ? 'bg-[rgb(26,77,156)] text-white border-transparent' : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300')}>
                      {LESSON_LABELS[type]}
                    </button>
                  ))}
                </div>
              </div>

              {/* Teacher */}
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Преподаватель</label>
                <SearchableSelect options={teachers.map(t => ({ id: t.id, label: t.name }))} value={editForm.teacherId ?? ''} onChange={val => setEditForm(f => ({ ...f, teacherId: val }))} placeholder="Выберите преподавателя…" />
              </div>

              {/* Group */}
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Группа</label>
                <SearchableSelect options={groups.map(g => ({ id: g.id, label: g.name }))} value={editForm.groupId ?? ''} onChange={val => setEditForm(f => ({ ...f, groupId: val }))} placeholder="Выберите группу…" />
              </div>

              {/* Room */}
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Аудитория</label>
                <SearchableSelect options={rooms.map(r => ({ id: r.id, label: `Ауд. ${r.name} (${r.capacity} мест)` }))} value={editForm.roomId ?? ''} onChange={val => setEditForm(f => ({ ...f, roomId: val }))} placeholder="Выберите аудиторию…" />
              </div>
            </div>
          )}
          {editPanelTab === 'constraints' && renderConstraintsView()}
        </div>

        {/* Panel footer */}
        <div className="px-4 py-3 bg-slate-50 border-t border-slate-100 flex gap-2 shrink-0">
          <button onClick={() => setEditPanel(null)} className="flex-1 py-2 text-slate-600 hover:bg-slate-200 rounded-lg text-sm transition-colors">Отмена</button>
          <button onClick={handleSavePair} disabled={!isFormValid}
            className={cn('flex-1 py-2 rounded-lg text-sm font-semibold transition-all flex items-center justify-center gap-1.5', isFormValid ? 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm' : 'bg-slate-100 text-slate-400 cursor-not-allowed')}>
            <Save className="w-4 h-4" />Сохранить
          </button>
        </div>
      </div>
    );
  };

  // ── Render ──
  return (
    <div className="h-full flex flex-col bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">

      {/* ── Row 1: Week selector + actions ── */}
      <div className="px-4 py-3 border-b border-slate-100 flex items-center justify-between gap-4 shrink-0 flex-wrap">
        {/* Week selector */}
        <div className="flex items-center gap-3 flex-wrap">
          <div className="flex items-center gap-2">
            <Calendar className="w-4 h-4 text-slate-400" />
            <span className="text-xs font-medium text-slate-500 whitespace-nowrap">Неделя:</span>
            <select value={weekNumber} onChange={e => setWeekNumber(Number(e.target.value))}
              className="text-sm border border-slate-200 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30 font-medium">
              {Array.from({ length: TOTAL_WEEKS }, (_, i) => i + 1).map(w => (
                <option key={w} value={w}>{w} неделя{w === CURRENT_WEEK ? ' ★' : ''}</option>
              ))}
            </select>
          </div>
          {/* Week type badge */}
          <span className={cn('flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold border', weekType === 'red' ? 'bg-red-50 text-red-700 border-red-200' : 'bg-blue-50 text-blue-700 border-blue-200')}>
            {weekType === 'red' ? <Moon className="w-3.5 h-3.5" /> : <Sun className="w-3.5 h-3.5" />}
            {weekType === 'red' ? 'Красная неделя' : 'Синяя неделя'}
          </span>
          {weekNumber === CURRENT_WEEK && <span className="text-xs text-green-600 font-semibold flex items-center gap-1"><CheckCircle className="w-3.5 h-3.5" />Текущая</span>}
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 flex-wrap">
          <button onClick={handleResetToSaved} className="flex items-center gap-1.5 px-3 py-2 text-slate-600 hover:bg-slate-100 rounded-lg text-xs font-medium transition-colors border border-slate-200 whitespace-nowrap">
            <RotateCcw className="w-3.5 h-3.5" />Сбросить до выгруженного
          </button>

          {/* Auto-gen dropdown */}
          <div className="relative" ref={autoMenuRef}>
            <button onClick={() => setShowAutoMenu(v => !v)}
              className="flex items-center gap-1.5 px-3 py-2 bg-gradient-to-r from-purple-600 to-[rgb(26,77,156)] text-white rounded-lg text-xs font-medium transition-all shadow-sm hover:shadow-md whitespace-nowrap">
              <Sparkles className="w-3.5 h-3.5" />Автогенерация<ChevronDown className={cn('w-3.5 h-3.5 transition-transform', showAutoMenu && 'rotate-180')} />
            </button>
            {showAutoMenu && (
              <div className="absolute right-0 top-full mt-1.5 z-50 bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden w-56">
                <div className="p-1.5 space-y-0.5">
                  {[
                    { key: 'university', icon: GraduationCap, label: 'Для всего университета' },
                    { key: 'teacher', icon: User, label: 'Для выбранного преподавателя' },
                    { key: 'institute', icon: Building2, label: 'Для выбранного института' },
                  ].map(opt => {
                    const OIcon = opt.icon;
                    return <button key={opt.key} onClick={() => handleAutoGen(opt.key)} className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-slate-700 hover:bg-[rgb(26,77,156)]/5 hover:text-[rgb(26,77,156)] transition-colors"><OIcon className="w-4 h-4 shrink-0" />{opt.label}</button>;
                  })}
                </div>
              </div>
            )}
          </div>

          <button onClick={handleExport}
            className={cn('flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium transition-all border whitespace-nowrap', exportFlash ? 'bg-green-100 text-green-700 border-green-200' : 'bg-white text-slate-700 hover:bg-slate-50 border-slate-200 shadow-sm')}>
            {exportFlash ? <><CheckCircle className="w-3.5 h-3.5" />Выгружено!</> : <><Download className="w-3.5 h-3.5" />Выгрузить</>}
          </button>
        </div>
      </div>

      {/* ── Row 2: Big view tabs ── */}
      <div className="px-4 py-2.5 border-b border-slate-100 flex gap-2 shrink-0">
        {([
          { mode: 'teacher' as ViewMode, label: 'Преподаватели', icon: User, color: 'blue' },
          { mode: 'group'   as ViewMode, label: 'Группы',        icon: Users, color: 'teal' },
          { mode: 'room'    as ViewMode, label: 'Аудитории',     icon: MapPin, color: 'purple' },
        ]).map(tab => {
          const TIcon = tab.icon;
          const active = viewMode === tab.mode;
          return (
            <button key={tab.mode} onClick={() => setViewMode(tab.mode)}
              className={cn('flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border-2 transition-all',
                active
                  ? 'bg-[rgb(26,77,156)] text-white border-[rgb(26,77,156)] shadow-md'
                  : 'bg-white text-slate-600 border-slate-200 hover:border-[rgb(26,77,156)]/30 hover:bg-slate-50')}>
              <TIcon className={cn('w-4 h-4', active ? 'text-white' : 'text-slate-400')} />
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* ── Row 3: Filters + Entity selector + Toolbar ── */}
      <div className="px-4 py-2.5 border-b border-slate-100 flex items-center justify-between gap-4 flex-wrap shrink-0 bg-slate-50/50">
        {/* Filters */}
        <div className="flex items-center gap-3 flex-wrap">
          {viewMode === 'teacher' && (
            <>
              <div className="w-44">
                <SearchableSelect options={institutes.map(i => ({ id: i.id, label: i.name }))} value={fTeachInst} onChange={v => { setFTeachInst(v); setFTeachDept(''); }} placeholder="Все институты" onClear={() => { setFTeachInst(''); setFTeachDept(''); }} />
              </div>
              <div className="w-44">
                <SearchableSelect options={availableDepts.map(d => ({ id: d.id, label: d.name }))} value={fTeachDept} onChange={setFTeachDept} placeholder="Все кафедры" disabled={!fTeachInst} onClear={() => setFTeachDept('')} />
              </div>
              <div className="w-52">
                <SearchableSelect options={availableTeachers.map(t => ({ id: t.id, label: t.name }))} value={selectedTeacherId} onChange={setSelectedTeacherId} placeholder="Выберите преподавателя" />
              </div>
            </>
          )}
          {viewMode === 'group' && (
            <>
              <div className="w-40">
                <SearchableSelect options={institutes.map(i => ({ id: i.id, label: i.name }))} value={fGroupInst} onChange={v => { setFGroupInst(v); setFGroupEdu(''); setFGroupCourse(''); }} placeholder="Все институты" onClear={() => { setFGroupInst(''); }} />
              </div>
              <select value={fGroupEdu} onChange={e => { setFGroupEdu(e.target.value); setFGroupCourse(''); }}
                className="h-10 border border-slate-200 rounded-lg text-sm px-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
                <option value="">Все уровни</option>
                <option value="bachelor">Бакалавриат</option>
                <option value="master">Магистратура</option>
                <option value="phd">Аспирантура</option>
                <option value="specialist">Специалитет</option>
              </select>
              <select value={fGroupCourse} onChange={e => setFGroupCourse(Number(e.target.value) || '')}
                className="h-10 border border-slate-200 rounded-lg text-sm px-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
                <option value="">Все курсы</option>
                {[1, 2, 3, 4, 5].map(c => <option key={c} value={c}>{c} курс</option>)}
              </select>
              <div className="w-40">
                <SearchableSelect options={availableGroups.map(g => ({ id: g.id, label: g.name }))} value={selectedGroupId} onChange={setSelectedGroupId} placeholder="Выберите группу" />
              </div>
            </>
          )}
          {viewMode === 'room' && (
            <>
              <div className="w-44">
                <SearchableSelect options={buildings.map(b => ({ id: b.id, label: b.name }))} value={fRoomBuilding} onChange={v => { setFRoomBuilding(v); setSelectedRoomId(''); }} placeholder="Все корпусы" onClear={() => setFRoomBuilding('')} />
              </div>
              <div className="w-44">
                <SearchableSelect options={availableRooms.map(r => ({ id: r.id, label: `Ауд. ${r.name}` }))} value={selectedRoomId} onChange={setSelectedRoomId} placeholder="Выберите аудиторию" disabled={!fRoomBuilding} />
              </div>
            </>
          )}
        </div>

        {/* Toolbar */}
        <div className="flex items-center gap-2">
          {moveMode && (
            <>
              <div className="flex items-center gap-2 px-3 py-2 bg-blue-50 border border-blue-200 rounded-lg text-xs text-blue-700 font-medium">
                <ArrowLeftRight className="w-3.5 h-3.5" />
                <span>Режим перемещения</span>
                <span className="text-[10px] opacity-70">· Зелёный = доступно · Красный = конфликт</span>
              </div>
              <button onClick={() => { setMoveMode(false); setMovingPair(null); }} className="flex items-center gap-1.5 px-3 py-2 text-slate-600 hover:bg-slate-100 rounded-lg text-xs font-medium transition-colors border border-slate-200">
                <X className="w-3.5 h-3.5" />Отмена
              </button>
            </>
          )}
          {selectedPair && !moveMode && (
            <>
              <button onClick={handleDeletePair} className="flex items-center gap-1.5 px-3 py-2 text-red-600 hover:bg-red-50 rounded-lg text-xs font-medium transition-colors border border-red-200">
                <Trash2 className="w-3.5 h-3.5" />Удалить пару
              </button>
              <button onClick={handleStartMove} className="flex items-center gap-1.5 px-3 py-2 text-blue-600 hover:bg-blue-50 rounded-lg text-xs font-medium transition-colors border border-blue-200">
                <ArrowLeftRight className="w-3.5 h-3.5" />Переместить
              </button>
            </>
          )}
          {!moveMode && (
            <button onClick={handleAddPair} disabled={!selectedEntityId}
              className={cn('flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-semibold transition-all border', selectedEntityId ? 'bg-[rgb(26,77,156)] text-white border-[rgb(26,77,156)] hover:bg-[rgb(20,60,130)] shadow-sm' : 'bg-slate-100 text-slate-400 border-slate-200 cursor-not-allowed')}>
              <Plus className="w-3.5 h-3.5" />Добавить пару
            </button>
          )}
        </div>
      </div>

      {/* ── Grid + Edit panel ── */}
      <div className="flex-1 flex overflow-hidden">
        {/* Grid area */}
        <div className="flex-1 overflow-auto p-4">
          {!selectedEntityId ? (
            <div className="h-full flex flex-col items-center justify-center text-center">
              <div className="w-16 h-16 rounded-2xl bg-[rgb(26,77,156)]/10 flex items-center justify-center mb-4">
                {viewMode === 'teacher' ? <User className="w-8 h-8 text-[rgb(26,77,156)]/50" /> : viewMode === 'group' ? <Users className="w-8 h-8 text-[rgb(26,77,156)]/50" /> : <MapPin className="w-8 h-8 text-[rgb(26,77,156)]/50" />}
              </div>
              <h3 className="text-base font-semibold text-slate-600 mb-2">
                Выберите {viewMode === 'teacher' ? 'преподавателя' : viewMode === 'group' ? 'группу' : 'аудиторию'}
              </h3>
              <p className="text-sm text-slate-400 max-w-xs">Используйте фильтры выше для поиска и выбора объекта расписания.</p>
            </div>
          ) : (
            <div className="min-w-[700px]">
              {/* Week type indicator bar */}
              <div className={cn('mb-3 px-3 py-1.5 rounded-lg text-xs font-medium flex items-center gap-2', weekType === 'red' ? 'bg-red-50 text-red-700 border border-red-100' : 'bg-blue-50 text-blue-700 border border-blue-100')}>
                {weekType === 'red' ? <Moon className="w-3.5 h-3.5" /> : <Sun className="w-3.5 h-3.5" />}
                {weekType === 'red' ? 'Красная неделя' : 'Синяя неделя'} · Неделя {weekNumber} из {TOTAL_WEEKS}
                {moveMode && <span className="ml-auto font-semibold text-blue-700">Выберите ячейку для перемещения</span>}
              </div>

              {/* Grid header */}
              <div className="grid grid-cols-[90px_repeat(6,1fr)] gap-2 mb-2">
                <div className="text-center text-xs font-semibold text-slate-400 py-2">Пара</div>
                {DAYS.map(d => (
                  <div key={d} className="text-center font-bold text-[rgb(26,77,156)] bg-blue-50 py-2 rounded-lg text-sm">{d}</div>
                ))}
              </div>

              {/* Grid rows */}
              <div className="space-y-1.5">
                {TIME_SLOTS.map(slot => (
                  <div key={slot.id} className="grid grid-cols-[90px_repeat(6,1fr)] gap-2 h-24">
                    {/* Time label */}
                    <div className="flex flex-col items-center justify-center text-xs font-medium text-slate-500 bg-slate-50 rounded-lg border border-slate-200">
                      <span className="text-base font-bold text-[rgb(26,77,156)]">{slot.id}</span>
                      <span className="text-[10px] mt-0.5 text-slate-400">{slot.start}</span>
                      <span className="text-slate-300 text-[10px]">—</span>
                      <span className="text-[10px] text-slate-400">{slot.end}</span>
                    </div>

                    {/* Day cells */}
                    {DAYS.map(day => {
                      const item = getItemAt(day, slot.id);
                      const moveClass = getCellMoveClass(day, slot.id);
                      const isSelectedCell = selectedPair?.day === day && selectedPair?.timeId === slot.id;
                      return (
                        <div key={`${day}-${slot.id}`}
                          onClick={() => handleCellClick(day, slot.id)}
                          className={cn(
                            'rounded-lg border transition-all cursor-pointer relative group',
                            item ? 'bg-white border-slate-200' : 'bg-slate-50/50 hover:bg-white border-slate-100',
                            moveClass,
                            isSelectedCell && !moveMode && 'ring-2 ring-[rgb(26,77,156)] ring-offset-1',
                            !moveMode && !item && selectedEntityId && 'hover:border-[rgb(26,77,156)]/30',
                          )}>
                          {getCellContent(day, slot.id)}
                          {!item && selectedEntityId && !moveMode && (
                            <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                              <Plus className="w-5 h-5 text-slate-300" />
                            </div>
                          )}
                          {/* Move mode overlay labels */}
                          {moveMode && !item && movingPair && (
                            <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                              {hasConflict(day, slot.id, movingPair.item, movingPair)
                                ? <span className="text-[10px] text-red-500 font-semibold">Конфликт</span>
                                : <span className="text-[10px] text-green-600 font-semibold">Доступно</span>}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Edit panel */}
        {editPanel && renderEditPanel()}
      </div>
    </div>
  );
}
