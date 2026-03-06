using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver
{
    public class ScheduleModel
    {
        private IReadOnlyList<SemesterWorkload> _semesterWorkloads;
        private IReadOnlyList<Classroom> _classrooms;
        private IReadOnlyList<TimeSlot> _timeSlots;

        private Dictionary<(int, int, int), BoolVar> lessons;
        private CpModel model;

        public ScheduleModel(IEnumerable<SemesterWorkload> semesterWorkloads, IEnumerable<Classroom> classrooms, IEnumerable<TimeSlot> timeSlots)
        {
            _semesterWorkloads = semesterWorkloads.ToList();
            _classrooms = classrooms.ToList();
            _timeSlots = timeSlots.ToList();
        }

        public CpModel InitializingModel()
        {
            model = new CpModel();
            lessons = new Dictionary<(int WorkloadId, int RoomId, int SlotId), BoolVar>();

            for (int i = 0; i < _semesterWorkloads.Count; i++)
            {
                var workload = _semesterWorkloads[i];
                for (int j = 0; j < _classrooms.Count; j++)
                {
                    for (int k = 0; k < _timeSlots.Count; k++)
                    {
                        lessons[(i, j, k)] = model.NewBoolVar($"lesson_{i}_{j}_{k}");
                    }
                }
            }

            return model;
        }
    }
}
