using Application.solver.builder.builderInterface;
using Application.solver.model;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver.builder.buildSections
{
    public class WindowSectionBuilder: IModelSectionBuilder
    {
        public void Build(ScheduleModel model)
        {

        }
        
        private void AddPenalties(ScheduleModel model, IEnumerable<SemesterWorkload> workloadSubSet)
        {
        }
    }
}
