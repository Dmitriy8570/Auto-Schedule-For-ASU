using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver
{
    public class ScheduleModelBuilder
    {
        private readonly ScheduleModel _model;

        public ScheduleModelBuilder(IEnumerable<SemesterWorkload> semesterWorkloads, IEnumerable<Classroom> classrooms, IEnumerable<TimeSlot> timeSlots)
        {
            _model = new ScheduleModel(semesterWorkloads, classrooms, timeSlots);
        }

        public ScheduleModelBuilder AddVariables()
        {
            for (int workload = 0; workload < _model.SemesterWorkloads.Count; workload++)
            {
                for (int classroom = 0; classroom < _model.Classrooms.Count; classroom++)
                {
                    for (int timeslot = 0; timeslot < _model.TimeSlots.Count; timeslot++)
                    {
                        _model.Lessons[workload, classroom, timeslot] = _model.Model.NewBoolVar($"lesson_{workload}_{classroom}_{timeslot}");
                    }
                }
            }
            return this;
        }

        public ScheduleModelBuilder AddClassroomConstraint()
        {
            for (int classroom = 0; classroom < _model.Classrooms.Count; classroom++)
            {
                for (int timeslot = 0; timeslot < _model.TimeSlots.Count; timeslot++)
                {
                    var literals = new List<ILiteral>();
                    for (int workload = 0; workload < _model.SemesterWorkloads.Count; workload++)
                    {
                        literals.Add(_model.Lessons[workload, classroom, timeslot]);
                    }
                    _model.Model.AddAtMostOne(literals);
                }
            }
            return this;
        }

        public ScheduleModelBuilder AddTeacherConstraints()
        {
            var workloadsByTeacher = _model.SemesterWorkloads.GroupBy(w => w.Curriculum.Teacher);
            foreach (var teacherGroup in workloadsByTeacher)
            {
                AddExclusivityConstraint(teacherGroup);
            }
            return this;
        }

        public ScheduleModelBuilder AddGroupConstraints()
        {
            var workloadsByGroup = _model.SemesterWorkloads.GroupBy(w => w.Curriculum.Stream);
            foreach (var groupWorkloads in workloadsByGroup)
            {
                AddExclusivityConstraint(groupWorkloads);
            }
            return this;
        }

        private void AddExclusivityConstraint(IEnumerable<SemesterWorkload> workloadSubSet)
        {
            var targetIndices = new List<int>();
            for (int i = 0; i < _model.SemesterWorkloads.Count; i++)
            {
                if (workloadSubSet.Contains(_model.SemesterWorkloads[i]))
                {
                    targetIndices.Add(i);
                }
            }

            for (int slot = 0; slot < _model.TimeSlots.Count; slot++)
            {
                for (int room = 0; room < _model.Classrooms.Count; room++)
                {
                    var literals = new List<ILiteral>();

                    foreach (int wIndex in targetIndices)
                    {
                        literals.Add(_model.Lessons[wIndex, room, slot]);
                    }

                    _model.Model.AddAtMostOne(literals);
                }
            }
        }

        public ScheduleModel Build()
        {
            return _model;
        }
    }
}