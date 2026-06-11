namespace Domain.constraints.equipments
{
    /// <summary>Оборудование, которое может быть установлено в аудитории или требоваться для занятия.</summary>
    public class Equipment
    {
        private Equipment() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Аудитории, оснащённые данным оборудованием.</summary>
        public List<EquipmentRoom> EquipmentRooms { get; private set; }

        /// <summary>Учебные планы, требующие данное оборудование.</summary>
        public List<NeededEquipment> NeededEquipments { get; private set; }
    }
}
