namespace Domain.constraints.equipments
{
    /// <summary>Оборудование, которое может быть установлено в аудитории или требоваться для занятия.</summary>
    public class Equipment
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Аудитории, оснащённые данным оборудованием.</summary>
        public List<EquipmentRoom> EquipmentRooms { get; set; }

        /// <summary>Учебные планы, требующие данное оборудование.</summary>
        public List<NeededEquipment> NeededEquipments { get; set; }
    }
}
