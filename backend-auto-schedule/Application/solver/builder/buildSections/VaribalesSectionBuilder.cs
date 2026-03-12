using Application.solver.builder.builderInterface;
using Application.solver.model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.solver.builder.buildSections
{
    public class VaribalesSectionBuilder: IModelSectionBuilder
    {
        public void Build(ScheduleModel model)
        {
            for (int workload = 0; workload < model.Data.SemesterWorkloads.Count; workload++)
            {
                for (int classroom = 0; classroom < model.Data.Classrooms.Count; classroom++)
                {
                    for (int timeslot = 0; timeslot < model.Data.TimeSlots.Count; timeslot++)
                    {
                        model.Lessons[workload, classroom, timeslot] = model.Model.NewBoolVar($"lesson_{workload}_{classroom}_{timeslot}");
                    }
                }
            }
        }
    }
}
