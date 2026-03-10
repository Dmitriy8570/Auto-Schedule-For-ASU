using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver
{
    public class ScheduleModelBuilder
    {
        private ScheduleModel _model;
        private List<ILiteral> _literals; //List for creating constraints
        public ScheduleModelBuilder(IEnumerable<SemesterWorkload> semesterWorkloads, IEnumerable<Classroom> classrooms, IEnumerable<TimeSlot> timeSlots)
        {
            ScheduleModel _model = new ScheduleModel(semesterWorkloads, classrooms, timeSlots);
            _literals = new List<ILiteral>();
        }

        // Populates the dictionary with boolean flags
        public ScheduleModelBuilder AddVariables()
        {
            for (int workload = 0; workload < _model.semesterWorkloads.Count; workload++)
            {
                for (int classroom = 0; classroom < _model.classrooms.Count; classroom++)
                {
                    for (int timeslot = 0; timeslot < _model.timeSlots.Count; timeslot++)
                    {
                        _model.lessons[(workload, classroom, timeslot)] = _model.model.NewBoolVar($"lesson_{workload}_{classroom}_{timeslot}");
                    }
                }
            }
            return this;
        }

        private void AddExclusivityConstraint(IEnumerable<SemesterWorkload> workloadSubSet)
        {
            foreach (var slot in _model.timeSlots)
            {
                var literals = new List<BoolVar>();

                foreach (var workload in workloadSubSet)
                {
                    foreach (var room in _model.classrooms)
                    {
                        // Собираем все переменные для данного подмножества нагрузок в этот слот
                        if (_model.lessons.TryGetValue((workload.Id, room.Id, slot.Id), out var variable))
                        {
                            literals.Add(variable);
                        }
                    }
                }

                // Ключевое ограничение: только одна пара из списка может быть истинной
                _model.model.AddAtMostOne(literals);
            }
        }

        // Adds a constraint to prevent scheduling conflicts (overlapping classes)
        public ScheduleModelBuilder AddNoOverlapConstraint()
        {
            for (int classroom = 0; classroom < _model.classrooms.Count; classroom++)
            {
                for (int timeslot = 0; timeslot < _model.timeSlots.Count; timeslot++)
                {
                    for (int workload = 0; workload < _model.semesterWorkloads.Count; workload++)
                    {
                        _literals.Add(_model.lessons[(workload, classroom, timeslot)]);
                    }
                    _model.model.AddAtMostOne(_literals);
                    _literals.Clear();
                }
            }

            foreach (Teacher teacher in _model.semesterWorkloads.Select(t => t.Curriculum.Teacher))
            {
                for (int workload = 0; workload < _model.semesterWorkloads.Count; workload++)
                {
                    if (teacher == _model.semesterWorkloads[workload].Curriculum.Teacher)
                    {
                        for (int timeslot = 0; timeslot < _model.timeSlots.Count; timeslot++)
                        {
                            for (int classroom = 0; classroom < _model.classrooms.Count; classroom++)
                            {
                                _literals.Add(_model.lessons[(workload, classroom, timeslot)]);
                            }
                            _model.model.AddAtMostOne(_literals);
                            _literals.Clear();
                        }
                    }
                }
            }
            return this;
        }
    }
}
