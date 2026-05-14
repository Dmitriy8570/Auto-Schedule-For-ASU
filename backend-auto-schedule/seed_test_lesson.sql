-- Тестовые данные: один урок со всеми необходимыми зависимостями

DO $$
DECLARE
    v_building_id     uuid := gen_random_uuid();
    v_classroom_id    uuid := gen_random_uuid();
    v_stream_id       uuid := gen_random_uuid();
    v_semester_id     uuid := gen_random_uuid();
    v_week_id         uuid := gen_random_uuid();
    v_weekday_id      uuid := gen_random_uuid();
    v_timeslot_id     uuid := gen_random_uuid();
    v_lesson_id       uuid := gen_random_uuid();
BEGIN

    INSERT INTO "Building" ("Id", "Name")
    VALUES (v_building_id, 'Главный корпус');

    INSERT INTO "Classrooms" ("Id", "Name", "Capacity", "BuildingId")
    VALUES (v_classroom_id, '101', 30, v_building_id);

    INSERT INTO "AcademicStream" ("Id", "StudentsCount")
    VALUES (v_stream_id, 25);

    INSERT INTO "Semester" ("Id", "StartDate", "EndDate")
    VALUES (v_semester_id, '2026-02-01', '2026-06-30');

    -- WeekType: 0 = нечётная, 1 = чётная
    INSERT INTO "Week" ("Id", "StartDate", "EndDate", "WeekType", "SemesterId")
    VALUES (v_week_id, '2026-05-11', '2026-05-17', 0, v_semester_id);

    -- DayOfWeek: 1=Пн, 2=Вт, 3=Ср, 4=Чт, 5=Пт
    INSERT INTO "WeekDay" ("Id", "WeekId", "DayOfWeek")
    VALUES (v_weekday_id, v_week_id, 1);

    INSERT INTO "TimeSlot" ("Id", "WeekDayId", "Number")
    VALUES (v_timeslot_id, v_weekday_id, 1);

    INSERT INTO "Lessons" ("Id", "ClassroomId", "TimeSlotId", "StreamId", "Version")
    VALUES (v_lesson_id, v_classroom_id, v_timeslot_id, v_stream_id, 1);

    RAISE NOTICE 'Lesson created: %', v_lesson_id;
    RAISE NOTICE 'Stream:         %', v_stream_id;
    RAISE NOTICE 'Classroom:      %', v_classroom_id;
    RAISE NOTICE 'TimeSlot:       %', v_timeslot_id;

END $$;
