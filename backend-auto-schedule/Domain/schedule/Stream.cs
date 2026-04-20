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


        public List<StreamGroups> StreamGroups { get; set; }
        public List<Curriculum> Curriculums { get; set; }
        public List<Lesson> Lessons { get; set; }
    }
}
