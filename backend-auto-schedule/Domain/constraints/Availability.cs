using Domain.university.buildings;
using Domain.university.teachers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.constraints
{
    public class ClassroomAvailability
    {
        public Guid Id { get; set; }
        public int Penalty { get; set; }

        public Classroom Classroom { get; set; }
        public Guid ClassroomId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }

    public class TeacherAvailability
    {
        public Guid Id { get; set; }
        public int Penalty { get; set; }

        public Teacher Teacher { get; set; }
        public Guid TeacherId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }
}
