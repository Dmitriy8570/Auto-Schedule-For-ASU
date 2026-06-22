# Контекст сессии (для продолжения работы)

> Рабочая заметка-«снимок» состояния на 2026-06-22. Сюда сведено всё, что нужно, чтобы
> продолжить без повторного изучения проекта. Задача пользователя (Дмитрия): «в подробностях
> изучи проект, доделай недостающие моменты, перечитай диплом и диплом ксюши, если что
> отредактируй, напиши 2 и 3 главу обоих ВКР, если нужны скриншоты — попроси».

---

## 1. Кто есть кто и где что лежит

- **Бардаков Дмитрий Александрович**, гр. 4.205-2, ИМИТ АлтГУ — автор, делает **бэкенд** (домен,
  БД, алгоритм OR-Tools, API), **кроме Docker**. Его ВКР: `Docs/диплом/Диплом.docx`.
- **Берлова Ксения Андреевна** («Ксюша»), гр. 4.205-2 — товарищ по команде, делает **фронтенд
  (Vue/TS) + Docker**. Её ВКР: `Docs/диплом/диплом_ксюша.docx` (она прислала файл, он в папке).
- Тема у обоих одинаковая: «Разработка веб-приложения для автоматизированного формирования
  учебного расписания университета». Науч. рук. Михеева Т. В., председатель ГЭК Леонов С. Л.
- Общение с пользователем — **на русском**.

**Ключевые директории:**
- `backend-auto-schedule/` — .NET 10 решение (нет .sln; проекты `src/{API,Application,Domain,
  Infrastructure}`, тесты `tests/Schedule.DevSolver.Tests`).
- `Frontend/` — Vue 3 + TS + Vite (+ Dockerfile, nginx.conf).
- `docker-compose.yml` — 5 сервисов: frontend(nginx), backend(.NET), db(PostgreSQL16),
  mmis-db(MS SQL — имитация ММИС), ad-dc(Samba AD).
- `Docs/диплом/` — обе ВКР, образцы `vkr*.pdf`, требования вуза, исходники, ER `Docs/Diagram_db.drawio`.

---

## 2. Что сделано в ЭТОЙ сессии

### 2.1 Код («доделай до готового проекта») — бэкенд, всё собирается, **44 теста зелёные**
1. **Транзакции SERIALIZABLE** (защита от коллизий — центральное заявление ВКР Дмитрия):
   - Новое: `src/Application/Common/Interfaces/ITransactionRunner.cs`,
     `src/Infrastructure/Data/TransactionRunner.cs` (execution strategy + `IsolationLevel.Serializable`
     + автоповтор при сериализационном конфликте; `ChangeTracker.Clear()` перед попыткой).
   - Зарегистрирован в `src/Infrastructure/DependencyInjection.cs` (Scoped).
   - Обёрнуты хендлеры: `CreateLessonCommand` (фикс TOCTOU: проверка коллизий + вставка атомарно),
     `DeleteLessonCommand`, `PublishInstituteScheduleCommand`, `DiscardInstituteScheduleCommand`,
     `GenerateInstituteScheduleCommand`, `GenerateScheduleCommand` (солвер — ВНЕ транзакции, в
     транзакции только короткая запись результата).
2. **Real-time SignalR** (обе ВКР заявляют real-time):
   - Новое: `src/API/Hubs/ScheduleHub.cs` (`[Authorize]`, `/hubs/schedule`),
     `src/API/Hubs/SignalRRealtimeNotifier.cs`, `src/Application/Common/Interfaces/IRealtimeNotifier.cs`.
   - `Program.cs`: `AddSignalR()`, регистрация нотифаера, `MapHub`, JWT для WebSocket через
     query-параметр `access_token` (OnMessageReceived).
   - Рассылка: `PublishInstituteScheduleCommand` → `ScheduleChanged` (после коммита);
     `MmisSyncHostedService` → `WorkloadChanged` (если были изменения нагрузки).
   - `Frontend/nginx.conf`: добавлен `location /hubs/` с upgrade-заголовками WebSocket.
3. Тесты-фейки для новых зависимостей: `tests/.../Fakes/FakeTransactionRunner.cs`,
   `FakeRealtimeNotifier.cs`; поправлены `GenerateInstituteScheduleOptionsTests`,
   `PublishDiscardScheduleTests` (добавлены параметры в конструкторы хендлеров).

**Команды проверки** (из `backend-auto-schedule/`):
- сборка: `dotnet build src/API/API.csproj` и `dotnet build tests/Schedule.DevSolver.Tests/...csproj`
  (две отдельные команды — `dotnet build A B` НЕ работает: «можно указать только один проект»).
- тесты: `cd tests/Schedule.DevSolver.Tests && dotnet test` (~3.5 мин, прогоняет CP-SAT).

### 2.2 Диплом — обе работы доведены до полного состава
В обе ВКР добавлены: **главы 2 и 3, заключение, реферат (после титула), библиография, заполнено
введение** (число глав/страниц/источников + описания глав). Оформление по нормоконтролю (Times
New Roman 14, интервал 1.5, отступ 1.25 см; подписи таблиц 12 пт справа НАД таблицей, рисунков
12 пт по центру ПОД; заголовки глав ПРОПИСНЫЕ по центру с новой страницы).

**Диплом.docx (Бардаков): итог — 330 абз., 7 таблиц, 11 рисунков, библиография 33, реферат ≈70 с.**
- Гл. 2 «ПРОЕКТИРОВАНИЕ СЕРВЕРНОЙ ЧАСТИ СИСТЕМЫ» (2.1–2.12): требования; стек (табл.); чистая
  архитектура+CQRS (рис.); БД+ER (рис.+табл.); **мат. модель, формулы 2.1–2.11**; модуль солвера
  (паттерн «Строитель», прунинг, декомпозиция B+C, асинхронная генерация — 2 рис.);
  версионирование (рис.); SERIALIZABLE; журнал нагрузки; ММИС-ETL (рис.); REST API (табл.);
  LDAP+JWT (рис.) + SignalR.
- Гл. 3 «РЕАЛИЗАЦИЯ И ТЕСТИРОВАНИЕ»: структура решения (рис.); листинги (Lesson, TransactionRunner,
  конвейер билдеров); реализация солвера/асинхронности/ММИС/auth/API; тестирование (табл. 3.1,
  44 теста); соответствие требованиям (табл. 3.2).
- **Глава 1 СОГЛАСОВАНА С КОДОМ**: смена обучения, переходы между корпусами и вместимость
  перенесены из мягких в **жёсткие** (в коде они жёсткие). Жёстких стало 7, мягких 5.

**диплом_ксюша.docx (Берлова): итог — 260 абз., 5 таблиц, 10 рисунков, библиография 30, реферат ≈66 с.**
- Гл. 2 «ПРОЕКТИРОВАНИЕ КЛИЕНТСКОЙ ЧАСТИ И ЕЁ РАЗВЁРТЫВАНИЯ» (2.1–2.10): компонентная
  архитектура SPA (рис.); поток данных + Pinia (рис.); REST-клиент; real-time (рис.); сценарии +
  Vue Router (табл. экранов); интерактивная сетка расписания + drag&drop + конфликты (рис.-макет);
  auth/сессия; дизайн-система; контейнеризация (рис. multi-stage); docker compose (табл.+рис.).
- Гл. 3: структура проекта (рис.); листинги (http.ts, session, useAsync, Dockerfile); реализация
  визуализации/уведомлений/контейнеризации; тестирование; соответствие (табл.).

---

## 3. Инструменты/конвенции работы с диплома­ми (важно для продолжения)

- **Python 3.14** (`/c/Python314/python`), установлены `python-docx`, `matplotlib`, `Pillow`.
- **Правка docx — только python-docx.** Хелперы (ОСТАВЛЕНЫ в `Docs/диплом/`):
  - `_docxlib.py` — функции `body/bullet/chapter/heading2/figure/formula/table_caption/table/
    code/listing_caption` (точно повторяют формат существующих абзацев).
  - `_figlib.py` — примитивы рисования (matplotlib, кириллица через DejaVu Sans).
  - `_gen_figs_dmitriy.py`, `_gen_figs_ksyusha.py` — генерация рисунков в `Docs/диплом/figures/`
    (идемпотентны — можно перезапускать; правь и регенерируй фигуры здесь).
- **ВНИМАНИЕ:** скрипты, которые ДОПИСЫВАЛИ контент в docx (`_write_*.py`, `_reconcile_ch1.py`,
  `_write_*_front/back.py`), УДАЛЕНЫ намеренно — повторный запуск ДУБЛИРОВАЛ бы текст. Для новых
  правок писать новый одноразовый скрипт `_*.py`, по образцу хелперов, и удалять после.
- **Консоль Windows = cp1251**: кириллица в stdout — кракозябры (это норма). Результаты писать в
  UTF-8-файл и читать через инструмент Read. В сам docx всё пишется в UTF-8 корректно (формулы
  ∑ ∈ ∀ ≤ → · и т. п. отображаются нормально).
- **Бэкапы docx до правок** — `Docs/диплом/_backup/` (можно удалить, когда всё устроит).
- **Правило написания диплома (из CLAUDE.md):** пишем так, как будто всё реализовано (автор
  дорабатывает код параллельно). Поэтому в ВКР Берловой router/Pinia/клиент SignalR/drag&drop
  описаны как готовые, хотя в коде фронта их пока нет.

---

## 4. Ключевые факты о кодовой базе (чтобы не перечитывать)

**Домен** (`src/Domain`): Semester/Week/WeekDay/TimeSlot; Lesson(Version: Draft/Current);
AcademicStream/StreamGroups/Subject; Building/Classroom; Institute/Degree/Course/Group(Shift,
ParentGroupId); Department/Teacher; Curriculum(Parallelism, Double, FavoriteBuildingId, LessonType,
NeededEquipments); SemesterWorkload/WeekWorkload(+ ChangeHours→журнал); SemesterLog/WeekLog
(Add/Update/Delete, FK обнуляется при удалении); ClassroomAvailability/TeacherAvailability
(знаковый Penalty: Required −100, Preferred −10, Neutral 0, Discouraged +10, Prohibited +100);
ConstraintConfig(TeacherGap/StudentGap/…). Инкапсуляция: private set + фабрики Create + Guard.

**Солвер** (`src/Application/Solver`): `ScheduleModel.Lessons` = `BoolVar?[w,r,t]` (null = отсечено
прунингом по StaticFeasibility: вместимость/оборудование/смена). `ScheduleModelDirector`
(`CreateDefault`/`CreatePerInstitute`) применяет `IModelSectionBuilder` по порядку.
- **Жёсткие (7):** TotalHours (Σ = Hours/2), Intersection (аудитория/преподаватель/группа),
  Equipment, Capacity, Shift (1 смена: пары 1–4, 2-я: 5–6, вечер: 7+), BuildingTravel (нет
  соседних пар в разных корпусах), DoubleLesson (ровно 2 подряд). +OccupiedResources (для
  декомпозиции по институту).
- **Мягкие (5):** Window («окна»), Availability (знаковый штраф), DailyLessonsLimit (мягкий,
  группа 4 / преп. 7, штраф 50), FavoriteBuilding (5), Parallelism (2). +PreviousScheduleAnchor
  (якорь к прошлому семестру). Веса — `SolverPenaltyWeights` + БД (ConstraintConfig).
- `ScheduleSolver` (CP-SAT, `SolverOptions`: 180 c, 8 воркеров). Статусы Optimal/Feasible/
  Infeasible/Unknown. На ~530 нагрузках за 180 с → Unknown (тюнинг солвера — отдельная задача).

**Infrastructure:** EF Core + PostgreSQL (конфиги в `Data/Configurations`, 5 миграций);
репозитории; `ScheduleDataProvider` (декомпозиция по институту: SQL-фильтр + occupied + якоря);
ММИС (`Mmis/`: Dapper-чтение из MS SQL → upsert по детерминированному Guid + diff нагрузки →
журналы, всё в транзакции; `MmisSyncHostedService` по расписанию); сидер (`Seed/` — аудитории +
календарная сетка); асинхронная генерация (`Schedule/Generation` — Channel-очередь + hosted
service); Auth (`Auth/` — LDAP bind в AD + проверка группы + JWT).

**API** (`src/API/Controllers`): Auth(login/me), Calendar, Constraints, Facilities, Lessons
(by-group/room/teacher, CRUD, generate sync/async/status, publish/discard), Management (CRUD
аудиторий/оборудования/ограничений), University (справочники), Workloads (нагрузка+история, пагинация).
Глобальные обработчики исключений → 401/400/409 (ProblemDetails); Swagger; все эндпоинты под auth.

**Тесты** (44): ScheduleBuilderTests (жёсткие на реальном CP-SAT), SoftConstraintBuilderTests,
SemesterScheduleGenerationTests, LessonConflictTests, PublishDiscardScheduleTests,
WorkloadChangeTrackingTests, WorkloadChangesPaginationTests, InfrastructureSeederTests,
ScheduleGenerationQueueTests, GenerateInstituteScheduleOptionsTests. (xUnit, EF Core Sqlite in-memory.)

**Фронтенд** (`Frontend/src`): main.ts/App.vue (ручное переключение экранов, БЕЗ router);
components (LoginCard, MainDashboard, TheSidebar, ScheduleTab 831 стр., LoadTab, HistoryTab,
SettingsTab, TimeGrid — сетка доступности с drag-fill и 5 градациями, BaseButton/BaseInput,
ToastHost); composables (useAuth, useAsync c AbortController, useToast); api (http.ts — fetch+JWT+
разбор ProblemDetails/409, session.ts — JWT в localStorage, auth/lessons/calendar/lookups/
management/constraints/workloads/types). НЕТ в коде: vue-router, Pinia, клиент SignalR, drag&drop
ЗАНЯТИЙ (drag есть только в сетке доступности).

---

## 5. Что осталось (по приоритету)

**Оформление ВКР (быстро):**
1. Открыть оба docx в Word, посмотреть ИТОГОВОЕ число страниц, поправить его в реферате и во
   введении (сейчас оценка ≈70 у Дмитрия, ≈66 у Берловой — это единственная неточная цифра).
2. Вставить реальные скриншоты (в тексте отмечены сносками-просьбами): у Дмитрия — прогон тестов
   (44 зелёных), при желании Swagger; у Берловой — экраны UI (вход, сетка расписания, нагрузка,
   история, ограничения) + прогон тестов. Пользователь обещал прислать.
3. Перенести титул на последний лист (образец `титул_и_последний лист.docx`).

**Код (по желанию автора, фронт — это часть Берловой):**
- Фронт-полировка под текст ВКР Берловой: vue-router (вместо ручных вкладок), Pinia (стор сессии/
  справочников), клиент `@microsoft/signalr` (подписка на `/hubs/schedule`), предупреждение об
  истечении сессии, drag&drop занятий, компонентные/e2e-тесты. Бэкенд к real-time уже готов.
- Тюнинг солвера на масштабе (~530 нагрузок за 180 с → Unknown): эвристики первого решения,
  параметры поиска, более мелкая декомпозиция.
- Добор бэк-тестов (успешная генерация по институту; эндпоинты трека A).

**Актуальные источники статуса:** `TODO.md` (корень) и `Docs/диплом/Прогресс-диплома.md`
(оба обновлены этой сессией).

---

## 6. Открытые решения/нюансы
- ER в дипломе — это сгенерированная логическая схема (matplotlib), не экспорт из drawio
  (`Docs/Diagram_db.drawio` — настоящая, актуальна). Если нужно — заменить картинку.
- Антиплагиат между Дмитрием и Берловой: тексты разные (бэкенд vs фронтенд+Docker), общая только
  «Актуальность» во введении (так задумано автором).
- Реальные числа по АлтГУ (сколько групп/преподавателей/аудиторий) автор не давал — в тексте
  обтекаемо («сотни/десятки»).
