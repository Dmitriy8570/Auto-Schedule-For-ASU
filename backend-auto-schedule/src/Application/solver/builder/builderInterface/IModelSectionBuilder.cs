using Application.Solver.Model;

namespace Application.Solver.Builder.BuilderInterface;

public interface IModelSectionBuilder
{
    void Build(ScheduleModel model);
}
