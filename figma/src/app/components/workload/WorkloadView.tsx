import React, { useState, useMemo } from 'react';
import { Search, Filter, BookOpen, Users, Clock, ChevronRight, BarChart3, Calculator } from 'lucide-react';
import { SearchableSelect } from '../ui/SearchableSelect';
import { institutes, departments, teachers, subjects, groups, Institute, Department, Teacher } from '../../store/mockData';
import { clsx } from 'clsx';

// Mock data for workload items (MMIS style)
interface WorkloadItem {
  id: string;
  teacherId: string;
  subjectId: string;
  groupId: string;
  type: 'lecture' | 'practice' | 'lab';
  hoursTotal: number;
  semester: number;
  weeklyHours: number[]; // Array of hours for each of the 18 weeks
}

const generateWeeklyHours = (total: number, weeks: number = 18): number[] => {
  const perWeek = Math.floor(total / weeks);
  const remainder = total % weeks;
  return Array(weeks).fill(0).map((_, i) => i < remainder ? perWeek + 1 : perWeek);
};

const mockWorkload: WorkloadItem[] = [
  { id: 'w1', teacherId: 't1', subjectId: 'math_analysis', groupId: 'ivt-301', type: 'lecture', hoursTotal: 36, semester: 1, weeklyHours: generateWeeklyHours(36) },
  { id: 'w2', teacherId: 't1', subjectId: 'math_analysis', groupId: 'ivt-301', type: 'practice', hoursTotal: 36, semester: 1, weeklyHours: generateWeeklyHours(36) },
  { id: 'w3', teacherId: 't2', subjectId: 'programming', groupId: 'ivt-301', type: 'lecture', hoursTotal: 36, semester: 1, weeklyHours: generateWeeklyHours(36) },
  { id: 'w4', teacherId: 't3', subjectId: 'db', groupId: 'pi-201', type: 'lecture', hoursTotal: 36, semester: 1, weeklyHours: generateWeeklyHours(36) },
];

const WEEKS = Array.from({ length: 18 }, (_, i) => i + 1);

export function WorkloadView() {
  // Filter States
  const [selectedInstitute, setSelectedInstitute] = useState<string>('');
  const [selectedDepartment, setSelectedDepartment] = useState<string>('');
  const [selectedTeacher, setSelectedTeacher] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState('');

  // Derived Data for Filters
  const availableDepartments = useMemo(() => 
    departments.filter(d => !selectedInstitute || d.instituteId === selectedInstitute),
    [selectedInstitute]
  );
  
  const availableTeachers = useMemo(() => 
    teachers.filter(t => {
      if (selectedDepartment) return t.departmentId === selectedDepartment;
      if (selectedInstitute) {
        const deptIds = departments.filter(d => d.instituteId === selectedInstitute).map(d => d.id);
        return deptIds.includes(t.departmentId);
      }
      return true;
    }),
    [selectedDepartment, selectedInstitute]
  );

  // Filter Logic
  const filteredWorkload = useMemo(() => mockWorkload.filter(item => {
    if (selectedTeacher && item.teacherId !== selectedTeacher) return false;
    
    // If no teacher selected, but institute/dept is, filter by those
    if (!selectedTeacher) {
      const teacher = teachers.find(t => t.id === item.teacherId);
      if (!teacher) return false;
      
      if (selectedDepartment && teacher.departmentId !== selectedDepartment) return false;
      
      if (selectedInstitute) {
        const dept = departments.find(d => d.id === teacher.departmentId);
        if (!dept || dept.instituteId !== selectedInstitute) return false;
      }
    }

    if (searchTerm) {
       const subjectName = subjects.find(s => s.id === item.subjectId)?.name.toLowerCase() || '';
       return subjectName.includes(searchTerm.toLowerCase());
    }

    return true;
  }), [selectedTeacher, selectedDepartment, selectedInstitute, searchTerm]);

  // Calculate Totals for the view
  const totalHours = filteredWorkload.reduce((sum, item) => sum + item.hoursTotal, 0);

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-200 h-full flex flex-col">
      <div className="p-6 border-b border-slate-100">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <BarChart3 className="w-6 h-6 text-[rgb(26,77,156)]" />
            <h2 className="text-xl font-bold text-slate-800">Нагрузка (ММИС)</h2>
          </div>
          <div className="flex items-center gap-2 bg-blue-50 px-3 py-1 rounded-lg text-[rgb(26,77,156)] text-sm font-medium">
            <Calculator className="w-4 h-4" />
            Всего часов: {totalHours}
          </div>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
           {/* Filters */}
           <div className="space-y-1">
             <label className="text-xs font-medium text-slate-500 ml-1">Институт</label>
             <SearchableSelect
               options={institutes.map(i => ({ id: i.id, label: i.name }))}
               value={selectedInstitute}
               onChange={(val) => {
                 setSelectedInstitute(val);
                 setSelectedDepartment('');
                 setSelectedTeacher('');
               }}
               placeholder="Все институты"
               onClear={() => setSelectedInstitute('')}
             />
           </div>

           <div className="space-y-1">
             <label className="text-xs font-medium text-slate-500 ml-1">Кафедра</label>
             <SearchableSelect
               options={availableDepartments.map(d => ({ id: d.id, label: d.name }))}
               value={selectedDepartment}
               onChange={(val) => {
                 setSelectedDepartment(val);
                 setSelectedTeacher('');
               }}
               placeholder="Все кафедры"
               disabled={!selectedInstitute}
               onClear={() => setSelectedDepartment('')}
             />
           </div>

           <div className="space-y-1">
             <label className="text-xs font-medium text-slate-500 ml-1">Преподаватель</label>
             <SearchableSelect
               options={availableTeachers.map(t => ({ id: t.id, label: t.name }))}
               value={selectedTeacher}
               onChange={setSelectedTeacher}
               placeholder="Все преподаватели"
               disabled={!selectedInstitute && !selectedDepartment}
               onClear={() => setSelectedTeacher('')}
             />
           </div>

           <div className="space-y-1">
             <label className="text-xs font-medium text-slate-500 ml-1">Поиск по предмету</label>
             <div className="relative">
               <Search className="absolute left-2 top-2.5 w-4 h-4 text-slate-400" />
               <input 
                 type="text" 
                 value={searchTerm}
                 onChange={(e) => setSearchTerm(e.target.value)}
                 placeholder="Название дисциплины..."
                 className="w-full pl-8 pr-3 py-2 text-sm border border-slate-300 rounded-md focus:ring-1 focus:ring-[rgb(26,77,156)] outline-none"
               />
             </div>
           </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto p-0">
        {filteredWorkload.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-64 text-slate-400">
            <Filter className="w-12 h-12 mb-2 opacity-20" />
            <p>Нет данных, соответствующих фильтрам</p>
          </div>
        ) : (
          <div className="w-full">
            <table className="w-full text-sm text-left border-collapse">
              <thead className="bg-slate-50 text-slate-500 font-medium sticky top-0 z-10 shadow-sm">
                <tr>
                  <th className="px-4 py-3 border-b min-w-[200px] bg-slate-50">Преподаватель / Предмет</th>
                  <th className="px-4 py-3 border-b min-w-[100px] bg-slate-50">Группа</th>
                  <th className="px-4 py-3 border-b w-[80px] bg-slate-50">Тип</th>
                  <th className="px-4 py-3 border-b w-[60px] bg-slate-50 text-right">Всего</th>
                  {WEEKS.map(week => (
                    <th key={week} className="px-2 py-3 border-b text-center min-w-[40px] text-xs bg-slate-50 border-l border-slate-200">
                      {week}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredWorkload.map((item) => {
                  const teacher = teachers.find(t => t.id === item.teacherId);
                  const subject = subjects.find(s => s.id === item.subjectId);
                  const group = groups.find(g => g.id === item.groupId);
                  
                  return (
                    <tr key={item.id} className="hover:bg-slate-50 transition-colors">
                      <td className="px-4 py-3 border-b">
                        <div className="font-medium text-slate-800">{teacher?.name}</div>
                        <div className="text-xs text-slate-500 flex items-center gap-1 mt-1">
                          <BookOpen className="w-3 h-3" />
                          {subject?.name}
                        </div>
                      </td>
                      <td className="px-4 py-3 border-b text-slate-600">
                         <div className="flex items-center gap-2">
                          <Users className="w-3 h-3 text-slate-400" />
                          {group?.name}
                        </div>
                      </td>
                      <td className="px-4 py-3 border-b">
                        <span className={clsx(
                          "px-2 py-1 rounded-full text-[10px] font-bold uppercase tracking-wider",
                          item.type === 'lecture' ? "bg-blue-100 text-blue-700" :
                          item.type === 'practice' ? "bg-green-100 text-green-700" :
                          "bg-purple-100 text-purple-700"
                        )}>
                          {item.type === 'lecture' ? 'Лек' : item.type === 'practice' ? 'Прак' : 'Лаб'}
                        </span>
                      </td>
                      <td className="px-4 py-3 border-b text-right font-mono font-bold text-slate-700 bg-slate-50/50">
                        {item.hoursTotal}
                      </td>
                      {item.weeklyHours.map((hours, idx) => (
                        <td key={idx} className="px-2 py-3 border-b text-center border-l border-slate-100">
                          {hours > 0 ? (
                            <span className="font-medium text-[rgb(26,77,156)]">{hours}</span>
                          ) : (
                            <span className="text-slate-300">-</span>
                          )}
                        </td>
                      ))}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
