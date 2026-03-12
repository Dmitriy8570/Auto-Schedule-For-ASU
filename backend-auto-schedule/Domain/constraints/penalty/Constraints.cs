using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.constraints.penalty
{
    public enum ConstraintType
    {
        TeacherGap = 0,
        StudentGap = 1,
        ClassroomAvailability = 2,
        TeacherAvailability = 3,
    }

    public class ConstraintConfig
    {
        public Guid Id { get; set; }
        public ConstraintType ConstraintType { get; set; }
        public int Penalty { get; set; }

    }
}
