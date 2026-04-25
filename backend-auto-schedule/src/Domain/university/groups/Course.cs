using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.groups
{
    public class Course
    {
        public Guid Id { get; set; }
        public int Number { get; set; }

        public List<Group> Groups { get; set; }

        public Guid DegreeId { get; set; }
        public Degree Degree { get; set; }
    }
}
