using Application.Solver.Builder.BuildSections;
using Application.Solver.Model;
using Domain.schedule;
using Domain.university.groups;

namespace Application.Solver.Compaction;

/// <summary>
/// Пост-уплотнение чернового расписания: сдвигает занятия в более ранние свободные слоты того же
/// дня, чтобы убрать «окна» (пустые слоты между занятиями у одного субъекта — преподавателя или
/// группы). Зачем это нужно отдельным проходом: расписание строится по институтам и компонентам
/// связности <em>последовательно</em>, а уже занятые (преподаватель, слот)/(аудитория, слот)
/// передаются следующим как жёсткие блокировки. Преподаватель, ведущий в нескольких местах,
/// получает слоты «застолблёнными» первой подзадачей — у остальных его пары раздвигаются, образуя
/// окна. Штраф за окно мягкий и не пересиливает жёсткую блокировку, поэтому окна убираются здесь,
/// детерминированной перестановкой уже размещённых занятий.
///
/// Уплотнение консервативно (вариант «в рамках того же корпуса»): занятие остаётся в своей
/// аудитории — значит сохраняются вместимость, оборудование и корпус, — и переносится только если
/// перестановка <em>строго уменьшает</em> суммарное число окон у затронутых субъектов и не нарушает
/// ни одного жёсткого ограничения (пересечения ресурсов, смена группы, переходы между корпусами,
/// блокировки чужих институтов). Двойные пары не двигаются, чтобы не разорвать спаренность.
/// </summary>
public static class ScheduleCompactor
{
    /// <summary>Страховочный предел числа проходов на случай патологий (обычно сходится за 2–3 прохода).</summary>
    private const int MaxPasses = 100;

    /// <summary>
    /// Уплотнить расписание на месте, переназначая слоты у занятий из <paramref name="lessons"/>.
    /// </summary>
    /// <param name="lessons">Размещённые занятия одного института (мутируются через <see cref="Lesson.Reschedule"/>).</param>
    /// <param name="data">Данные института: аудитории (корпус), слоты и нагрузки (преподаватель/группы/двойная пара).</param>
    /// <param name="externalRooms">Аудитории, занятые в слотах расписанием других институтов семестра.</param>
    /// <param name="externalTeachers">Преподаватели, занятые в слотах расписанием других институтов семестра.</param>
    /// <returns>Сколько занятий было перенесено.</returns>
    public static int Compact(
        IReadOnlyList<Lesson> lessons,
        ScheduleData data,
        IReadOnlyCollection<OccupiedSlot> externalRooms,
        IReadOnlyCollection<OccupiedTeacherSlot> externalTeachers)
    {
        if (lessons.Count == 0) return 0;

        var slotById = data.TimeSlots.ToDictionary(s => s.Id);
        var buildingByRoom = data.Classrooms.ToDictionary(c => c.Id, c => c.BuildingId);
        var curById = BuildCurriculumIndex(data);

        // Слоты дня по порядку пар + позиция слота внутри дня (для соседства при проверке переездов).
        var slotsByDay = data.TimeSlots
            .GroupBy(s => s.WeekDayId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Number).ToList());
        var positionInDay = new Dictionary<Guid, int>();
        foreach (var day in slotsByDay.Values)
            for (int i = 0; i < day.Count; i++) positionInDay[day[i].Id] = i;

        var externalRoomSet = new HashSet<(Guid Room, Guid Slot)>(
            externalRooms.Select(o => (o.ClassroomId, o.TimeSlotId)));
        var externalTeacherSet = new HashSet<(Guid Teacher, Guid Slot)>(
            externalTeachers.Select(o => (o.TeacherId, o.TimeSlotId)));

        // Превращаем занятия в подвижные элементы; элементы с неизвестным слотом/планом остаются
        // неподвижными препятствиями (их ресурсы всё равно учитываются в занятости).
        var items = new List<Item>(lessons.Count);
        foreach (var lesson in lessons)
        {
            if (!slotById.TryGetValue(lesson.TimeSlotId, out var slot)) continue;
            if (!buildingByRoom.TryGetValue(lesson.ClassroomId, out var building)) continue;

            CurriculumInfo? info = lesson.CurriculumId is { } cid && curById.TryGetValue(cid, out var ci) ? ci : null;
            items.Add(new Item
            {
                Lesson = lesson,
                Slot = slot,
                RoomId = lesson.ClassroomId,
                BuildingId = building,
                TeacherId = info?.TeacherId ?? Guid.Empty,
                Groups = info?.Groups ?? Array.Empty<(Guid, Shift)>(),
                // Двигаем только обычные занятия с известным субъектом; двойные пары держим спаренными.
                Movable = info is { Double: false },
            });
        }

        var roomBusy = new HashSet<(Guid Room, Guid Slot)>();
        var teacherBusy = new HashSet<(Guid Teacher, Guid Slot)>();
        var groupBusy = new HashSet<(Guid Group, Guid Slot)>();
        var byTeacher = new Dictionary<Guid, List<Item>>();
        var byGroup = new Dictionary<Guid, List<Item>>();
        foreach (var it in items)
        {
            roomBusy.Add((it.RoomId, it.Slot.Id));
            if (it.TeacherId != Guid.Empty)
            {
                teacherBusy.Add((it.TeacherId, it.Slot.Id));
                Add(byTeacher, it.TeacherId, it);
            }
            foreach (var (groupId, _) in it.Groups)
            {
                groupBusy.Add((groupId, it.Slot.Id));
                Add(byGroup, groupId, it);
            }
        }

        int moved = 0;
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool changed = false;
            // Тянем пары влево, обрабатывая их в порядке возрастания номера: ранние занимают
            // свободные слоты раньше, поздние подтягиваются за ними.
            foreach (var it in items.Where(i => i.Movable).OrderBy(i => i.Slot.WeekDayId).ThenBy(i => i.Slot.Number).ToList())
            {
                TimeSlot? best = null;
                int bestDelta = 0;

                foreach (var target in slotsByDay[it.Slot.WeekDayId])
                {
                    if (target.Number >= it.Slot.Number) break; // только более ранние слоты того же дня
                    if (!IsValidMove(it, target)) continue;

                    int delta = WindowDelta(it, target);
                    if (delta < bestDelta) { bestDelta = delta; best = target; }
                }

                if (best is not null && bestDelta < 0)
                {
                    ApplyMove(it, best);
                    moved++;
                    changed = true;
                }
            }

            if (!changed) break;
        }

        return moved;

        // --- Локальные функции ---

        bool IsValidMove(Item it, TimeSlot target)
        {
            var slotId = target.Id;

            // Смена группы: целевой номер пары должен подходить всем группам.
            foreach (var (_, shift) in it.Groups)
                if (!ShiftSectionBuilder.SlotMatchesShift(shift, target.Number)) return false;

            // Ресурсы свободны (учитываем и блокировки чужих институтов).
            if (roomBusy.Contains((it.RoomId, slotId)) || externalRoomSet.Contains((it.RoomId, slotId))) return false;
            if (it.TeacherId != Guid.Empty &&
                (teacherBusy.Contains((it.TeacherId, slotId)) || externalTeacherSet.Contains((it.TeacherId, slotId)))) return false;
            foreach (var (groupId, _) in it.Groups)
                if (groupBusy.Contains((groupId, slotId))) return false;

            // Переходы между корпусами: в соседних парах субъект не должен оказаться в другом корпусе.
            if (it.TeacherId != Guid.Empty && byTeacher.TryGetValue(it.TeacherId, out var tList) && TravelConflict(tList, it, target))
                return false;
            foreach (var (groupId, _) in it.Groups)
                if (byGroup.TryGetValue(groupId, out var gList) && TravelConflict(gList, it, target)) return false;

            return true;
        }

        bool TravelConflict(List<Item> subjectItems, Item it, TimeSlot target)
        {
            var day = slotsByDay[target.WeekDayId];
            int p = positionInDay[target.Id];
            Guid? prevId = p > 0 ? day[p - 1].Id : null;
            Guid? nextId = p < day.Count - 1 ? day[p + 1].Id : null;

            foreach (var other in subjectItems)
            {
                if (ReferenceEquals(other, it)) continue;
                if ((other.Slot.Id == prevId || other.Slot.Id == nextId) && other.BuildingId != it.BuildingId)
                    return true;
            }
            return false;
        }

        // Изменение суммарного числа окон у затронутых субъектов (преподаватель + группы) при переносе.
        int WindowDelta(Item it, TimeSlot target)
        {
            var dayId = it.Slot.WeekDayId;
            int before = AffectedWindows(it, dayId);
            var original = it.Slot;
            it.Slot = target;
            int after = AffectedWindows(it, dayId);
            it.Slot = original;
            return after - before;
        }

        int AffectedWindows(Item it, Guid dayId)
        {
            int total = 0;
            if (it.TeacherId != Guid.Empty && byTeacher.TryGetValue(it.TeacherId, out var tList))
                total += WindowsOf(tList, dayId);
            foreach (var (groupId, _) in it.Groups)
                if (byGroup.TryGetValue(groupId, out var gList))
                    total += WindowsOf(gList, dayId);
            return total;
        }

        int WindowsOf(List<Item> subjectItems, Guid dayId)
        {
            int min = int.MaxValue, max = int.MinValue;
            var busy = new HashSet<int>();
            foreach (var s in subjectItems)
                if (s.Slot.WeekDayId == dayId)
                {
                    busy.Add(s.Slot.Number);
                    if (s.Slot.Number < min) min = s.Slot.Number;
                    if (s.Slot.Number > max) max = s.Slot.Number;
                }
            if (busy.Count == 0) return 0;

            int windows = 0;
            foreach (var slot in slotsByDay[dayId])
                if (slot.Number > min && slot.Number < max && !busy.Contains(slot.Number))
                    windows++;
            return windows;
        }

        void ApplyMove(Item it, TimeSlot target)
        {
            roomBusy.Remove((it.RoomId, it.Slot.Id));
            if (it.TeacherId != Guid.Empty) teacherBusy.Remove((it.TeacherId, it.Slot.Id));
            foreach (var (groupId, _) in it.Groups) groupBusy.Remove((groupId, it.Slot.Id));

            it.Slot = target;
            it.Lesson.Reschedule(it.RoomId, target.Id, it.Lesson.StreamId, it.Lesson.CurriculumId);

            roomBusy.Add((it.RoomId, target.Id));
            if (it.TeacherId != Guid.Empty) teacherBusy.Add((it.TeacherId, target.Id));
            foreach (var (groupId, _) in it.Groups) groupBusy.Add((groupId, target.Id));
        }
    }

    private static void Add(Dictionary<Guid, List<Item>> index, Guid key, Item item)
    {
        if (!index.TryGetValue(key, out var list)) index[key] = list = new List<Item>();
        list.Add(item);
    }

    private static Dictionary<Guid, CurriculumInfo> BuildCurriculumIndex(ScheduleData data)
    {
        var result = new Dictionary<Guid, CurriculumInfo>();
        foreach (var workload in data.Workloads)
        {
            var curriculum = workload.Curriculum;
            if (curriculum is null || result.ContainsKey(curriculum.Id)) continue;

            var groups = curriculum.Stream?.StreamGroups
                .Select(sg => (sg.GroupId, sg.Group?.Shift ?? Shift.Unspecified))
                .ToArray() ?? Array.Empty<(Guid, Shift)>();

            result[curriculum.Id] = new CurriculumInfo(curriculum.TeacherId, groups, curriculum.Double);
        }
        return result;
    }

    private readonly record struct CurriculumInfo(
        Guid TeacherId, IReadOnlyList<(Guid GroupId, Shift Shift)> Groups, bool Double);

    private sealed class Item
    {
        public required Lesson Lesson { get; init; }
        public required TimeSlot Slot { get; set; }
        public required Guid RoomId { get; init; }
        public required Guid BuildingId { get; init; }
        public required Guid TeacherId { get; init; }
        public required IReadOnlyList<(Guid GroupId, Shift Shift)> Groups { get; init; }
        public required bool Movable { get; init; }
    }
}
