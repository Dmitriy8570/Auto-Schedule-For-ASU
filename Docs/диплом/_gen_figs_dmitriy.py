# -*- coding: utf-8 -*-
"""Генерация схем для глав 2-3 ВКР Бардакова (серверная часть)."""
from _figlib import *


def fig_architecture():
    fig, ax = new_ax(9.5, 9)
    title_box(ax, 20, 88, 60, 9, "Слой представления — API", fc=NAVY)
    box(ax, 14, 86.5, 72, 0, "", fc="none", ec="none")
    ax.text(50, 84.6, "Контроллеры REST, SignalR-хаб реального времени, обработчики исключений, Swagger",
            ha="center", va="center", fontsize=9.5, color=GREY)

    title_box(ax, 20, 64, 60, 9, "Слой приложения — Application", fc=BLUE)
    ax.text(50, 60.6, "CQRS: команды и запросы (MediatR), валидация (FluentValidation),\nмодуль составления расписания (OR-Tools CP-SAT), контракты-интерфейсы",
            ha="center", va="center", fontsize=9.5, color=GREY)

    title_box(ax, 20, 40, 60, 9, "Слой предметной области — Domain", fc=GREEN)
    ax.text(50, 36.6, "Сущности и агрегаты, перечисления, инварианты (Guard), фабричные методы",
            ha="center", va="center", fontsize=9.5, color=GREY)

    title_box(ax, 20, 14, 60, 9, "Слой инфраструктуры — Infrastructure", fc="#5b6b86")
    ax.text(50, 10.6, "EF Core + PostgreSQL, интеграция с ММИС (MS SQL), LDAP/JWT,\nфоновые службы (генерация, синхронизация, сидер), SignalR-уведомления",
            ha="center", va="center", fontsize=9.5, color=GREY)

    arrow(ax, (50, 88), (50, 73), color=DARK)
    arrow(ax, (50, 64), (50, 49), color=DARK)
    arrow(ax, (84, 18.5), (84, 44), color=RED, ls="--")
    arrow(ax, (84, 44), (80, 44.5), color=RED, ls="--")
    arrow(ax, (16, 18.5), (16, 68.5), color=RED, ls="--")
    label(ax, 53.5, 80, "зависит от", color=DARK, rot=90, fs=9)
    label(ax, 53.5, 56, "зависит от", color=DARK, rot=90, fs=9)
    label(ax, 90, 33, "реализует\nинтерфейсы\n(инверсия\nзависимостей)", color=RED, rot=90, fs=8.2)
    label(ax, 10.5, 44, "реализует\nинтерфейсы", color=RED, rot=90, fs=8.2)
    label(ax, 50, 97.5, "Направление зависимостей — к доменному ядру", color=DARK, bold=True, fs=11)
    save(fig, "arch_layers")


def fig_er():
    fig, ax = new_ax(13, 14, ylim=(0, 140))
    def ent(x, y, name, fc=LBLUE, ec=NAVY, w=22, h=5.2):
        return box(ax, x, y, w, h, name, fc=fc, ec=ec, fs=9.2, bold=True)
    # Зоны (фон под линиями связей)
    ax.add_patch(Rectangle((1, 1), 31, 138, fc="#f7f9fc", ec="#cfd8e6", lw=1, zorder=0))
    ax.add_patch(Rectangle((34, 1), 31, 138, fc="#f6fbf8", ec="#cfe6da", lw=1, zorder=0))
    ax.add_patch(Rectangle((67, 1), 32, 138, fc="#fbf7f1", ec="#e6dcc6", lw=1, zorder=0))
    label(ax, 16.5, 135, "Структура университета", bold=True, color=NAVY, fs=11)
    label(ax, 49.5, 135, "Нагрузка и календарь", bold=True, color=GREEN, fs=11)
    label(ax, 83, 135, "Расписание, фонд, ограничения", bold=True, color=ORANGE, fs=11)

    # Левая зона
    E = {}
    E["Institute"]  = ent(5, 126, "Institute (институт)")
    E["Department"] = ent(5, 116, "Department (кафедра)")
    E["Teacher"]    = ent(5, 106, "Teacher (преподаватель)")
    E["Degree"]     = ent(5, 92, "Degree (ступень)")
    E["Course"]     = ent(5, 82, "Course (курс)")
    E["Group"]      = ent(5, 72, "Group (группа)")
    # Центр
    E["Subject"]    = ent(38, 126, "Subject (дисциплина)", fc=LGREEN, ec=GREEN)
    E["Curriculum"] = ent(38, 112, "Curriculum (учебный план)", fc=LGREEN, ec=GREEN)
    E["SemWl"]      = ent(38, 96, "SemesterWorkload", fc=LGREEN, ec=GREEN)
    E["WeekWl"]     = ent(38, 86, "WeekWorkload", fc=LGREEN, ec=GREEN)
    E["SemLog"]     = ent(38, 72, "SemesterLog", fc="#eef6f1", ec=GREEN)
    E["WeekLog"]    = ent(38, 62, "WeekLog", fc="#eef6f1", ec=GREEN)
    E["Semester"]   = ent(38, 44, "Semester (семестр)", fc=LGREEN, ec=GREEN)
    E["Week"]       = ent(38, 34, "Week (неделя)", fc=LGREEN, ec=GREEN)
    E["WeekDay"]    = ent(38, 24, "WeekDay (день)", fc=LGREEN, ec=GREEN)
    # Правая зона
    E["Stream"]     = ent(70, 126, "AcademicStream (поток)", fc=LORANGE, ec=ORANGE, w=27)
    E["StreamGr"]   = ent(70, 116, "StreamGroups", fc="#f6efe2", ec=ORANGE, w=27)
    E["Lesson"]     = ent(70, 102, "Lesson (занятие)", fc=LORANGE, ec=ORANGE, w=27)
    E["TimeSlot"]   = ent(70, 88, "TimeSlot (слот)", fc=LORANGE, ec=ORANGE, w=27)
    E["Building"]   = ent(70, 70, "Building (корпус)", fc=LORANGE, ec=ORANGE, w=27)
    E["Classroom"]  = ent(70, 60, "Classroom (аудитория)", fc=LORANGE, ec=ORANGE, w=27)
    E["Equipment"]  = ent(70, 46, "Equipment (оборудование)", fc=LORANGE, ec=ORANGE, w=27)
    E["EqRoom"]     = ent(70, 36, "EquipmentRoom", fc="#f6efe2", ec=ORANGE, w=27)
    E["NeedEq"]     = ent(70, 26, "NeededEquipment", fc="#f6efe2", ec=ORANGE, w=27)
    E["ClAv"]       = ent(70, 14, "ClassroomAvailability", fc="#f6efe2", ec=ORANGE, w=27)
    E["TAv"]        = ent(38, 106, "TeacherAvailability", fc="#eef6f1", ec=GREEN)
    E["CC"]         = ent(70, 6, "ConstraintConfig", fc="#f6efe2", ec=ORANGE, w=27)

    def link(a, b, rad=0.0, color=GREY):
        p1 = (E[a][0], E[a][1])
        p2 = (E[b][0], E[b][1])
        arrow(ax, p1, p2, color=color, style="-", lw=1.1, rad=rad)
    # связи внутри левой
    link("Institute", "Department"); link("Department", "Teacher")
    link("Institute", "Degree"); link("Degree", "Course"); link("Course", "Group")
    # центр
    link("Subject", "Curriculum"); link("Teacher", "Curriculum", rad=-0.15, color="#9aa7bd")
    link("Curriculum", "SemWl"); link("SemWl", "WeekWl")
    link("SemWl", "SemLog"); link("WeekWl", "WeekLog")
    link("Semester", "Week"); link("Week", "WeekDay"); link("Semester", "SemWl", rad=0.2)
    link("Week", "WeekWl", rad=0.2)
    link("Teacher", "TAv", color="#9aa7bd")
    # право
    link("Group", "StreamGr", rad=-0.2, color="#9aa7bd"); link("Stream", "StreamGr")
    link("Stream", "Curriculum", rad=0.25, color="#9aa7bd")
    link("Stream", "Lesson"); link("Classroom", "Lesson", rad=-0.1); link("TimeSlot", "Lesson")
    link("WeekDay", "TimeSlot", rad=0.0, color="#9aa7bd")
    link("Building", "Classroom"); link("Classroom", "ClAv")
    link("Classroom", "EqRoom"); link("Equipment", "EqRoom"); link("Equipment", "NeedEq")
    link("Curriculum", "NeedEq", rad=0.3, color="#9aa7bd")
    link("Curriculum", "Lesson", rad=0.0, color="#9aa7bd")
    save(fig, "er_schema")


def fig_builder_pipeline():
    fig, ax = new_ax(12, 7.6, xlim=(0,120))
    title_box(ax, 2, 78, 26, 16, "ScheduleModelDirector\nBuild(ScheduleData)", fc=NAVY, fs=10)
    ax.text(15, 70, "применяет секции\nпо порядку", ha="center", fontsize=8.5, color=GREY)
    steps = [
        ("VariablesSection\n(переменные + прунинг)", LBLUE, NAVY),
        ("Жёсткие ограничения:\nTotalHours, Intersection,\nEquipment, Capacity, Shift,\nBuildingTravel, DoubleLesson,\nOccupiedResources", LRED, RED),
        ("Мягкие ограничения:\nDailyLimit, Window,\nAvailability, FavoriteBuilding,\nParallelism, PreviousAnchor", LGREEN, GREEN),
        ("ObjectiveSection\n(целевая функция → min)", LORANGE, ORANGE),
    ]
    x = 33
    prev = (28, 86)
    for i,(t,fc,ec) in enumerate(steps):
        h = 30 if i in (1,2) else 16
        y = 86 - h/2 - (0 if i in (0,3) else 0)
        yy = 71 if i in (1,2) else 78
        b = box(ax, x, yy- (14 if i in (1,2) else 0), 26, 30 if i in (1,2) else 16, t, fc=fc, ec=ec, fs=8.6)
        arrow(ax, prev, (x, b[1]), color=DARK)
        prev = (x+26, b[1])
        x += 30
    # интерфейс
    box(ax, 33, 8, 83, 12, "IModelSectionBuilder.Build(ScheduleModel)  —  единый контракт секции;\nновое правило добавляется отдельным классом без изменения существующих (принцип открытости/закрытости)",
        fc=LGREY, ec=GREY, fs=9)
    for xx in (46, 76, 106):
        arrow(ax, (xx, 57 if xx!=106 else 78), (xx, 20), color=GREY, ls=":", style="-")
    label(ax, 60, 2.5, "Расширяемая архитектура системы ограничений (паттерн «Строитель»)", bold=True, color=NAVY, fs=10.5)
    save(fig, "builder_pipeline")


def fig_mmis_etl():
    fig, ax = new_ax(12, 6.6, xlim=(0,120))
    box(ax, 2, 60, 26, 16, "ММИС: «Деканат» и\n«Нагрузка ВУЗа»\n(MS SQL Server)", fc=LGREY, ec=GREY, fs=9.5, bold=True)
    box(ax, 2, 20, 26, 16, "БД Auto-Schedule\n(PostgreSQL)", fc=LGREEN, ec=GREEN, fs=10, bold=True)
    box(ax, 36, 60, 22, 16, "MmisReader\n(Dapper, SELECT)", fc=LBLUE, ec=NAVY, fs=9.5)
    box(ax, 66, 56, 26, 24, "MmisSyncService\n• upsert справочников\n   по детерминир. Guid\n• diff нагрузки\n• SERIALIZABLE-транзакция", fc=LBLUE, ec=NAVY, fs=9)
    box(ax, 98, 60, 20, 16, "SemesterLog /\nWeekLog\n(журнал изменений)", fc=LORANGE, ec=ORANGE, fs=9)
    box(ax, 66, 20, 26, 16, "InfrastructureSeeder\nрабочие дни и пары\n(ось «время»)", fc=LBLUE, ec=NAVY, fs=9)
    box(ax, 36, 20, 22, 16, "MmisSyncHostedService\nежедневно/по интервалу", fc="#eaf0f8", ec=NAVY, fs=8.8)
    arrow(ax, (28, 68), (36, 68))
    arrow(ax, (58, 68), (66, 68))
    arrow(ax, (92, 68), (98, 68))
    arrow(ax, (79, 56), (79, 36), color=GREEN)
    arrow(ax, (66, 30), (58, 30))
    arrow(ax, (47, 36), (47, 60), color=GREY, ls="--")
    label(ax, 50, 71, "снимок", fs=8.5)
    label(ax, 95, 50, "изменения\nчасов", fs=8.2, color=ORANGE)
    label(ax, 83, 45, "upsert/diff", fs=8.5, color=GREEN, rot=90)
    arrow(ax, (66, 24), (40, 24), color=GREY, style="-|>")
    arrow(ax, (28, 28), (36, 24), color=GREY, ls="--", style="-|>")
    label(ax, 60, 88, "Конвейер синхронизации справочников и учебной нагрузки с ММИС", bold=True, color=NAVY, fs=11)
    save(fig, "mmis_etl")


def fig_gen_sequence():
    fig, ax = new_ax(11, 8.2, xlim=(0,110))
    actors = [("Клиент (SPA)",10), ("API-контроллер",33), ("Очередь задач",56), ("Фоновая служба",79), ("Солвер CP-SAT",98)]
    for name,x in actors:
        title_box(ax, x-9, 90, 18, 7, name, fc=NAVY, fs=9)
        ax.plot([x,x],[6,90], color=GREY, lw=1, ls=(0,(4,3)), zorder=0)
    def msg(x1,x2,y,t,color=DARK,ret=False):
        arrow(ax,(x1,y),(x2,y),color=color,style="-|>" , lw=1.4, ls="--" if ret else "-")
        label(ax,(x1+x2)/2,y+1.6,t,fs=8.3,color=color)
    msg(10,33,84,"POST …/institute/{id}/async")
    msg(33,56,79,"Enqueue(semester, institute)")
    msg(33,10,74,"202 Accepted, jobId",ret=True,color=GREEN)
    msg(56,79,68,"Dequeue jobId")
    box(ax,72,52,16,12,"DataProvider:\nнагрузка, фонд,\nзанятые ресурсы,\nякорь",fc=LBLUE,ec=NAVY,fs=8)
    msg(79,98,46,"Solve(model), ≤180 c")
    box(ax,90,30,17,10,"распростр.\nограничений +\nпоиск с возвратом",fc=LRED,ec=RED,fs=8)
    msg(98,79,26,"назначения / статус",ret=True,color=GREEN)
    box(ax,71,14,18,9,"SERIALIZABLE:\nсохранить Draft",fc=LGREEN,ec=GREEN,fs=8.4)
    msg(10,33,9,"GET …/status/{jobId}")
    msg(33,10,5,"статус: Succeeded + метрики",ret=True,color=GREEN)
    label(ax,55,98.5,"Асинхронная генерация расписания института",bold=True,color=NAVY,fs=11)
    save(fig,"gen_sequence")


def fig_auth_sequence():
    fig, ax = new_ax(10.5, 5.6, xlim=(0,110))
    actors=[("Клиент",12),("API: AuthController",40),("LdapAuthenticationService",68),("Active Directory",95)]
    for name,x in actors:
        title_box(ax,x-12,76,24,8,name,fc=NAVY,fs=8.6)
        ax.plot([x,x],[6,76],color=GREY,lw=1,ls=(0,(4,3)),zorder=0)
    def msg(x1,x2,y,t,color=DARK,ret=False):
        arrow(ax,(x1,y),(x2,y),color=color,style="-|>",lw=1.4,ls="--" if ret else "-")
        label(ax,(x1+x2)/2,y+1.8,t,fs=8.2,color=color)
    msg(12,40,68,"POST /api/auth/login {логин, пароль}")
    msg(40,68,60,"AuthenticateAsync")
    msg(68,95,52,"LDAP bind по UPN + чтение memberOf")
    msg(95,68,44,"профиль и группы",ret=True,color=GREEN)
    box(ax,55,30,26,9,"проверка членства\nв группе доступа",fc=LRED,ec=RED,fs=8.3)
    msg(68,40,24,"AuthenticatedUser")
    box(ax,28,14,24,8,"JwtTokenService:\nвыпуск HS256-JWT",fc=LGREEN,ec=GREEN,fs=8.3)
    msg(40,12,8,"200 OK: JWT + роли + срок",ret=True,color=GREEN)
    label(ax,55,86,"Аутентификация через доменные учётные записи (LDAP) и выпуск JWT",bold=True,color=NAVY,fs=10.5)
    save(fig,"auth_sequence")


def fig_version_lifecycle():
    fig, ax = new_ax(10.5, 4.4, xlim=(0,110))
    s_new = box(ax,3,30,18,14,"Нет расписания\n(института)",fc=LGREY,ec=GREY,fs=9.5,bold=True)
    s_draft = box(ax,42,30,20,14,"Черновик\n(Draft)",fc=LORANGE,ec=ORANGE,fs=10,bold=True)
    s_cur = box(ax,84,30,20,14,"Опубликовано\n(Current)",fc=LGREEN,ec=GREEN,fs=10,bold=True)
    arrow(ax,(21,37),(42,37)); label(ax,31.5,40,"генерация",fs=8.6,color=NAVY)
    arrow(ax,(62,37),(84,37)); label(ax,73,40,"публикация",fs=8.6,color=GREEN)
    arrow(ax,(52,30),(52,14),color=RED); arrow(ax,(52,14),(21,14),color=RED)
    arrow(ax,(21,14),(12,30),color=RED)
    label(ax,33,11,"сброс черновика (удаление Draft)",fs=8.4,color=RED)
    arrow(ax,(90,44),(90,56),color=NAVY); arrow(ax,(90,56),(52,56),color=NAVY); arrow(ax,(52,56),(52,44),color=NAVY)
    label(ax,71,58.5,"перегенерация поверх опубликованного",fs=8.4,color=NAVY)
    label(ax,55,68,"Жизненный цикл версий расписания",bold=True,color=NAVY,fs=11)
    save(fig,"version_lifecycle")


def fig_solution_structure():
    fig, ax = new_ax(9.5, 6.2)
    box(ax,30,84,40,10,"Auto-Schedule (решение)",fc=NAVY,ec=NAVY,tc=WHITE,fs=11,bold=True)
    projs=[("API","контроллеры, Program,\nSignalR, обработчики",LBLUE,NAVY,6),
           ("Application","CQRS, солвер,\nинтерфейсы, валидация",LBLUE,NAVY,30),
           ("Domain","сущности, перечисления,\nинварианты",LGREEN,GREEN,54),
           ("Infrastructure","EF Core, ММИС, Auth,\nфоновые службы",LGREY,GREY,78)]
    y=58
    for name,desc,fc,ec,x in projs:
        box(ax,x-2,y,20,16,f"{name}\n{desc}",fc=fc,ec=ec,fs=8.6,bold=False)
    box(ax,30,30,40,10,"Schedule.DevSolver.Tests (xUnit)",fc=LORANGE,ec=ORANGE,fs=9.5,bold=True)
    for x in (6,30,54,78):
        arrow(ax,(50,84),(x+8,74),color=GREY,style="-",lw=1)
    for x in (6,30,54,78):
        arrow(ax,(x+8,58),(50,40),color=GREY,style="-",lw=1,ls=":")
    label(ax,50,24,"Тесты ссылаются на Domain, Application, Infrastructure",fs=9,color=GREY)
    label(ax,50,96,"Структура программного решения",bold=True,color=NAVY,fs=11)
    save(fig,"solution_structure")


for f in (fig_architecture, fig_er, fig_builder_pipeline, fig_mmis_etl,
          fig_gen_sequence, fig_auth_sequence, fig_version_lifecycle, fig_solution_structure):
    f()
print("figures generated")
