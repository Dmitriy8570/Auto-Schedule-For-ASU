using Domain.schedule;
using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.groups
{
    public enum Shift
    {
        First = 0,
        Second = 1,
        Evening = 2,
    }

    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Shift Shift { get; set; }
        public Group ParentGroup { get; set; }
        public int StudentCount { get; set; }

        public List<Group> Groups { get; set; }

        public Guid CourseId { get; set; }
        public Course Course { get; set; }

        public List<StreamGroups> StreamGroups { get; set; }
    }
}
