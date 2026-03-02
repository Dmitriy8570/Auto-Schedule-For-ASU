using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.buildings
{
    public class Building
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<Classroom> Classrooms { get; set; }

        public List<Curriculum> Currilumus { get; set; }
    }
}
