using Domain.constraints.equipments;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.workload
{
    public enum LessonType
    {
        Lecture = 0,
        Seminar = 1,
        Laboratory = 2,
        Consultation = 3,
        Examination = 4
    }
    public class Curriculum
    {
        public Guid Id { get; set; }

        public bool Parallelism { get; set; }
        public bool Double { get; set; }
        

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; }


        public Guid StreamId { get; set; }
        public schedule.Stream Stream { get; set; }


        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; }


        public LessonType LessonType { get; set; }


        public Guid FavoriteBuildingId { get; set; }
        public Building FavoriteBuilding { get; set; }

        public List<WeekWorkload> WeekWorkloads { get; set; }
        public List<SemesterWorkload> semesterWorkloads { get; set; }
        public List<NeededEquipment> NeededEquipments { get; set; }
    }
}
