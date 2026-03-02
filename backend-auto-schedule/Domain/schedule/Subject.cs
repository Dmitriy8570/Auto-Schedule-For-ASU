using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.schedule
{
    public class Subject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<Curriculum> Currilumus { get; set; }
    }
}
