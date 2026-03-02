using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.schedule
{
    public class Stream
    {
        public Guid Id { get; set; }
        public int StudentsCount { get; set; }


        List<StreamGroups> StreamGroups { get; set; }
        List<Curriculum> Curriculums { get; set; }
        List<Lesson> Lessons { get; set; }
    }
}
