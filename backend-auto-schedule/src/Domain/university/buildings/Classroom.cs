using Domain.constraints;
using Domain.constraints.equipments;
using Domain.schedule;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.university.buildings
{
    public class Classroom
    {
        public Guid Id {  get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }

        public Guid BuildingId { get; set; }
        public Building Building { get; set; } 

        public List<Lesson> Lessons { get; set; }

        public List<ClassroomAvailability> ClassroomAvailabilitys { get; set; }
        public List<EquipmentRoom> EquipmentRooms { get; set; }
    }
}
