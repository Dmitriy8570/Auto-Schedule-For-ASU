using Application.solver.builder.builderInterface;
using Application.solver.model;

namespace Application.solver.builder
{
    public class ScheduleModelDirector
    {
        private readonly IReadOnlyList<IModelSectionBuilder> _builders;

        public ScheduleModelDirector(IReadOnlyList<IModelSectionBuilder> builders)
        {
            _builders = builders;
        }

        public ScheduleModel Build(ScheduleData data)
        {
            var model = new ScheduleModel(data);

            foreach (var builder in _builders)
            {
                builder.Build(model);
            }

            return model;
        }
    }
}
