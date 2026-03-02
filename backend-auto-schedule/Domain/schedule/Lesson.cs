using Domain.calendar;
using Domain.university.buildings;
using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.schedule
{
    public enum Version
    {
        Current = 0,
        Draft = 1
    }
    public class Lesson
    {
        public Guid Id { get; set; }

        public Classroom Classroom { get; set; }
        public Guid ClassroomId { get; set; }

        public Guid TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; }

        public Stream Stream { get; set; }
        public Guid StreamId { get; set; }

        public Version Version { get; set; }
    }
}
