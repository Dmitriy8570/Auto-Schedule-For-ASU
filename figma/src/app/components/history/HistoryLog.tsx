
import React, { useState } from 'react';
import { History, Search, Filter, ChevronLeft, ChevronRight, Clock, User, Calendar } from 'lucide-react';
import { SearchableSelect } from '../ui/SearchableSelect';
import { institutes, departments, teachers } from '../../store/mockData';

interface HistoryItem {
  id: string;
  teacherId: string;
  action: 'add' | 'remove' | 'modify';
  description: string;
  timestamp: string;
  details: string;
}

// Generate some mock history
const mockHistory: HistoryItem[] = Array.from({ length: 50 }, (_, i) => ({
  id: `h${i}`,
  teacherId: teachers[i % teachers.length].id,
  action: i % 3 === 0 ? 'add' : i % 3 === 1 ? 'remove' : 'modify',
  description: i % 3 === 0 ? 'Добавлено занятие' : i % 3 === 1 ? 'Удалено занятие' : 'Изменено расписание',
  timestamp: new Date(Date.now() - i * 3600000).toISOString(),
  details: `Группа ИВТ-${300 + (i % 5)}, Ауд. ${300 + (i % 10)}`
}));

export function HistoryLog() {
  const [selectedInstitute, setSelectedInstitute] = useState('');
  const [selectedDepartment, setSelectedDepartment] = useState('');
  const [selectedTeacher, setSelectedTeacher] = useState('');
  const [page, setPage] = useState(1);
  const itemsPerPage = 10;

  // Filter Logic
  const filteredHistory = mockHistory.filter(item => {
    if (selectedTeacher && item.teacherId !== selectedTeacher) return false;
    
    if (!selectedTeacher) {
      const teacher = teachers.find(t => t.id === item.teacherId);
      if (!teacher) return false;
      if (selectedDepartment && teacher.departmentId !== selectedDepartment) return false;
      if (selectedInstitute) {
        const dept = departments.find(d => d.id === teacher.departmentId);
        if (dept?.instituteId !== selectedInstitute) return false;
      }
    }
    return true;
  });

  const totalPages = Math.ceil(filteredHistory.length / itemsPerPage);
  const currentItems = filteredHistory.slice((page - 1) * itemsPerPage, page * itemsPerPage);

  // Derived options for filters
  const availableDepartments = departments.filter(d => !selectedInstitute || d.instituteId === selectedInstitute);
  const availableTeachers = teachers.filter(t => {
    if (selectedDepartment) return t.departmentId === selectedDepartment;
    if (selectedInstitute) {
        const deptIds = departments.filter(d => d.instituteId === selectedInstitute).map(d => d.id);
        return deptIds.includes(t.departmentId);
    }
    return true;
  });

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-200 h-full flex flex-col">
      <div className="p-6 border-b border-slate-100">
        <h2 className="text-xl font-bold text-slate-800 mb-4 flex items-center gap-2">
          <History className="w-6 h-6 text-[rgb(26,77,156)]" />
          История изменений
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">Институт</label>
            <SearchableSelect
              options={institutes.map(i => ({ id: i.id, label: i.name }))}
              value={selectedInstitute}
              onChange={(val) => {
                setSelectedInstitute(val);
                setSelectedDepartment('');
                setSelectedTeacher('');
                setPage(1);
              }}
              placeholder="Все институты"
              onClear={() => setSelectedInstitute('')}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">Кафедра</label>
            <SearchableSelect
              options={availableDepartments.map(d => ({ id: d.id, label: d.name }))}
              value={selectedDepartment}
              onChange={(val) => {
                setSelectedDepartment(val);
                setSelectedTeacher('');
                setPage(1);
              }}
              placeholder="Все кафедры"
              disabled={!selectedInstitute}
              onClear={() => setSelectedDepartment('')}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">Преподаватель</label>
            <SearchableSelect
              options={availableTeachers.map(t => ({ id: t.id, label: t.name }))}
              value={selectedTeacher}
              onChange={(val) => {
                setSelectedTeacher(val);
                setPage(1);
              }}
              placeholder="Все преподаватели"
              disabled={!selectedInstitute && !selectedDepartment}
              onClear={() => setSelectedTeacher('')}
            />
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto p-4 space-y-4">
        {currentItems.length === 0 ? (
          <div className="text-center text-slate-400 py-10">История пуста</div>
        ) : (
          currentItems.map((item) => {
             const teacher = teachers.find(t => t.id === item.teacherId);
             const date = new Date(item.timestamp);
             
             return (
               <div key={item.id} className="bg-slate-50 p-4 rounded-lg border border-slate-200 flex items-start justify-between group hover:bg-white hover:shadow-sm transition-all">
                 <div className="flex gap-4">
                   <div className={`mt-1 p-2 rounded-full ${
                     item.action === 'add' ? 'bg-green-100 text-green-600' :
                     item.action === 'remove' ? 'bg-red-100 text-red-600' :
                     'bg-blue-100 text-blue-600'
                   }`}>
                     <History className="w-4 h-4" />
                   </div>
                   <div>
                     <h4 className="font-medium text-slate-800">{item.description}</h4>
                     <p className="text-sm text-slate-600 mb-1">{item.details}</p>
                     <div className="flex items-center gap-4 text-xs text-slate-400">
                       <span className="flex items-center gap-1">
                         <User className="w-3 h-3" />
                         {teacher?.name}
                       </span>
                       <span className="flex items-center gap-1">
                         <Calendar className="w-3 h-3" />
                         {date.toLocaleDateString()}
                       </span>
                       <span className="flex items-center gap-1">
                         <Clock className="w-3 h-3" />
                         {date.toLocaleTimeString()}
                       </span>
                     </div>
                   </div>
                 </div>
               </div>
             );
          })
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="p-4 border-t border-slate-100 flex items-center justify-between">
          <p className="text-xs text-slate-500">
            Показано {((page - 1) * itemsPerPage) + 1}-{Math.min(page * itemsPerPage, filteredHistory.length)} из {filteredHistory.length}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-2 border rounded-md disabled:opacity-50 hover:bg-slate-50"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <span className="px-4 py-2 text-sm font-medium text-slate-700 bg-slate-50 rounded-md">
              {page} / {totalPages}
            </span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-2 border rounded-md disabled:opacity-50 hover:bg-slate-50"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
