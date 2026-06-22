// Человекочитаемые русские подписи для enum'ов бэкенда (приходят строками вида "Bachelor").
// Единый источник: используется и в дереве «Ограничения», и в фильтрах расписания.
import type { TypeDegree, Shift, LessonType } from './types'

export const degreeLabels: Record<TypeDegree, string> = {
  Secondary: 'СПО',
  Bachelor: 'Бакалавриат',
  Specialist: 'Специалитет',
  Master: 'Магистратура',
  Postgraduate: 'Аспирантура',
  Doctoral: 'Докторантура',
}

export const shiftLabels: Record<Shift, string> = {
  First: '1-я смена',
  Second: '2-я смена',
  Evening: 'Вечерняя',
  Unspecified: 'Не указана',
}

export const lessonTypeLabels: Record<LessonType, string> = {
  Lecture: 'Лекция',
  Seminar: 'Семинар',
  Laboratory: 'Лаб. работа',
  Consultation: 'Консультация',
  Examination: 'Экзамен',
}
