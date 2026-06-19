export interface Institute {
  id: string;
  name: string;
}

export interface Department {
  id: string;
  instituteId: string;
  name: string;
}

export interface Teacher {
  id: string;
  departmentId: string;
  name: string;
  email: string;
  properties?: string[]; // Special properties/requirements for teacher
}

export interface Subject {
  id: string;
  name: string;
  type: 'lecture' | 'practice' | 'lab' | 'exam' | 'consultation';
  requirements?: string[]; // Property IDs required for this subject
}

export interface Group {
  id: string;
  name: string;
  course: number;
  instituteId: string;
  educationLevel?: 'bachelor' | 'master' | 'phd' | 'specialist'; // Education level
  size?: number; // Number of students in group
}

export interface Building {
  id: string;
  name: string;
  address: string;
}

export interface Room {
  id: string;
  name: string;
  capacity: number;
  type: string;
  buildingId: string;
  properties?: string[]; // Property IDs possessed by this room
}

// Data
export const institutes: Institute[] = [
  { id: 'iit', name: 'Институт информационных технологий' },
  { id: 'iet', name: 'Институт экономики и торговли' },
  { id: 'law', name: 'Юридический институт' },
];

export const departments: Department[] = [
  { id: 'dep_prog', instituteId: 'iit', name: 'Кафедра программирования' },
  { id: 'dep_is', instituteId: 'iit', name: 'Кафедра информационных систем' },
  { id: 'dep_math', instituteId: 'iit', name: 'Кафедра высшей математики' },
  { id: 'dep_econ', instituteId: 'iet', name: 'Кафедра экономики' },
  { id: 'dep_men', instituteId: 'iet', name: 'Кафедра менеджмента' },
  { id: 'dep_civil', instituteId: 'law', name: 'Кафедра гражданского права' },
];

export const teachers: Teacher[] = [
  { id: 't1', departmentId: 'dep_prog', name: 'Иванов Иван Иванович', email: 'ivanov@uni.edu', properties: ['proj'] },
  { id: 't2', departmentId: 'dep_prog', name: 'Петрова Мария Сергеевна', email: 'petrova@uni.edu', properties: ['comp'] },
  { id: 't3', departmentId: 'dep_is', name: 'Сидоров Петр Александрович', email: 'sidorov@uni.edu', properties: [] },
  { id: 't4', departmentId: 'dep_math', name: 'Козлова Анна Владимировна', email: 'kozlova@uni.edu', properties: ['proj'] },
  { id: 't5', departmentId: 'dep_econ', name: 'Смирнов Алексей Петрович', email: 'smirnov@uni.edu', properties: [] },
];

export const groups: Group[] = [
  { id: 'ivt-301', name: 'ИВТ-301', course: 3, instituteId: 'iit', educationLevel: 'bachelor', size: 28 },
  { id: 'ivt-302', name: 'ИВТ-302', course: 3, instituteId: 'iit', educationLevel: 'bachelor', size: 30 },
  { id: 'pi-201', name: 'ПИ-201', course: 2, instituteId: 'iit', educationLevel: 'bachelor', size: 25 },
  { id: 'kb-101', name: 'КБ-101', course: 1, instituteId: 'iit', educationLevel: 'bachelor', size: 32 },
  { id: 'econ-101', name: 'ЭК-101', course: 1, instituteId: 'iet', educationLevel: 'bachelor', size: 27 },
  { id: 'ivt-m101', name: 'ИВТ-М101', course: 1, instituteId: 'iit', educationLevel: 'master', size: 15 },
  { id: 'pi-asp1', name: 'ПИ-АСП1', course: 1, instituteId: 'iit', educationLevel: 'phd', size: 5 },
];

export const buildings: Building[] = [
  { id: 'main', name: 'Главный корпус', address: 'ул. Ленина, 1' },
  { id: 'tech', name: 'Технический корпус', address: 'ул. Гагарина, 5' },
];

export const rooms: Room[] = [
  { id: '305', name: '305', capacity: 30, type: 'computer_lab', buildingId: 'main', properties: ['proj', 'comp'] },
  { id: '308', name: '308', capacity: 30, type: 'computer_lab', buildingId: 'main', properties: ['comp'] },
  { id: '401', name: '401', capacity: 100, type: 'lecture_hall', buildingId: 'main', properties: ['proj', 'mic'] },
  { id: '215', name: '215', capacity: 20, type: 'lab', buildingId: 'tech', properties: [] },
  { id: 'assembly', name: 'Актовый зал', capacity: 500, type: 'hall', buildingId: 'main', properties: ['proj', 'mic', 'scene'] },
];

export const subjects: Subject[] = [
  { id: 'math_analysis', name: 'Математический анализ', type: 'lecture', requirements: ['proj'] },
  { id: 'programming', name: 'Программирование', type: 'practice', requirements: ['comp'] },
  { id: 'db', name: 'Базы данных', type: 'lecture', requirements: ['proj'] },
  { id: 'algorithms', name: 'Алгоритмы', type: 'practice' },
  { id: 'web', name: 'Веб-разработка', type: 'lab', requirements: ['comp'] },
  { id: 'security', name: 'Информационная безопасность', type: 'lecture' },
  { id: 'english', name: 'Английский язык', type: 'practice' },
  { id: 'programming_exam', name: 'Экзамен по программированию', type: 'exam' },
  { id: 'math_consultation', name: 'Консультация по математике', type: 'consultation' },
];

// Workload (нагрузка)
export interface WorkloadItem {
  id: string;
  teacherId: string;
  groupId: string;
  subjectId: string;
  subgroups: string[]; // e.g. ['Подгруппа 1', 'Подгруппа 2']
  hoursPerWeek: number;
  lessonType: 'lecture' | 'practice' | 'lab';
}

export const workloadItems: WorkloadItem[] = [
  { id: 'wl1',  teacherId: 't1', groupId: 'ivt-301', subjectId: 'programming',    subgroups: ['Подгруппа 1', 'Подгруппа 2'], hoursPerWeek: 4, lessonType: 'practice' },
  { id: 'wl2',  teacherId: 't2', groupId: 'ivt-301', subjectId: 'web',            subgroups: ['Подгруппа 1', 'Подгруппа 2'], hoursPerWeek: 2, lessonType: 'lab'      },
  { id: 'wl3',  teacherId: 't4', groupId: 'ivt-301', subjectId: 'math_analysis',  subgroups: [],                              hoursPerWeek: 4, lessonType: 'lecture'  },
  { id: 'wl4',  teacherId: 't1', groupId: 'ivt-301', subjectId: 'english',        subgroups: ['Подгруппа 1', 'Подгруппа 2'], hoursPerWeek: 2, lessonType: 'practice' },
  { id: 'wl5',  teacherId: 't2', groupId: 'ivt-302', subjectId: 'programming',    subgroups: ['Подгруппа 1', 'Подгруппа 2'], hoursPerWeek: 4, lessonType: 'practice' },
  { id: 'wl6',  teacherId: 't2', groupId: 'ivt-302', subjectId: 'english',        subgroups: ['Подгруппа 1', 'Подгруппа 2'], hoursPerWeek: 2, lessonType: 'practice' },
  { id: 'wl7',  teacherId: 't4', groupId: 'ivt-302', subjectId: 'math_analysis',  subgroups: [],                              hoursPerWeek: 4, lessonType: 'lecture'  },
  { id: 'wl8',  teacherId: 't1', groupId: 'pi-201',  subjectId: 'algorithms',     subgroups: [],                              hoursPerWeek: 2, lessonType: 'practice' },
  { id: 'wl9',  teacherId: 't3', groupId: 'pi-201',  subjectId: 'security',       subgroups: [],                              hoursPerWeek: 2, lessonType: 'lecture'  },
  { id: 'wl10', teacherId: 't3', groupId: 'kb-101',  subjectId: 'db',             subgroups: [],                              hoursPerWeek: 2, lessonType: 'lecture'  },
  { id: 'wl11', teacherId: 't5', groupId: 'econ-101',subjectId: 'math_analysis',  subgroups: [],                              hoursPerWeek: 4, lessonType: 'lecture'  },
  { id: 'wl12', teacherId: 't1', groupId: 'ivt-m101',subjectId: 'algorithms',     subgroups: [],                              hoursPerWeek: 2, lessonType: 'practice' },
];

// Helper to get derived data
export const getDepartmentsByInstitute = (instId: string) => departments.filter(d => d.instituteId === instId);
export const getTeachersByDepartment = (depId: string) => teachers.filter(t => t.departmentId === depId);
export const getGroupsByInstitute = (instId: string) => groups.filter(g => g.instituteId === instId);
export const getGroupsByCourse = (course: number) => groups.filter(g => g.course === course);
export const getRoomsByBuilding = (buildId: string) => rooms.filter(r => r.buildingId === buildId);