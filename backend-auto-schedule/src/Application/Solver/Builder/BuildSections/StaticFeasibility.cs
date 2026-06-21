using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Статические (не зависящие от решения солвера) проверки осуществимости тройки
/// (нагрузка, аудитория, слот). Заведомо невозможные тройки не получают переменной в модели
/// (прунинг, см. <see cref="VariablesSectionBuilder"/>) — это резко сокращает размер модели.
/// Проверки null-устойчивы: при отсутствии данных навигаций (минимальные модели тестов)
/// ограничение считается неопределённым ⇒ тройка разрешена. Логика согласована с жёсткими
/// строителями <see cref="CapacitySectionBuilder"/>, <see cref="EquipmentSectionBuilder"/>,
/// <see cref="ShiftSectionBuilder"/>.
/// </summary>
internal static class StaticFeasibility
{
    /// <summary>Аудитория заведомо непригодна для нагрузки (вместимость или оборудование).</summary>
    public static bool RoomForbidden(SemesterWorkload workload, Classroom classroom)
    {
        int students = workload.Curriculum?.Stream?.StudentsCount ?? 0;
        if (students > 0 && !classroom.CanAccommodate(students))
            return true;

        var needed = workload.Curriculum?.NeededEquipments;
        if (needed is { Count: > 0 })
        {
            var available = classroom.EquipmentRooms.Select(er => er.EquipmentId).ToHashSet();
            if (!needed.Select(ne => ne.EquipmentId).ToHashSet().IsSubsetOf(available))
                return true;
        }

        return false;
    }

    /// <summary>Слот заведомо запрещён нагрузке по смене (хотя бы одна группа потока не может в нём заниматься).</summary>
    public static bool SlotForbidden(SemesterWorkload workload, TimeSlot timeSlot)
    {
        var groups = workload.Curriculum?.Stream?.StreamGroups;
        if (groups is null || groups.Count == 0)
            return false;

        // Group может быть не загружена в минимальных моделях — тогда смену не ограничиваем.
        return !groups.All(sg =>
            sg.Group is null || ShiftSectionBuilder.SlotMatchesShift(sg.Group.Shift, timeSlot.Number));
    }
}
