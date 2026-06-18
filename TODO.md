# TODO / Бэклог улучшений бэкенда

Список составлен по результатам ревью архитектуры и кода. Сгруппирован по приоритету;
ссылки указывают на файлы в `backend-auto-schedule/src`.

Статус: `[ ]` — не начато, `[~]` — в работе, `[x]` — сделано.

---

## 🔴 P0 — корректность модели расписания  ✅ выполнено

- [x] **Ограничение вместимости аудитории.** Добавлен `CapacitySectionBuilder` (жёсткий):
  запрет `Lessons[w,r,t]` где `Classrooms[r].Capacity < Stream.StudentsCount`. Включён в оба
  набора `ScheduleModelDirector`.

- [x] **Чётность часов в `TotalHoursSectionBuilder`.** Нечётные часы больше не теряются молча —
  генерация прерывается с явным сообщением (fail-fast) с указанием нагрузки.

- [x] **`Lesson.SemesterId` (денормализация).** Добавлен FK `SemesterId` на `Lesson` (+ индекс,
  миграция `AddLessonSemesterId`). Прокинут через `ScheduleData` → провайдер → маппер и в
  `CreateLessonCommand`. Удаление/перегенерация теперь могут работать по семестру.

- [x] **Повторная генерация на весь семестр плодит дубли.** `GenerateScheduleCommand` теперь
  удаляет прежнее расписание семестра перед вставкой (одним `SaveChanges`); институтная
  генерация удаляет по (институт + семестр).

- [x] **Пересмотрен `DoubleLessonSectionBuilder`.** Добавлен запрет серий из 3+ (в любом окне из
  трёх слотов занято ≤2); вместе с «нет одиночных» даёт непрерывные блоки ровно по 2.

  > Осталось из P0-блока на потом (P1): publish по-прежнему по институту без фильтра семестра
  > (`GetByInstituteAsync`) — теперь, когда есть `SemesterId`, стоит ограничить семестром.

## 🔴 P0 — масштабируемость солвера

- [ ] **Прунинг переменных по статическим ограничениям.** Сейчас `VariablesSectionBuilder` создаёт
  BoolVar для каждой тройки `W×R×T`, а оборудование/смена/доступность/вместимость накладываются
  поверх как `== 0`. Не создавать заведомо запрещённые `(w,r)` — урежет модель в разы
  (память + время, `IntersectionSectionBuilder` = `O(R·T·W)`).
  _Файлы: `VariablesSectionBuilder.cs`, `EquipmentSectionBuilder.cs`, `ShiftSectionBuilder.cs`._

- [ ] **Асинхронная генерация вместо блокирующего solve в HTTP-запросе.** `Solve` синхронный,
  `SolverOptions.Default` = 180с — endpoint держит поток до 3 минут. Вынести в фоновую обработку
  (Channel / `IHostedService` / Hangfire), возвращать `202 Accepted` + job id, добавить endpoint статуса.
  _Файлы: `Application/Solver/Solving/ScheduleSolver.cs`, `GenerateInstituteScheduleCommand.cs`._

- [ ] **`SolverOptions` из конфигурации.** Сейчас хардкод (180с, 8 воркеров). Вынести в `IOptions`
  + возможность переопределения на запрос.
  _Файл: `Application/Solver/Solving/SolverOptions.cs`._

## 🟡 P1 — EF и консистентность

- [ ] **`LessonRepository`: AsNoTracking + серверная проекция** для read-методов (как в
  `UniversityRepository`). `GetByInstituteAsync` оставить трекаемым (нужен для мутаций в publish).
  _Файл: `Infrastructure/Repositories/LessonRepository.cs`._

- [ ] **Пагинация журнала нагрузки.** `GET /workload/changes` вернёт всё; добавить `skip/take`
  и индекс на `TimeStamp`.
  _Файл: `Infrastructure/Repositories/WorkloadLogRepository.cs`._

- [ ] **`ScheduleDataProvider`: фильтрацию института перенести в SQL.** Сейчас `BelongsToInstitute`/
  `LessonBelongsToInstitute` выполняются в памяти после загрузки с глубокими `Include`.
  _Файл: `Infrastructure/Schedule/ScheduleDataProvider.cs`._

- [ ] **Единый стиль репозиториев.** Привести старый стиль к новому (primary-constructor, `sealed`,
  убрать двойной пробел в `class  LessonRepository`). Переименовать папку `Querys` → `Queries`.

## 🟡 P1 — безопасность и эксплуатация

- [ ] **Аутентификация/авторизация.** `app.UseAuthorization()` без схемы и без `[Authorize]`;
  endpoints генерации и публикации открыты всем. Добавить аутентификацию и защитить мутирующие
  endpoints.
  _Файл: `API/Program.cs`, контроллеры._

- [ ] **Секреты из конфигурации.** Пароль БД в открытом виде в `appsettings.json`. Перенести в
  user-secrets / env / Key Vault (под `.env.example` уже есть задел).
  _Файл: `API/appsettings.json`._

- [ ] **Миграции при старте.** `db.Database.Migrate()` в `Program.cs` опасен при нескольких
  инстансах; вынести в отдельный шаг деплоя или оставить только для dev.
  _Файл: `API/Program.cs:36`._

- [ ] **CORS** настроить под фронтенд.

## 🟡 P1 — тестирование

- [ ] **Юнит-тесты на отдельные section-builder'ы** на крошечных детерминированных моделях
  (3-4 нагрузки). Особенно покрыть новый B+C: `OccupiedResourcesSectionBuilder`,
  `PreviousScheduleAnchorSectionBuilder`.

- [ ] **Тесты на хендлеры и репозитории** (генерация по институту, публикация, журнал нагрузки).

- [ ] **Изолировать медленный dev-тест.** `SemesterScheduleGenerationTests` гоняет солвер до 180с;
  пометить `Trait`/категорией и не запускать в обычном CI.

## ⚪ P2 — мелочи

- [ ] **Централизовать веса штрафов.** Часть в БД (`ConstraintConfig`), часть — magic-numbers в
  коде (`FavoriteBuilding`=5, `Parallelism`=2, `Anchor`=2/1, `DailyLimit`=50). Свести к
  `ConstraintConfig`/`IOptions`.

- [ ] **`ShiftSectionBuilder`: границы смен в конфиг** (сейчас хардкод, отмечено в комментарии).

---

## Контекст по реализованному

Декомпозиция «B+C» (генерация по институту) и эндпоинты из `endpoints.txt` уже реализованы
в ветке `feature/per-institute-scheduling`. Открытые риски по ним вынесены в список выше
(прунинг переменных, асинхронность, тесты на новые builder'ы, фильтрация в SQL).
