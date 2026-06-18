# TODO / Бэклог улучшений бэкенда

Список составлен по результатам ревью архитектуры и кода. Сгруппирован по приоритету;
ссылки указывают на файлы в `backend-auto-schedule/src`.

Статус: `[ ]` — не начато, `[~]` — в работе, `[x]` — сделано.

---

## 🔴 P0 — корректность модели расписания

- [ ] **Ограничение вместимости аудитории.** `Classroom.Capacity` и `AcademicStream.StudentsCount`
  не используются ни одним builder'ом — поток может попасть в маленькую аудиторию.
  Добавить `CapacitySectionBuilder` (жёсткий): запретить `Lessons[w,r,t]`, где
  `Classrooms[r].Capacity < StudentsCount`. Включить в оба набора директора.
  _Файлы: `Application/Solver/Builder/BuildSections/`, `ScheduleModelDirector.cs`._

- [ ] **Чётность часов в `TotalHoursSectionBuilder`.** `Hours / 2` молча теряет нечётные часы
  (3ч → 1 пара). Добавить валидацию чётности `Hours` или явную политику округления.
  _Файл: `Application/Solver/Builder/BuildSections/TotalHoursSectionBuilder.cs:22`._

- [ ] **`Lesson.SemesterId` (денормализация).** Сейчас семестр занятия достижим только через
  `TimeSlot→WeekDay→Week→Semester`; из-за этого publish/delete работают по институту через
  все семестры, а запросы расписания делают лишние джойны. Добавить FK `SemesterId` на `Lesson`,
  миграцию, обновить запросы/команды.
  _Файлы: `Domain/schedule/Lesson.cs`, `Infrastructure/Data/Configurations/LessonConfiguration.cs`, миграции._

- [ ] **Повторная генерация на весь семестр плодит дубли.** `GenerateScheduleCommand` только
  добавляет занятия (без удаления прежних), в отличие от по-институтной команды. Унифицировать
  «заменить, а не добавить».
  _Файл: `Application/Common/Lessons/Commands/GenerateScheduleCommand.cs`._

- [ ] **Пересмотреть `DoubleLessonSectionBuilder`.** Гарантирует наличие соседа, но не запрещает
  3+ подряд и не гарантирует ровно пары; с `TotalHours` возможны странные раскладки.
  Рассмотреть моделирование двойной пары одной переменной на пару слотов.
  _Файл: `Application/Solver/Builder/BuildSections/DoubleLessonSectionBuilder.cs`._

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
