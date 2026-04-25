using Application.solver.builder.builderInterface;
using Application.solver.model;
using Domain.university.teachers;
using Google.OrTools.Sat;


namespace Application.solver.builder.buildSections
{
    public class TotalHoursConstraintSectionBuilder : IModelSectionBuilder
    {
        public void Build(ScheduleModel model)
        {
            var workloads = model.Data.SemesterWorkloads.Index();

            foreach (var (index, item) in workloads)
            {
                var taskVars = new List<LinearExpr>();

                for (int room = 0; room < model.Data.Classrooms.Count; room++)
                {
                    for (int slot = 0; slot < model.Data.TimeSlots.Count; slot++)
                    {
                        taskVars.Add(model.Lessons[index, room, slot]);
                    }
                }

                model.Model.Add(LinearExpr.Sum(taskVars) == item.Hours / 2);
            }
        }
    }
}
