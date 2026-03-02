using Domain.university.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.teachers
{
    public class Departament
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<Teacher> Teachers { get; set; }
        
        public Guid InstituteId { get; set; }
        public Institute Institute { get; set; }
    }
}
