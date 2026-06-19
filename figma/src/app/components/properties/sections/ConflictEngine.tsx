import React, { useState } from 'react';
import {
  AlertTriangle, XCircle, CheckCircle, Info,
  ChevronDown, ChevronRight, Lightbulb, ArrowRight,
  X, RefreshCw, Zap, Shield, Clock, Route, Briefcase
} from 'lucide-react';
import type { DistributionRule } from './DistributionRules';
import type { ActivityLink } from './RelationshipBuilder';
import type { TravelRule, StreamConfig } from './LogisticsModule';
import type { ContractConfig } from './ContractModule';

export type Severity = 'critical' | 'warning' | 'info' | 'ok';
export type ConflictCategory = 'time' | 'contract' | 'distribution' | 'logistics' | 'relationship' | 'general';

export interface Suggestion {
  id: string;
  label: string;
  impact: 'high' | 'medium' | 'low';
  action?: () => void;
}

export interface ConflictItem {
  id: string;
  severity: Severity;
  category: ConflictCategory;
  message: string;
  details?: string;
  suggestions: Suggestion[];
  dismissed?: boolean;
}

// ─── Conflict Rules Engine ────────────────────────────────────────────────────

interface EngineInput {
  timeGrid: Record<string, string>;
  maxHoursPerDay: number;
  maxHoursPerWeek?: number;
  distributionRules: DistributionRule[];
  activityLinks: ActivityLink[];
  travelRules: TravelRule[];
  streams: StreamConfig[];
  contract: ContractConfig;
  entityType: string;
}

export function computeConflicts(input: EngineInput): ConflictItem[] {
  const items: ConflictItem[] = [];

  const prohibited = Object.values(input.timeGrid).filter(v => v === 'prohibited').length;
  const available = 48 - prohibited;
  const required = Object.values(input.timeGrid).filter(v => v === 'required').length;
  const preferred = Object.values(input.timeGrid).filter(v => v === 'preferred').length;

  // 1. Availability conflicts
  if (available < 6) {
    items.push({
      id: 'c_avail_critical',
      severity: 'critical',
      category: 'time',
      message: `Критически мало доступных слотов: ${available}/48`,
      details: 'Автосоставитель не сможет разместить стандартную нагрузку (мин. 6 слотов).',
      suggestions: [
        { id: 's1', label: 'Снять запрет с субботних слотов', impact: 'high' },
        { id: 's2', label: 'Перевести «Запрещено» → «Нежелательно» для пятницы', impact: 'high' },
        { id: 's3', label: 'Разделить нагрузку между двумя преподавателями', impact: 'medium' },
      ],
    });
  } else if (available < 16) {
    items.push({
      id: 'c_avail_warn',
      severity: 'warning',
      category: 'time',
      message: `Мало доступных слотов: ${available}/48 (${Math.round(available / 48 * 100)}%)`,
      details: 'Ограниченная гибкость — сложно будет избежать конфликтов при высокой нагрузке.',
      suggestions: [
        { id: 's4', label: 'Разрешить 2–3 слота в пятницу после обеда', impact: 'medium' },
        { id: 's5', label: 'Перевести «Запрещено» → «Нежелательно» (сохранит гибкость)', impact: 'medium' },
      ],
    });
  }

  // 2. Too many prohibited relative to preferred/required
  if (prohibited > 32 && (required + preferred) < 8) {
    items.push({
      id: 'c_balance',
      severity: 'warning',
      category: 'time',
      message: `Дисбаланс: ${prohibited} запрещено, но только ${required + preferred} предпочтительных/обязательных`,
      details: 'Расписание будет «зажато» — высокий риск конфликтов с другими объектами.',
      suggestions: [
        { id: 's6', label: 'Пометить 5–10 слотов как «Предпочтительно»', impact: 'medium' },
        { id: 's7', label: 'Добавить «Обязательные» слоты в первые 2 пары каждый день', impact: 'high' },
      ],
    });
  }

  // 3. Contract conflicts
  if (input.entityType === 'teacher') {
    const { contract } = input;
    const isExternal = contract.contractType === 'external' || contract.contractType === 'hourly';

    if (contract.maxHoursPerDay > 8) {
      items.push({
        id: 'c_day_overload',
        severity: 'warning',
        category: 'contract',
        message: `Максимум ${contract.maxHoursPerDay} ч/день превышает норму (8 ч)`,
        details: 'Нормативный максимум рабочего дня по ТК РФ — 8 часов.',
        suggestions: [
          { id: 's8', label: 'Снизить лимит до 8 ч/день', impact: 'high' },
          { id: 's9', label: 'Разделить нагрузку на 2 дня', impact: 'medium' },
        ],
      });
    }

    if (isExternal && contract.maxHoursPerWeek > 18) {
      items.push({
        id: 'c_external_limit',
        severity: 'critical',
        category: 'contract',
        message: `Внешний совместитель: ${contract.maxHoursPerWeek} ч/нед — превышение лимита`,
        details: 'ТК РФ ст. 284: внешние совместители не могут работать более 18 ч/нед.',
        suggestions: [
          { id: 's10', label: 'Установить лимит ≤ 18 ч/нед', impact: 'high' },
          { id: 's11', label: 'Перевести на 0.5 ставки (штат)', impact: 'medium' },
        ],
      });
    }

    if (contract.allowedShifts.length === 1 && contract.allowedShifts[0] === 'evening') {
      items.push({
        id: 'c_evening_only',
        severity: 'info',
        category: 'contract',
        message: 'Только вечерняя смена — ограниченный выбор аудиторий',
        details: 'После 18:00 доступны не все корпуса. Проверьте актуальность графика уборки.',
        suggestions: [
          { id: 's12', label: 'Добавить дневную смену (хотя бы 1 день)', impact: 'medium' },
        ],
      });
    }

    if (contract.maxHoursPerDay > 0 && contract.maxHoursPerWeek > 0
      && contract.maxHoursPerDay * 5 < contract.maxHoursPerWeek) {
      items.push({
        id: 'c_impossible_week',
        severity: 'warning',
        category: 'contract',
        message: `Недостижимый лимит: ${contract.maxHoursPerDay} ч/день × 5 дней = ${contract.maxHoursPerDay * 5} < ${contract.maxHoursPerWeek} ч/нед`,
        details: 'Недельный лимит нельзя выполнить при текущем дневном лимите.',
        suggestions: [
          { id: 's13', label: `Увеличить дневной лимит до ${Math.ceil(contract.maxHoursPerWeek / 5)} ч`, impact: 'high' },
          { id: 's14', label: `Снизить недельный лимит до ${contract.maxHoursPerDay * 5} ч`, impact: 'medium' },
        ],
      });
    }
  }

  // 4. Distribution rule conflicts
  const conflicting = input.distributionRules.filter(r =>
    r.pattern === 'same_day' && input.distributionRules.some(r2 => r2.pattern === 'evenly_spread' && r2.id !== r.id)
  );
  if (conflicting.length > 0) {
    items.push({
      id: 'c_dist_contradict',
      severity: 'warning',
      category: 'distribution',
      message: 'Противоречие: «Один день» и «Равномерно» несовместимы',
      details: 'Нельзя одновременно концентрировать занятия в один день и равномерно распределять их.',
      suggestions: [
        { id: 's15', label: 'Удалить паттерн «Один день»', impact: 'high' },
        { id: 's16', label: 'Заменить на «Фронтзагрузка» (нач. недели)', impact: 'medium' },
      ],
    });
  }

  const highWeightRules = input.distributionRules.filter(r => r.weight >= 85);
  if (highWeightRules.length >= 3) {
    items.push({
      id: 'c_dist_overloaded',
      severity: 'warning',
      category: 'distribution',
      message: `${highWeightRules.length} правила с критическим весом (≥85%) — высокий риск неразрешимости`,
      details: 'Слишком много жёстких правил распределения ведут к «замораживанию» расписания.',
      suggestions: [
        { id: 's17', label: 'Снизить вес 1–2 правил до 60–70%', impact: 'high' },
        { id: 's18', label: 'Перевести одно правило в «Рекомендацию» (вес ≤40%)', impact: 'medium' },
      ],
    });
  }

  // 5. Travel/Logistics conflicts
  const longTravel = input.travelRules.filter(r => r.enforceBuffer && r.travelMinutes >= 40);
  if (longTravel.length > 0) {
    items.push({
      id: 'c_travel_long',
      severity: 'warning',
      category: 'logistics',
      message: `${longTravel.length} маршрут(а) ≥40 мин — занятия в смежных парах невозможны`,
      details: `Стандартная перемена 10 мин. При маршруте ${longTravel[0]?.travelMinutes} мин нужен пропуск минимум 2–3 пар.`,
      suggestions: [
        { id: 's19', label: 'Сгруппировать пары по корпусам в один день', impact: 'high' },
        { id: 's20', label: 'Перенести занятия в корпусе X в соседний день', impact: 'medium' },
        { id: 's21', label: 'Снизить буфер до «Рекомендательного»', impact: 'low' },
      ],
    });
  }

  // 6. Relationship conflicts
  const circularRisk = input.activityLinks.filter(l => !l.parentName || !l.childName);
  if (circularRisk.length > 0) {
    items.push({
      id: 'c_links_empty',
      severity: 'info',
      category: 'relationship',
      message: `${circularRisk.length} связк(и) активностей не заполнены до конца`,
      details: 'Пустые связки будут проигнорированы при составлении расписания.',
      suggestions: [
        { id: 's22', label: 'Заполнить или удалить пустые связки', impact: 'medium' },
      ],
    });
  }

  // 7. Stream conflicts
  const bigStreams = input.streams.filter(s => !s.isElective && s.totalStudents > 150 && s.splitCount < 3);
  if (bigStreams.length > 0) {
    items.push({
      id: 'c_stream_big',
      severity: 'warning',
      category: 'logistics',
      message: `Большой поток (${bigStreams[0].totalStudents} чел.) разбит только на ${bigStreams[0].splitCount} группы`,
      details: 'При норме 30 чел/аудитория требуется минимум 5 подгрупп.',
      suggestions: [
        { id: 's23', label: `Увеличить разбивку до ${Math.ceil(bigStreams[0].totalStudents / 30)} групп`, impact: 'high' },
        { id: 's24', label: 'Использовать лекционный зал на 500 мест', impact: 'medium' },
      ],
    });
  }

  // All clear
  if (items.length === 0) {
    items.push({
      id: 'c_ok',
      severity: 'ok',
      category: 'general',
      message: 'Все ограничения совместимы. Готово к автосоставлению расписания.',
      suggestions: [],
    });
  }

  return items;
}

// ─── UI ───────────────────────────────────────────────────────────────────────

const SEV_META: Record<Severity, { icon: React.FC<{ className?: string }>; bg: string; border: string; text: string; badge: string }> = {
  critical: { icon: XCircle,      bg: 'bg-red-50',    border: 'border-red-200',    text: 'text-red-700',    badge: 'bg-red-100 text-red-700 border-red-200' },
  warning:  { icon: AlertTriangle, bg: 'bg-amber-50',  border: 'border-amber-200',  text: 'text-amber-700',  badge: 'bg-amber-100 text-amber-700 border-amber-200' },
  info:     { icon: Info,          bg: 'bg-blue-50',   border: 'border-blue-200',   text: 'text-blue-700',   badge: 'bg-blue-100 text-blue-700 border-blue-200' },
  ok:       { icon: CheckCircle,   bg: 'bg-green-50',  border: 'border-green-200',  text: 'text-green-700',  badge: 'bg-green-100 text-green-700 border-green-200' },
};

const CAT_META: Record<ConflictCategory, { label: string; icon: React.FC<{ className?: string }> }> = {
  time:         { label: 'Время',          icon: Clock },
  contract:     { label: 'Контракт',       icon: Briefcase },
  distribution: { label: 'Распределение',  icon: RefreshCw },
  logistics:    { label: 'Логистика',      icon: Route },
  relationship: { label: 'Связки',         icon: Zap },
  general:      { label: 'Общее',          icon: Shield },
};

const IMPACT_COLOR: Record<'high' | 'medium' | 'low', string> = {
  high:   'bg-green-100 text-green-700 border-green-200',
  medium: 'bg-blue-100 text-blue-700 border-blue-200',
  low:    'bg-slate-100 text-slate-600 border-slate-200',
};

const IMPACT_LABEL: Record<'high' | 'medium' | 'low', string> = {
  high: 'Высокий эффект',
  medium: 'Средний',
  low: 'Низкий',
};

function ConflictCard({
  item,
  onDismiss,
}: {
  item: ConflictItem;
  onDismiss: () => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const sev = SEV_META[item.severity];
  const cat = CAT_META[item.category];
  const SevIcon = sev.icon;
  const CatIcon = cat.icon;

  if (item.severity === 'ok') {
    return (
      <div className={`flex items-center gap-3 p-3.5 rounded-xl border ${sev.bg} ${sev.border}`}>
        <SevIcon className={`w-5 h-5 shrink-0 ${sev.text}`} />
        <span className={`text-xs font-medium flex-1 ${sev.text}`}>{item.message}</span>
        <span className="text-[10px] bg-green-200 text-green-800 px-2 py-0.5 rounded-full font-semibold">✓ OK</span>
      </div>
    );
  }

  return (
    <div className={`rounded-xl border overflow-hidden ${sev.border}`}>
      {/* Header */}
      <div className={`${sev.bg} px-3 py-2.5 flex items-start gap-2.5`}>
        <SevIcon className={`w-4 h-4 shrink-0 mt-0.5 ${sev.text}`} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className={`text-[10px] px-1.5 py-0.5 rounded-full border font-medium inline-flex items-center gap-1 ${sev.badge}`}>
              <CatIcon className="w-2.5 h-2.5" />
              {cat.label}
            </span>
          </div>
          <p className={`text-xs font-semibold mt-1 ${sev.text}`}>{item.message}</p>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {item.details && (
            <button
              onClick={() => setExpanded(e => !e)}
              className={`p-1 rounded ${sev.text} opacity-60 hover:opacity-100`}
            >
              {expanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
            </button>
          )}
          <button
            onClick={onDismiss}
            className="p-1 rounded text-slate-400 hover:text-slate-600"
            title="Скрыть"
          >
            <X className="w-3 h-3" />
          </button>
        </div>
      </div>

      {/* Expanded details */}
      {expanded && item.details && (
        <div className="px-3 py-2 bg-white border-t border-slate-100">
          <p className="text-[11px] text-slate-600 leading-relaxed">{item.details}</p>
        </div>
      )}

      {/* Suggestions */}
      {item.suggestions.length > 0 && (
        <div className="bg-white px-3 py-2 border-t border-slate-100">
          <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wider mb-2 flex items-center gap-1">
            <Lightbulb className="w-3 h-3" />
            Предложения по решению
          </p>
          <div className="space-y-1.5">
            {item.suggestions.map(s => (
              <div
                key={s.id}
                className="flex items-center gap-2 p-2 bg-slate-50 rounded-lg border border-slate-100 hover:bg-slate-100 transition-colors cursor-pointer group"
                onClick={s.action}
              >
                <ArrowRight className="w-3 h-3 text-slate-400 shrink-0 group-hover:text-[rgb(26,77,156)] transition-colors" />
                <span className="flex-1 text-[11px] text-slate-700 group-hover:text-slate-900">{s.label}</span>
                <span className={`text-[9px] px-1.5 py-0.5 rounded border font-medium shrink-0 ${IMPACT_COLOR[s.impact]}`}>
                  {IMPACT_LABEL[s.impact]}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

interface Props {
  conflicts: ConflictItem[];
  onDismiss: (id: string) => void;
  onRefresh: () => void;
  lastChecked?: Date;
}

export function ConflictEngine({ conflicts, onDismiss, onRefresh, lastChecked }: Props) {
  const visible = conflicts.filter(c => !c.dismissed);
  const criticals = visible.filter(c => c.severity === 'critical').length;
  const warnings = visible.filter(c => c.severity === 'warning').length;
  const infos = visible.filter(c => c.severity === 'info').length;

  return (
    <div className="space-y-3">
      {/* Summary bar */}
      <div className="flex items-center gap-2 p-3 bg-slate-50 rounded-xl border border-slate-200">
        <div className="flex gap-3 flex-1">
          {criticals > 0 && (
            <span className="flex items-center gap-1 text-[11px] font-semibold text-red-700 bg-red-100 border border-red-200 px-2 py-1 rounded-lg">
              <XCircle className="w-3.5 h-3.5" />
              {criticals} критичных
            </span>
          )}
          {warnings > 0 && (
            <span className="flex items-center gap-1 text-[11px] font-semibold text-amber-700 bg-amber-100 border border-amber-200 px-2 py-1 rounded-lg">
              <AlertTriangle className="w-3.5 h-3.5" />
              {warnings} предупреждений
            </span>
          )}
          {infos > 0 && (
            <span className="flex items-center gap-1 text-[11px] font-semibold text-blue-700 bg-blue-100 border border-blue-200 px-2 py-1 rounded-lg">
              <Info className="w-3.5 h-3.5" />
              {infos} замечаний
            </span>
          )}
          {criticals + warnings + infos === 0 && (
            <span className="flex items-center gap-1 text-[11px] font-semibold text-green-700">
              <CheckCircle className="w-3.5 h-3.5" />
              Нет нарушений
            </span>
          )}
        </div>
        <button
          onClick={onRefresh}
          className="flex items-center gap-1.5 text-[11px] text-slate-500 hover:text-[rgb(26,77,156)] transition-colors px-2 py-1 rounded-lg hover:bg-white"
        >
          <RefreshCw className="w-3 h-3" />
          Проверить
        </button>
        {lastChecked && (
          <span className="text-[9px] text-slate-400">
            {lastChecked.toLocaleTimeString('ru', { hour: '2-digit', minute: '2-digit' })}
          </span>
        )}
      </div>

      {/* Conflict cards */}
      <div className="space-y-2">
        {visible.map(item => (
          <ConflictCard key={item.id} item={item} onDismiss={() => onDismiss(item.id)} />
        ))}
      </div>

      {visible.length === 0 && (
        <div className="text-center py-8 text-slate-400 text-sm">
          <CheckCircle className="w-8 h-8 mx-auto mb-2 text-green-400" />
          <p className="font-medium text-green-600">Всё в порядке</p>
          <p className="text-xs mt-1">Нажмите «Проверить», чтобы запустить анализ заново</p>
        </div>
      )}
    </div>
  );
}
