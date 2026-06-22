import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { token } from '../api/session'

import MainDashboard from '../components/MainDashboard.vue'
import LoginView from '../views/LoginView.vue'
import ScheduleTab from '../components/ScheduleTab.vue'
import LoadTab from '../components/LoadTab.vue'
import UnplacedLoadTab from '../components/UnplacedLoadTab.vue'
import GenerationHistoryTab from '../components/GenerationHistoryTab.vue'
import HistoryTab from '../components/HistoryTab.vue'
import SettingsTab from '../components/SettingsTab.vue'

// Маршруты вкладок-разделов вынесены в URL: глубокие ссылки, история браузера и состояние
// активной вкладки теперь живут в адресе. Заголовок шапки берётся из meta.title.
const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
  {
    path: '/',
    component: MainDashboard,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/schedule' },
      { path: 'schedule', name: 'schedule', component: ScheduleTab, meta: { title: 'Расписание' } },
      { path: 'load', name: 'load', component: LoadTab, meta: { title: 'Нагрузка' } },
      { path: 'unplaced', name: 'unplaced', component: UnplacedLoadTab, meta: { title: 'Нераспределённая нагрузка' } },
      { path: 'generation', name: 'generation', component: GenerationHistoryTab, meta: { title: 'История автогенерации' } },
      { path: 'history', name: 'history', component: HistoryTab, meta: { title: 'История изменений' } },
      { path: 'settings', name: 'settings', component: SettingsTab, meta: { title: 'Ограничения' } },
    ],
  },
  { path: '/:pathMatch(.*)*', redirect: '/schedule' },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

// Гвард доступа: защищённые маршруты требуют сессии; на /login авторизованного не пускаем.
router.beforeEach((to) => {
  const authenticated = token.value !== null
  if (to.meta.requiresAuth && !authenticated) return { name: 'login' }
  if (to.meta.public && authenticated) return { name: 'schedule' }
  return true
})
