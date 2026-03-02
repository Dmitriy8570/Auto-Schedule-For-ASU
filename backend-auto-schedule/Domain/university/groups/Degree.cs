using Domain.university.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.groups
{
    public enum TypeDegree
    {
        Secondary = 0,
        Bachelor = 1,
        Specialist = 2,
        Master = 3,
        Postgraduate = 4,
        Doctoral = 5
    }
    public class Degree
    {
        public Guid Id { get; set; }
        public TypeDegree TypeDegree { get; set; }

        public List<Course> Courses { get; set; }

        public Guid InstituteId { get; set; }
        public Institute Institute { get; set; }
    }
}
