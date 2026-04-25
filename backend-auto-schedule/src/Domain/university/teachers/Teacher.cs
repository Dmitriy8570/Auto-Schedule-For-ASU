using Domain.constraints;
using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.teachers
{
    public class Teacher
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid DepartmentId { get; set; }
        public Departament Departament { get; set; }

        public List<Curriculum> Currilumus { get; set; }
        public List<TeacherAvailability> TeacherAvailabilitys { get; set; }
    }
}
