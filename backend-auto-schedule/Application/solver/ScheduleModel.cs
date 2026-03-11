using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver
{
    public class ScheduleModel
    {
        public IReadOnlyList<SemesterWorkload> SemesterWorkloads { get; }
        public IReadOnlyList<Classroom> Classrooms { get; }
        public IReadOnlyList<TimeSlot> TimeSlots { get; }

        public BoolVar[,,] Lessons { get; }
        public CpModel Model { get; }

        public ScheduleModel(IEnumerable<SemesterWorkload> semesterWorkloads, IEnumerable<Classroom> classrooms, IEnumerable<TimeSlot> timeSlots)
        {
            SemesterWorkloads = semesterWorkloads.ToList();
            Classrooms = classrooms.ToList();
            TimeSlots = timeSlots.ToList();

            Model = new CpModel();
            Lessons = new BoolVar[SemesterWorkloads.Count, Classrooms.Count, TimeSlots.Count];
        }
    }
}