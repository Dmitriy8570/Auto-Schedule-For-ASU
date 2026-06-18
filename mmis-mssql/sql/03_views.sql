/* ============================================================================
   MMIS — представления с агрегированной нагрузкой.
   Удобно для проверки: нагрузка по группам и по преподавателям
   (на неделю и на весь семестр). CREATE OR ALTER — безопасно при повторном
   запуске.
   ============================================================================ */

USE MMIS;
GO

/* Недельная нагрузка по группам (в парах и ак. часах). */
CREATE OR ALTER VIEW mmis.vGroupWeeklyLoad AS
SELECT g.Id AS GroupId, g.Name AS GroupName,
       SUM(c.PairsPerWeek)     AS PairsPerWeek,
       SUM(c.PairsPerWeek) * 2 AS WeeklyHours
FROM mmis.Groups g
JOIN mmis.Curriculums c ON c.GroupId = g.Id
GROUP BY g.Id, g.Name;
GO

/* Семестровая нагрузка по группам. */
CREATE OR ALTER VIEW mmis.vGroupSemesterLoad AS
SELECT g.Id AS GroupId, g.Name AS GroupName,
       SUM(sw.Hours) AS SemesterHours
FROM mmis.Groups g
JOIN mmis.Curriculums c        ON c.GroupId = g.Id
JOIN mmis.SemesterWorkloads sw ON sw.CurriculumId = c.Id
GROUP BY g.Id, g.Name;
GO

/* Недельная нагрузка по преподавателям. */
CREATE OR ALTER VIEW mmis.vTeacherWeeklyLoad AS
SELECT t.Id AS TeacherId, t.FullName,
       SUM(c.PairsPerWeek)     AS PairsPerWeek,
       SUM(c.PairsPerWeek) * 2 AS WeeklyHours
FROM mmis.Teachers t
JOIN mmis.Curriculums c ON c.TeacherId = t.Id
GROUP BY t.Id, t.FullName;
GO

/* Семестровая нагрузка по преподавателям. */
CREATE OR ALTER VIEW mmis.vTeacherSemesterLoad AS
SELECT t.Id AS TeacherId, t.FullName, d.Name AS Department,
       SUM(sw.Hours) AS SemesterHours
FROM mmis.Teachers t
JOIN mmis.Departments d        ON d.Id = t.DepartmentId
JOIN mmis.Curriculums c        ON c.TeacherId = t.Id
JOIN mmis.SemesterWorkloads sw ON sw.CurriculumId = c.Id
GROUP BY t.Id, t.FullName, d.Name;
GO

/* Детализация учебного плана (плоская витрина для импорта/проверки). */
CREATE OR ALTER VIEW mmis.vWorkloadDetails AS
SELECT
    c.Id            AS CurriculumId,
    inst.Name       AS Institute,
    dep.Name        AS Department,
    t.FullName      AS Teacher,
    g.Name          AS [Group],
    sub.Name        AS Subject,
    c.LessonType,
    c.PairsPerWeek,
    sw.Hours        AS SemesterHours,
    sem.Name        AS Semester
FROM mmis.Curriculums c
JOIN mmis.Teachers t           ON t.Id = c.TeacherId
JOIN mmis.Departments dep      ON dep.Id = t.DepartmentId
JOIN mmis.Institutes inst      ON inst.Id = dep.InstituteId
JOIN mmis.Groups g             ON g.Id = c.GroupId
JOIN mmis.Subjects sub         ON sub.Id = c.SubjectId
JOIN mmis.SemesterWorkloads sw ON sw.CurriculumId = c.Id
JOIN mmis.Semesters sem        ON sem.Id = sw.SemesterId;
GO
