/* ============================================================================
   MMIS — имитация исходной базы MS SQL (ММИС «Деканат»).
   Схема справочников и учебной нагрузки, из которой Auto-Schedule
   импортирует данные. Скрипт идемпотентен: повторный запуск ничего не ломает.
   ============================================================================ */

IF DB_ID('MMIS') IS NULL
    CREATE DATABASE MMIS;
GO

USE MMIS;
GO

IF SCHEMA_ID('mmis') IS NULL
    EXEC('CREATE SCHEMA mmis');
GO

/* ---------- Организационная структура ---------- */

IF OBJECT_ID('mmis.Institutes') IS NULL
CREATE TABLE mmis.Institutes (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL              -- институт / факультет
);

IF OBJECT_ID('mmis.Departments') IS NULL
CREATE TABLE mmis.Departments (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(200) NOT NULL,      -- кафедра
    InstituteId INT NOT NULL REFERENCES mmis.Institutes(Id)
);

IF OBJECT_ID('mmis.Teachers') IS NULL
CREATE TABLE mmis.Teachers (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    FullName       NVARCHAR(200) NOT NULL,   -- ФИО (Фамилия И. О.)
    Position       NVARCHAR(100) NULL,       -- должность
    AcademicDegree NVARCHAR(100) NULL,       -- учёная степень
    DepartmentId   INT NOT NULL REFERENCES mmis.Departments(Id)
);

/* ---------- Студенты: ступени, курсы, группы ---------- */

IF OBJECT_ID('mmis.Degrees') IS NULL
CREATE TABLE mmis.Degrees (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    InstituteId INT NOT NULL REFERENCES mmis.Institutes(Id),
    Type        NVARCHAR(50) NOT NULL        -- Bachelor / Master / Specialist ...
);

IF OBJECT_ID('mmis.Courses') IS NULL
CREATE TABLE mmis.Courses (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    DegreeId INT NOT NULL REFERENCES mmis.Degrees(Id),
    Number   INT NOT NULL                    -- номер курса (1..4)
);

IF OBJECT_ID('mmis.Groups') IS NULL
CREATE TABLE mmis.Groups (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(50) NOT NULL,      -- шифр группы, напр. ИВТ-101
    CourseId     INT NOT NULL REFERENCES mmis.Courses(Id),
    Shift        TINYINT NOT NULL DEFAULT 0, -- 0 первая, 1 вторая, 2 вечерняя
    StudentCount INT NOT NULL                -- численность группы
);

/* ---------- Дисциплины ---------- */

IF OBJECT_ID('mmis.Subjects') IS NULL
CREATE TABLE mmis.Subjects (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL
);

/* ---------- Календарь ---------- */

IF OBJECT_ID('mmis.Semesters') IS NULL
CREATE TABLE mmis.Semesters (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(100) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate   DATE NOT NULL
);

IF OBJECT_ID('mmis.Weeks') IS NULL
CREATE TABLE mmis.Weeks (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    SemesterId INT NOT NULL REFERENCES mmis.Semesters(Id),
    Number     INT NOT NULL,                 -- номер недели в семестре
    StartDate  DATE NOT NULL,
    EndDate    DATE NOT NULL,
    WeekType   TINYINT NOT NULL              -- 0 красная (числитель), 1 синяя (знаменатель)
);

/* ---------- Учебный план (нагрузка): кто, что и кому ведёт ---------- */

IF OBJECT_ID('mmis.Curriculums') IS NULL
CREATE TABLE mmis.Curriculums (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    TeacherId    INT NOT NULL REFERENCES mmis.Teachers(Id),
    GroupId      INT NOT NULL REFERENCES mmis.Groups(Id),
    SubjectId    INT NOT NULL REFERENCES mmis.Subjects(Id),
    LessonType   TINYINT NOT NULL,           -- 0 лекция,1 семинар,2 лаб,3 консульт,4 экзамен
    IsParallel   BIT NOT NULL DEFAULT 0,     -- может идти параллельно с другими потоками
    IsDouble     BIT NOT NULL DEFAULT 0,     -- двойная пара
    PairsPerWeek INT NOT NULL                -- пар в неделю по этому плану
);

/* ---------- Нагрузка на весь семестр (в академических часах) ---------- */

IF OBJECT_ID('mmis.SemesterWorkloads') IS NULL
CREATE TABLE mmis.SemesterWorkloads (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    CurriculumId INT NOT NULL REFERENCES mmis.Curriculums(Id),
    SemesterId   INT NOT NULL REFERENCES mmis.Semesters(Id),
    Hours        INT NOT NULL                -- суммарные часы за семестр
);

/* ---------- Недельная нагрузка (часы по каждой неделе) ---------- */

IF OBJECT_ID('mmis.WeekWorkloads') IS NULL
CREATE TABLE mmis.WeekWorkloads (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    CurriculumId       INT NOT NULL REFERENCES mmis.Curriculums(Id),
    WeekId             INT NOT NULL REFERENCES mmis.Weeks(Id),
    SemesterWorkloadId INT NOT NULL REFERENCES mmis.SemesterWorkloads(Id),
    Hours              INT NOT NULL          -- часы на конкретной неделе
);
GO
