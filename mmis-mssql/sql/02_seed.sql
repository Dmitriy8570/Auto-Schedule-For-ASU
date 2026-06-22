/* ============================================================================
   MMIS — генерация данных на весь семестр (структура и наименования — по образцу
   Алтайского государственного университета: реальные институты, кафедры, шифры групп
   и профильные дисциплины). Числовые объёмы подобраны так, чтобы расписание было
   осуществимым на dev-масштабе; распределение нагрузки — процедурное:
     - 3 института АлтГУ, 9 кафедр, 36 преподавателей
     - 36 групп (4 курса x 3 группы x 3 института), 20..30 студентов
     - 28 дисциплин (математика, информатика, физика, экономика, общеобразовательные)
     - 1 семестр = 18 недель (чередование красная/синяя)
     - нагрузка группы зависит от курса: 16/15/14/13 пар/неделю (26..32 ч)
     - суммарно 522 пары/неделю распределены между преподавателями по точным
       целям 12..17 пар => у КАЖДОГО преподавателя 24..34 ч/неделю (в норме 20..40)
     - семестровая и понедельная нагрузка по каждому плану
     - агрегированная нагрузка по преподавателям и группам (см. 03_views.sql)
   Договорённость: 1 пара = 2 академических часа.
   Скрипт идемпотентен: если данные уже загружены — выходит без изменений.
   ============================================================================ */

USE MMIS;
GO

IF EXISTS (SELECT 1 FROM mmis.Groups)
BEGIN
    PRINT 'MMIS seed: данные уже загружены — пропуск.';
    RETURN;
END;

SET NOCOUNT ON;

/* ---------- Институты (АлтГУ) ---------- */
INSERT mmis.Institutes(Name) VALUES
 (N'Институт математики и информационных технологий'),
 (N'Международный институт экономики, менеджмента и информационных систем'),
 (N'Институт цифровых технологий, электроники и физики');

/* ---------- Кафедры (по 3 на институт, наименования — по образцу АлтГУ) ---------- */
INSERT mmis.Departments(Name, InstituteId)
SELECT d.Name, i.Id
FROM (VALUES
  (N'Кафедра информатики',                                  N'Институт математики и информационных технологий'),
  (N'Кафедра теоретической кибернетики и прикладной математики', N'Институт математики и информационных технологий'),
  (N'Кафедра математического анализа',                      N'Институт математики и информационных технологий'),
  (N'Кафедра экономической теории',                         N'Международный институт экономики, менеджмента и информационных систем'),
  (N'Кафедра менеджмента, маркетинга и организации производства', N'Международный институт экономики, менеджмента и информационных систем'),
  (N'Кафедра финансов и кредита',                           N'Международный институт экономики, менеджмента и информационных систем'),
  (N'Кафедра общей и экспериментальной физики',             N'Институт цифровых технологий, электроники и физики'),
  (N'Кафедра радиофизики и теоретической физики',           N'Институт цифровых технологий, электроники и физики'),
  (N'Кафедра информационной безопасности',                  N'Институт цифровых технологий, электроники и физики')
) d(Name, InstName)
JOIN mmis.Institutes i ON i.Name = d.InstName;

/* индекс кафедр 0..8 -> реальный Id */
DECLARE @DeptIds TABLE (idx INT, Id INT);
INSERT @DeptIds SELECT ROW_NUMBER() OVER (ORDER BY Id) - 1, Id FROM mmis.Departments;

/* ---------- Преподаватели (36) ---------- */
DECLARE @Last TABLE (i INT IDENTITY(0,1), v NVARCHAR(50));
INSERT @Last(v) VALUES
 (N'Иванов'),(N'Петров'),(N'Сидоров'),(N'Кузнецов'),(N'Смирнов'),(N'Попов'),
 (N'Васильев'),(N'Соколов'),(N'Михайлов'),(N'Новиков'),(N'Фёдоров'),(N'Морозов'),
 (N'Волков'),(N'Алексеев'),(N'Лебедев'),(N'Семёнов'),(N'Егоров'),(N'Павлов'),
 (N'Козлов'),(N'Степанов'),(N'Николаев'),(N'Орлов'),(N'Андреев'),(N'Макаров');

DECLARE @FN TABLE (i INT IDENTITY(0,1), v NVARCHAR(10));
INSERT @FN(v) VALUES
 (N'А.'),(N'Б.'),(N'В.'),(N'Г.'),(N'Д.'),(N'Е.'),(N'И.'),
 (N'К.'),(N'Л.'),(N'М.'),(N'Н.'),(N'О.'),(N'П.'),(N'С.');

DECLARE @ON TABLE (i INT IDENTITY(0,1), v NVARCHAR(10));
INSERT @ON(v) VALUES
 (N'А.'),(N'В.'),(N'Г.'),(N'Д.'),(N'И.'),(N'М.'),(N'Н.'),(N'П.'),(N'С.'),(N'Ф.');

DECLARE @Pos TABLE (i INT IDENTITY(0,1), v NVARCHAR(50));
INSERT @Pos(v) VALUES
 (N'Профессор'),(N'Доцент'),(N'Старший преподаватель'),(N'Преподаватель'),(N'Ассистент');

DECLARE @Deg TABLE (i INT IDENTITY(0,1), v NVARCHAR(50));
INSERT @Deg(v) VALUES
 (N'д-р наук'),(N'канд. наук'),(N'—'),(N'канд. наук'),(N'—');

;WITH T AS (
    SELECT TOP (36) rn = ROW_NUMBER() OVER (ORDER BY (SELECT 1)) - 1
    FROM sys.all_objects
)
INSERT mmis.Teachers(FullName, Position, AcademicDegree, DepartmentId)
SELECT l.v + N' ' + f.v + N' ' + o.v, p.v, g.v, dd.Id
FROM T
JOIN @Last l   ON l.i  = T.rn % 24
JOIN @FN   f   ON f.i  = (T.rn * 5 + 1) % 14
JOIN @ON   o   ON o.i  = (T.rn * 3 + 2) % 10
JOIN @Pos  p   ON p.i  = T.rn % 5
JOIN @Deg  g   ON g.i  = T.rn % 5
JOIN @DeptIds dd ON dd.idx = T.rn % 9;

/* ---------- Дисциплины (28: профильные ИМИТ / МИЭМИС / ИЦТЭФ + общеобразовательные) ---------- */
INSERT mmis.Subjects(Name) VALUES
 /* ИМИТ — математика и информатика */
 (N'Математический анализ'),(N'Линейная алгебра и аналитическая геометрия'),
 (N'Дискретная математика'),(N'Дифференциальные уравнения'),
 (N'Теория вероятностей и математическая статистика'),(N'Программирование'),
 (N'Алгоритмы и структуры данных'),(N'Базы данных'),
 (N'Операционные системы'),(N'Объектно-ориентированное программирование'),
 (N'Машинное обучение'),(N'Численные методы'),
 /* МИЭМИС — экономика и менеджмент */
 (N'Микроэкономика'),(N'Макроэкономика'),(N'Менеджмент'),(N'Маркетинг'),
 (N'Бухгалтерский учёт'),(N'Финансы и кредит'),(N'Эконометрика'),
 /* ИЦТЭФ — физика и электроника */
 (N'Общая физика'),(N'Электродинамика'),(N'Компьютерные сети'),
 (N'Информационная безопасность'),(N'Архитектура ЭВМ'),
 /* Общеобразовательные */
 (N'Философия'),(N'История России'),(N'Иностранный язык'),(N'Физическая культура');

/* ---------- Ступени и курсы ---------- */
INSERT mmis.Degrees(InstituteId, Type)
SELECT Id, N'Bachelor' FROM mmis.Institutes;

;WITH C AS (SELECT TOP (4) n = ROW_NUMBER() OVER (ORDER BY (SELECT 1)) FROM sys.all_objects)
INSERT mmis.Courses(DegreeId, Number)
SELECT d.Id, C.n FROM mmis.Degrees d CROSS JOIN C;

/* ---------- Группы (36): шифр <код института>-<курс><№> ---------- */
DECLARE @Prefix TABLE (InstituteId INT, Code NVARCHAR(10));
INSERT @Prefix
SELECT i.Id, x.Code
FROM mmis.Institutes i
JOIN (VALUES
  (N'Институт математики и информационных технологий',                     N'ИВТ'),
  (N'Международный институт экономики, менеджмента и информационных систем', N'ЭК'),
  (N'Институт цифровых технологий, электроники и физики',                  N'РФ')
) x(Name, Code) ON x.Name = i.Name;

;WITH G AS (SELECT TOP (3) s = ROW_NUMBER() OVER (ORDER BY (SELECT 1)) FROM sys.all_objects)
INSERT mmis.Groups(Name, CourseId, Shift, StudentCount)
SELECT
  p.Code + N'-' + CAST(co.Number AS NVARCHAR(2)) + RIGHT(N'0' + CAST(G.s AS NVARCHAR(2)), 2),
  co.Id, 0,
  20 + (ABS(CHECKSUM(NEWID())) % 11)            -- 20..30 студентов
FROM mmis.Courses co
JOIN mmis.Degrees de ON de.Id = co.DegreeId
JOIN @Prefix p ON p.InstituteId = de.InstituteId
CROSS JOIN G;

/* ---------- Семестр + 18 недель ---------- */
DECLARE @SemStart DATE = '2026-02-09';        -- весенний семестр, понедельник
INSERT mmis.Semesters(Name, StartDate, EndDate)
VALUES (N'Весенний семестр 2025/2026', @SemStart, DATEADD(DAY, 18*7 - 3, @SemStart));
DECLARE @SemId INT = SCOPE_IDENTITY();

;WITH W AS (SELECT TOP (18) n = ROW_NUMBER() OVER (ORDER BY (SELECT 1)) FROM sys.all_objects)
INSERT mmis.Weeks(SemesterId, Number, StartDate, EndDate, WeekType)
SELECT @SemId, n,
       DATEADD(DAY, (n-1)*7,     @SemStart),
       DATEADD(DAY, (n-1)*7 + 5, @SemStart),
       CASE WHEN n % 2 = 1 THEN 0 ELSE 1 END   -- нечётная — красная, чётная — синяя
FROM W;

/* ============================================================================
   Учебный план и распределение нагрузки.
   Идея: каждая строка плана = 1 пара/неделю. Объём группы (число строк) задаётся
   курсом: 16/15/14/13. Все строки нумеруются (rn = 0..R-1), а каждому
   преподавателю выделяется НЕПРЕРЫВНЫЙ диапазон ровно нужной длины (его «цель» —
   12..17 пар). Так нагрузка точно укладывается в 24..34 ч/неделю у каждого.
   ============================================================================ */
DECLARE @SubCount INT = (SELECT COUNT(*) FROM mmis.Subjects);

DECLARE @Subj TABLE (idx INT, Id INT);
INSERT @Subj SELECT ROW_NUMBER() OVER (ORDER BY Id) - 1, Id FROM mmis.Subjects;

/* Преподаватели с персональной целью нагрузки (пар/неделю) и непрерывным
   диапазоном строк плана [cumStart, cumEnd). Цель = 12 + (idx % 6) => 12..17. */
DECLARE @Tch TABLE (idx INT, Id INT, target INT, cumStart INT, cumEnd INT);
INSERT @Tch (idx, Id, target)
SELECT ROW_NUMBER() OVER (ORDER BY Id) - 1,
       Id,
       12 + ((ROW_NUMBER() OVER (ORDER BY Id) - 1) % 6)
FROM mmis.Teachers;

UPDATE t
SET cumStart = c.cs,
    cumEnd   = c.cs + t.target
FROM @Tch t
JOIN (
    SELECT idx,
           cs = ISNULL(SUM(target) OVER (ORDER BY idx
                       ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0)
    FROM @Tch
) c ON c.idx = t.idx;

;WITH
Tally AS (SELECT TOP (16) k = ROW_NUMBER() OVER (ORDER BY (SELECT 1)) - 1 FROM sys.all_objects),
GL AS (   -- объём (пар/неделю) для каждой группы по её курсу
    SELECT g.Id AS GroupId,
           gi    = ROW_NUMBER() OVER (ORDER BY g.Id) - 1,
           pairs = CASE co.Number WHEN 1 THEN 16 WHEN 2 THEN 15 WHEN 3 THEN 14 ELSE 13 END
    FROM mmis.Groups g
    JOIN mmis.Courses co ON co.Id = g.CourseId
),
GroupPlan AS (  -- разворачиваем группу в её строки плана и нумеруем глобально
    SELECT gl.GroupId, gl.gi, t.k,
           rn = ROW_NUMBER() OVER (ORDER BY gl.gi, t.k) - 1
    FROM GL gl
    JOIN Tally t ON t.k < gl.pairs
)
INSERT mmis.Curriculums(TeacherId, GroupId, SubjectId, LessonType, IsParallel, IsDouble, PairsPerWeek)
SELECT
    tc.Id,
    p.GroupId,
    s.Id,
    p.k % 3,                                   -- 0 лекция, 1 семинар, 2 лаб (чередуются)
    CASE WHEN p.k % 3 = 0 THEN 1 ELSE 0 END,   -- лекции параллельны
    0,
    1                                          -- 1 пара/неделю на строку плана
FROM GroupPlan p
JOIN @Tch tc ON p.rn >= tc.cumStart AND p.rn < tc.cumEnd
JOIN @Subj s ON s.idx = (p.gi * 5 + p.k * 3) % @SubCount;

/* ---------- Семестровая нагрузка (1 пара = 2 ак. часа, 18 недель) ---------- */
INSERT mmis.SemesterWorkloads(CurriculumId, SemesterId, Hours)
SELECT c.Id, @SemId, c.PairsPerWeek * 2 * 18
FROM mmis.Curriculums c;

/* ---------- Недельная нагрузка по каждой неделе ---------- */
INSERT mmis.WeekWorkloads(CurriculumId, WeekId, SemesterWorkloadId, Hours)
SELECT c.Id, w.Id, sw.Id, c.PairsPerWeek * 2
FROM mmis.Curriculums c
JOIN mmis.SemesterWorkloads sw ON sw.CurriculumId = c.Id
JOIN mmis.Weeks w ON w.SemesterId = @SemId;

/* ---------- Итоги (подзапросы — в переменные, иначе PRINT не компилируется) ---------- */
DECLARE @cInst INT, @cDept INT, @cTch INT, @cGrp INT, @cSub INT,
        @cWeek INT, @cCur INT, @cSem INT, @cWk INT;
SELECT @cInst = COUNT(*) FROM mmis.Institutes;
SELECT @cDept = COUNT(*) FROM mmis.Departments;
SELECT @cTch  = COUNT(*) FROM mmis.Teachers;
SELECT @cGrp  = COUNT(*) FROM mmis.Groups;
SELECT @cSub  = COUNT(*) FROM mmis.Subjects;
SELECT @cWeek = COUNT(*) FROM mmis.Weeks;
SELECT @cCur  = COUNT(*) FROM mmis.Curriculums;
SELECT @cSem  = COUNT(*) FROM mmis.SemesterWorkloads;
SELECT @cWk   = COUNT(*) FROM mmis.WeekWorkloads;

PRINT 'MMIS seed выполнен:';
PRINT '  институтов:     ' + CAST(@cInst AS VARCHAR(10));
PRINT '  кафедр:         ' + CAST(@cDept AS VARCHAR(10));
PRINT '  преподавателей: ' + CAST(@cTch  AS VARCHAR(10));
PRINT '  групп:          ' + CAST(@cGrp  AS VARCHAR(10));
PRINT '  дисциплин:      ' + CAST(@cSub  AS VARCHAR(10));
PRINT '  недель:         ' + CAST(@cWeek AS VARCHAR(10));
PRINT '  планов:         ' + CAST(@cCur  AS VARCHAR(10));
PRINT '  сем. нагрузок:  ' + CAST(@cSem  AS VARCHAR(10));
PRINT '  нед. нагрузок:  ' + CAST(@cWk   AS VARCHAR(10));
GO
