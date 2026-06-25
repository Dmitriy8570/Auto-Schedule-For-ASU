using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Domain.university.groups;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// <em>Мягкое</em> ограничение смены: занятие предпочитает полосу слотов своей смены (1-я — пары 1..4,
/// 2-я — 5..6, вечерняя — 7+), но может выйти за неё со штрафом, растущим с расстоянием от полосы.
/// В отличие от жёсткого <see cref="ShiftSectionBuilder"/>, это не «коробка» в 4 слота на день: при
/// плотной нагрузке (например, 15 пар у группы при общих преподавателях) жёсткая смена физически не
/// даёт упаковать день без окон, а мягкая — расширяет манёвр, поэтому окна реально устранимы. Вес
/// (<see cref="SolverPenaltyWeights.ShiftOutOfBand"/>) держится ниже штрафов окна и учебного дня,
/// поэтому выход за смену происходит лишь тогда, когда снимает окно или лишний день.
/// </summary>
public class SoftShiftSectionBuilder(SolverPenaltyWeights weights) : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        int weight = weights.ShiftOutOfBand;
        if (weight <= 0) return;

        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var groups = model.Data.Workloads[w].Curriculum.Stream.StreamGroups;
            if (groups.Count == 0) continue;

            for (int t = 0; t < model.TimeSlotCount; t++)
            {
                int number = model.Data.TimeSlots[t].Number;

                // Расстояние от полосы смены — по самой «нарушенной» группе потока.
                int distance = groups.Max(sg => OutOfBandDistance(sg.Group.Shift, number));
                if (distance == 0) continue;

                for (int r = 0; r < model.ClassroomCount; r++)
                    if (model.Lessons[w, r, t] is { } cell)
                        model.Objective.Add(LinearExpr.Term(cell, weight * distance));
            }
        }
    }

    /// <summary>
    /// На сколько пар номер слота выходит за полосу смены (0 — внутри полосы). Полосы те же, что у
    /// жёсткого варианта: 1-я смена — 1..4, 2-я — 5..6, вечерняя — 7+; <see cref="Shift.Unspecified"/>
    /// без ограничения.
    /// </summary>
    internal static int OutOfBandDistance(Shift shift, int lessonNumber) => shift switch
    {
        Shift.First => Math.Max(0, lessonNumber - 4),
        Shift.Second => Math.Max(0, 5 - lessonNumber) + Math.Max(0, lessonNumber - 6),
        Shift.Evening => Math.Max(0, 7 - lessonNumber),
        _ => 0
    };
}
