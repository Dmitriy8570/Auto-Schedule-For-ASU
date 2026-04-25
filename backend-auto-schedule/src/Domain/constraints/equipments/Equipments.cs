using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.constraints.equipments
{
    public class Equipment
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<EquipmentRoom> EquipmentRooms { get; set; }
        public List<NeededEquipment> NeededEquipments { get; set; }
    }
}
