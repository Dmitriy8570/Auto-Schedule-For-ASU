using Application.solver.builder.builderInterface;
using Application.solver.model;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.solver.builder.buildSections
{
    public class IntersectionSectionBuilder : IModelSectionBuilder
    {
        public void Build(ScheduleModel model)
        {
            AddClassroomConstraint(model);
            AddTeacherConstraints(model);
            AddGroupConstraints(model);
        }

        private void AddClassroomConstraint(ScheduleModel model)
        {
            for (int classroom = 0; classroom < model.Data.Classrooms.Count; classroom++)
            {
                for (int timeslot = 0; timeslot < model.Data.TimeSlots.Count; timeslot++)
                {
                    var literals = new List<ILiteral>();
                    for (int workload = 0; workload < model.Data.SemesterWorkloads.Count; workload++)
                    {
                        literals.Add(model.Lessons[workload, classroom, timeslot]);
                    }
                    model.Model.AddAtMostOne(literals);
                }
            }
        }

        private void AddTeacherConstraints(ScheduleModel model)
        {
            var workloadsByTeacher = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Teacher);
            foreach (var teacherGroup in workloadsByTeacher)
            {
                AddExclusivityConstraint(model, teacherGroup);
            }
        }

        private void AddGroupConstraints(ScheduleModel model)
        {
            var workloadsByGroup = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Stream);
            foreach (var groupWorkloads in workloadsByGroup)
            {
                AddExclusivityConstraint(model, groupWorkloads);
            }
        }

        private void AddExclusivityConstraint(ScheduleModel model, IEnumerable<SemesterWorkload> workloadSubSet)
        {
            var targetIndices = new List<int>();
            for (int i = 0; i < model.Data.SemesterWorkloads.Count; i++)
            {
                if (workloadSubSet.Contains(model.Data.SemesterWorkloads[i]))
                {
                    targetIndices.Add(i);
                }
            }

            foreach (int wIndex in targetIndices)
            {
                for (int slot = 0; slot < model.Data.TimeSlots.Count; slot++)
                {
                    var literals = new List<ILiteral>();
                    for (int room = 0; room < model.Data.Classrooms.Count; room++)
                    {
                        literals.Add(model.Lessons[wIndex, room, slot]);
                    }
                    model.Model.AddAtMostOne(literals);
                }
            }

            for (int slot = 0; slot < model.Data.TimeSlots.Count; slot++)
            {
                for (int room = 0; room < model.Data.Classrooms.Count; room++)
                {
                    var literals = new List<ILiteral>();
                    foreach (int wIndex in targetIndices)
                    {
                        literals.Add(model.Lessons[wIndex, room, slot]);
                    }
                    model.Model.AddAtMostOne(literals);
                }
            }
        }
    }
}