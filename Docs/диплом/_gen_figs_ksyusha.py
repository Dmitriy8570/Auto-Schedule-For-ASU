# -*- coding: utf-8 -*-
"""Схемы для глав 2-3 ВКР Берловой (клиентская часть + контейнеризация)."""
from _figlib import *
from matplotlib.patches import Rectangle


def fig_component_arch():
    fig, ax = new_ax(11, 7.4, xlim=(0, 120))
    box(ax, 48, 86, 24, 9, "App.vue", fc=NAVY, ec=NAVY, tc=WHITE, bold=True, fs=11)
    box(ax, 20, 70, 26, 9, "LoginCard\n(экран входа)", fc=LBLUE, ec=NAVY, fs=9.5)
    box(ax, 74, 70, 26, 9, "MainDashboard\n(рабочий экран)", fc=LBLUE, ec=NAVY, fs=9.5)
    arrow(ax, (54, 86), (33, 79), color=DARK); arrow(ax, (66, 86), (87, 79), color=DARK)
    box(ax, 60, 54, 22, 9, "TheSidebar\n(навигация)", fc="#eaf0f8", ec=NAVY, fs=9)
    tabs = [("ScheduleTab\nРасписание", 2), ("LoadTab\nНагрузка", 30),
            ("HistoryTab\nИстория", 58), ("SettingsTab\nОграничения", 86)]
    for t, x in tabs:
        box(ax, x, 38, 26, 9, t, fc=LBLUE, ec=NAVY, fs=8.8)
        arrow(ax, (87, 70), (x + 13, 47), color=GREY, style="-", lw=1)
    arrow(ax, (87, 70), (71, 63), color=DARK)
    base = [("BaseButton", 2), ("BaseInput", 26), ("TimeGrid\n(сетка)", 50), ("ToastHost", 74), ("TheHeader/Footer", 96)]
    for t, x in base:
        box(ax, x, 22, 22, 8, t, fc=LGREEN, ec=GREEN, fs=8.5)
    label(ax, 50, 32, "переиспользуемые базовые компоненты", fs=9, color=GREEN)
    # сбоку — композаблы и api
    box(ax, 2, 70, 14, 16, "Композаблы:\nuseAuth\nuseAsync\nuseToast", fc=LORANGE, ec=ORANGE, fs=8.3)
    box(ax, 104, 70, 14, 16, "API-модули:\nhttp, auth,\nlessons, …", fc=LORANGE, ec=ORANGE, fs=8.3)
    label(ax, 60, 99, "Компонентная архитектура клиентского приложения", bold=True, color=NAVY, fs=11.5)
    save(fig, "ksy_component_arch")


def fig_data_flow():
    fig, ax = new_ax(8.5, 8.5, xlim=(0, 100))
    box(ax, 20, 84, 60, 9, "Компоненты представления (Vue SFC)", fc=LBLUE, ec=NAVY, bold=True, fs=10.5)
    box(ax, 20, 66, 60, 9, "Состояние: композаблы и хранилище Pinia", fc=LORANGE, ec=ORANGE, bold=True, fs=10.5)
    box(ax, 20, 48, 60, 9, "Модуль API-клиента (обёртка fetch)", fc=LGREEN, ec=GREEN, bold=True, fs=10.5)
    box(ax, 8, 28, 38, 10, "REST API\n(HTTP, JSON, JWT)", fc="#eef2f7", ec=GREY, fs=9.5)
    box(ax, 54, 28, 38, 10, "WebSocket / SignalR\n(реальное время)", fc="#eef2f7", ec=GREY, fs=9.5)
    box(ax, 20, 8, 60, 9, "Серверная часть (ASP.NET Core)", fc=NAVY, ec=NAVY, tc=WHITE, bold=True, fs=10.5)
    arrow(ax, (50, 84), (50, 75), color=DARK); label(ax, 62, 80, "реактивность", fs=8.5)
    arrow(ax, (50, 66), (50, 57), color=DARK)
    arrow(ax, (40, 48), (27, 38), color=DARK); arrow(ax, (60, 48), (73, 38), color=DARK)
    arrow(ax, (27, 28), (40, 17), color=DARK)
    arrow(ax, (73, 28), (60, 17), color=DARK)
    arrow(ax, (66, 12), (66, 62), color=ORANGE, ls="--", rad=-0.3)
    label(ax, 84, 40, "push-\nсобытия", fs=8.3, color=ORANGE)
    label(ax, 50, 97, "Поток данных в клиентском приложении", bold=True, color=NAVY, fs=11.5)
    save(fig, "ksy_data_flow")


def fig_realtime():
    fig, ax = new_ax(11, 5.0, xlim=(0, 110))
    actors = [("Серверная часть", 14), ("SignalR-хаб", 40), ("Клиент: обработчик", 68), ("Интерфейс (UI)", 95)]
    for name, x in actors:
        title_box(ax, x - 12, 74, 24, 8, name, fc=NAVY, fs=8.8)
        ax.plot([x, x], [6, 74], color=GREY, lw=1, ls=(0, (4, 3)), zorder=0)

    def msg(x1, x2, y, t, color=DARK):
        arrow(ax, (x1, y), (x2, y), color=color, style="-|>", lw=1.4)
        label(ax, (x1 + x2) / 2, y + 2, t, fs=8.2, color=color)
    box(ax, 2, 58, 24, 9, "публикация расписания /\nсинхронизация нагрузки", fc=LGREEN, ec=GREEN, fs=8)
    msg(14, 40, 52, "событие изменения")
    msg(40, 68, 44, "ScheduleChanged / WorkloadChanged", color=ORANGE)
    box(ax, 56, 28, 24, 9, "обновление\nхранилища (Pinia)", fc=LORANGE, ec=ORANGE, fs=8.3)
    msg(68, 95, 22, "реактивное обновление")
    box(ax, 84, 8, 22, 9, "перерисовка\nсетки без\nперезагрузки", fc=LBLUE, ec=NAVY, fs=8.3)
    label(ax, 55, 86, "Обновление интерфейса в режиме реального времени", bold=True, color=NAVY, fs=11)
    save(fig, "ksy_realtime")


def fig_schedule_mock():
    fig, ax = new_ax(11, 7, xlim=(0, 120), ylim=(0, 80))
    box(ax, 1, 1, 118, 78, "", fc="#f8fafc", ec="#cbd5e1", rounded=False, lw=1.2)
    # верхняя панель
    box(ax, 3, 70, 114, 7, "", fc=WHITE, ec="#e2e8f0", rounded=False)
    for i, t in enumerate(["Институт ▾", "Группа ▾", "Семестр ▾", "Неделя ▾"]):
        box(ax, 5 + i * 17, 71, 15, 5, t, fc=LGREY, ec=GREY, fs=8)
    box(ax, 80, 71, 16, 5, "Генерация", fc=NAVY, ec=NAVY, tc=WHITE, fs=8.5, bold=True)
    box(ax, 98, 71, 16, 5, "Сохранить", fc=LGREEN, ec=GREEN, fs=8.5, bold=True)
    # шапка дней
    days = ["", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"]
    cw = 16; x0 = 4; y0 = 62
    for j, d in enumerate(days):
        box(ax, x0 + j * cw, y0, cw, 5, d, fc="#e2e8f0", ec="#cbd5e1", fs=8.5, bold=True)
    times = ["1\n8:00", "2\n9:40", "3\n11:20", "4\n13:20", "5\n15:00"]
    for i, tm in enumerate(times):
        yy = y0 - 11 - i * 11
        box(ax, x0, yy, cw, 11, tm, fc="#eef2f7", ec="#cbd5e1", fs=7.5)
        for j in range(1, 7):
            box(ax, x0 + j * cw, yy, cw, 11, "", fc=WHITE, ec="#eef2f7", lw=0.8)
    # карточки занятий
    def card(col, row, text, fc, ec):
        yy = y0 - 11 - row * 11
        box(ax, x0 + col * cw + 0.6, yy + 0.8, cw - 1.2, 9.4, text, fc=fc, ec=ec, fs=6.6)
    card(1, 0, "Матанализ\nЛекция\nауд. 412", "#dbeafe", "#3b82f6")
    card(2, 1, "Физика\nПрактика\nауд. 305", "#dcfce7", "#22c55e")
    card(3, 0, "Программир.\nЛаб.\nауд. 201", "#fef9c3", "#eab308")
    card(5, 2, "История\nЛекция\nауд. 110", "#dbeafe", "#3b82f6")
    # конфликт
    card(1, 3, "КОНФЛИКТ\nаудитория\nзанята", "#fee2e2", "#ef4444")
    label(ax, 60, 77.5, "Экран расписания: интерактивная сетка с индикацией конфликта (макет)",
          bold=True, color=NAVY, fs=10)
    save(fig, "ksy_schedule_mock")


def fig_docker_build():
    fig, ax = new_ax(11, 4.6, xlim=(0, 120))
    box(ax, 4, 30, 50, 36, "", fc="#eef4fb", ec=NAVY, rounded=True, lw=1.4)
    label(ax, 29, 62, "Этап 1 — сборка (node:22-slim)", bold=True, color=NAVY, fs=10)
    box(ax, 8, 48, 18, 8, "npm ci", fc=LBLUE, ec=NAVY, fs=9)
    box(ax, 30, 48, 20, 8, "npm run build", fc=LBLUE, ec=NAVY, fs=9)
    box(ax, 16, 34, 26, 8, "статические файлы /app/dist", fc=LGREEN, ec=GREEN, fs=8.5)
    arrow(ax, (26, 52), (30, 52)); arrow(ax, (40, 48), (32, 42))
    box(ax, 66, 30, 50, 36, "", fc="#f3f6fb", ec=GREY, rounded=True, lw=1.4)
    label(ax, 91, 62, "Этап 2 — выполнение (nginx:alpine)", bold=True, color=DARK, fs=10)
    box(ax, 70, 46, 20, 9, "dist → /usr/share/\nnginx/html", fc=LGREEN, ec=GREEN, fs=8.2)
    box(ax, 94, 46, 18, 9, "nginx.conf\n(SPA + proxy)", fc="#eef2f7", ec=GREY, fs=8.2)
    box(ax, 78, 33, 26, 8, "итоговый образ frontend", fc=NAVY, ec=NAVY, tc=WHITE, fs=8.6, bold=True)
    arrow(ax, (42, 38), (70, 50), color=DARK)
    label(ax, 56, 47, "копирование\nсборки", fs=8, color=DARK)
    label(ax, 60, 72, "Многоэтапная сборка образа клиентского приложения", bold=True, color=NAVY, fs=11)
    save(fig, "ksy_docker_build")


def fig_compose():
    fig, ax = new_ax(11, 6, xlim=(0, 120))
    box(ax, 4, 44, 20, 10, "Браузер\nпользователя", fc=LGREY, ec=GREY, fs=9.5, bold=True)
    box(ax, 4, 2, 112, 56, "", fc="none", ec="#cbd5e1", rounded=True, lw=1.3)
    label(ax, 95, 54, "сеть Docker (app-network)", fs=8.5, color=GREY)
    box(ax, 30, 44, 22, 10, "frontend\nnginx :80", fc=LBLUE, ec=NAVY, fs=9.5, bold=True)
    box(ax, 60, 44, 22, 10, "backend\nASP.NET :8080", fc=LBLUE, ec=NAVY, fs=9.5, bold=True)
    box(ax, 90, 52, 24, 8, "db — PostgreSQL", fc=LGREEN, ec=GREEN, fs=9)
    box(ax, 90, 40, 24, 8, "mmis — MS SQL", fc=LORANGE, ec=ORANGE, fs=9)
    box(ax, 90, 28, 24, 8, "ad-dc — Samba AD", fc="#eef2f7", ec=GREY, fs=9)
    arrow(ax, (24, 49), (30, 49))
    arrow(ax, (52, 49), (60, 49)); label(ax, 56, 51.5, "/api,\n/hubs", fs=7.5)
    arrow(ax, (82, 49), (90, 55)); arrow(ax, (82, 48), (90, 44)); arrow(ax, (82, 46), (90, 32))
    label(ax, 60, 22, "контейнеры запускаются и связываются одной командой docker compose up",
          fs=9, color=GREY)
    label(ax, 60, 64, "Топология контейнеризованного развёртывания", bold=True, color=NAVY, fs=11.5)
    save(fig, "ksy_compose")


def fig_project_structure():
    fig, ax = new_ax(7.5, 7, xlim=(0, 60), ylim=(0, 70))
    items = [
        (2, "Frontend/", True), (6, "├─ src/", True),
        (10, "│   ├─ api/  (http, auth, lessons, …)", False),
        (10, "│   ├─ components/  (.vue)", False),
        (10, "│   ├─ composables/  (useAuth, …)", False),
        (10, "│   ├─ App.vue", False),
        (10, "│   └─ main.ts", False),
        (6, "├─ Dockerfile", False),
        (6, "├─ nginx.conf", False),
        (6, "├─ vite.config.ts", False),
        (6, "└─ package.json", False),
    ]
    y = 60
    for indent, text, bold in items:
        ax.text(2 + indent, y, text, fontsize=11 if bold else 10.5,
                family="DejaVu Sans Mono", color=NAVY if bold else DARK,
                fontweight="bold" if bold else "normal", va="center")
        y -= 5.2
    label(ax, 30, 67, "Структура проекта клиентского приложения", bold=True, color=NAVY, fs=11)
    save(fig, "ksy_project_structure")


for f in (fig_component_arch, fig_data_flow, fig_realtime, fig_schedule_mock,
          fig_docker_build, fig_compose, fig_project_structure):
    f()
print("ksyusha figures generated")
