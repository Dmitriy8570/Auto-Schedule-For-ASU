using Domain.university.buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.constraints.equipments
{
    public class EquipmentRoom
    {
        public Guid EquipmentId { get; set; }
        public Guid ClassroomId { get; set; }

        public Equipment Equipment { get; set; }
        public Classroom Classroom { get; set; }
    }
}
