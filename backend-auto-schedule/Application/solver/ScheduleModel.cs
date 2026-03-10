using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver
{
    public class ScheduleModel
    {
        public IReadOnlyList<SemesterWorkload> semesterWorkloads;
        public IReadOnlyList<Classroom> classrooms;
        public IReadOnlyList<TimeSlot> timeSlots;

        public Dictionary<(int, int, int), BoolVar> lessons;
        public CpModel model;

        public ScheduleModel(IEnumerable<SemesterWorkload> semesterWorkloads, IEnumerable<Classroom> classrooms, IEnumerable<TimeSlot> timeSlots)
        {
            semesterWorkloads = semesterWorkloads.ToList();
            classrooms = classrooms.ToList();
            timeSlots = timeSlots.ToList();

            model = new CpModel();
            lessons = new Dictionary<(int WorkloadId, int RoomId, int SlotId), BoolVar>();
        }
    }
}
