using Google.OrTools.Sat;

namespace Application.solver.model
{
    public class ScheduleModel
    {
        public ScheduleData Data { get; }

        public BoolVar[,,] Lessons { get; }
        public CpModel Model { get; }

        public ScheduleModel(ScheduleData data)
        {
            Data = data;

            Model = new CpModel();
            Lessons = new BoolVar[Data.SemesterWorkloads.Count, Data.Classrooms.Count, Data.TimeSlots.Count];
        }
    }
}