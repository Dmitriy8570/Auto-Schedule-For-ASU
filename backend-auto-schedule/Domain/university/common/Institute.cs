using Domain.university.groups;
using Domain.university.teachers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.common
{
    public class Institute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<Degree> Degrees { get; set; }
        public List<Departament> Departaments { get; set; }
    }
}
