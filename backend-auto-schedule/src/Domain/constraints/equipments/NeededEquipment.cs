using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.constraints.equipments
{
    public class NeededEquipment
    {
        public Guid CurriculumId { get; set; }
        public Guid EquipmentId { get; set; }

        public Curriculum Curriculum { get; set; }
        public Equipment Equipment { get; set; }
    }
}
