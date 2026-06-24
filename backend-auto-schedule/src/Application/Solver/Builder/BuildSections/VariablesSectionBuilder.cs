using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Инициализирует трёхмерный массив булевых переменных модели: для каждой допустимой тройки
/// (нагрузка, аудитория, временной слот) создаётся BoolVar.
///
/// Прунинг (трек B): тройки, заведомо невозможные по статическим критериям (вместимость,
/// оборудование, смена — см. <see cref="StaticFeasibility"/>), переменной НЕ получают
/// (ячейка остаётся <c>null</c>). На реальных данных эти критерии отсекают большинство
/// аудиторий/слотов для каждой нагрузки, поэтому число переменных и размер ограничений падают
/// в разы. Все последующие строители, целевая функция и извлечение решения обязаны пропускать
/// <c>null</c>-ячейки (тройка просто не существует в модели).
/// </summary>
public class VariablesSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var workload = model.Data.Workloads[w];

            for (int r = 0; r < model.ClassroomCount; r++)
            {
                if (StaticFeasibility.RoomForbidden(workload, model.Data.Classrooms[r]))
                    continue; // вся аудитория недопустима для нагрузки — ни одного слота.

                for (int t = 0; t < model.TimeSlotCount; t++)
                {
                    if (StaticFeasibility.SlotForbidden(workload, model.Data.TimeSlots[t]))
                        continue;

                    model.Lessons[w, r, t] = model.Model.NewBoolVar($"lesson_{w}_{r}_{t}");
                }
            }
        }
    }
}
