using Domain.university.common;

namespace Domain.university.groups
{
    /// <summary>Ступень образования.</summary>
    public enum TypeDegree
    {
        Secondary = 0,
        Bachelor = 1,
        Specialist = 2,
        Master = 3,
        Postgraduate = 4,
        Doctoral = 5
    }

    /// <summary>Образовательная ступень внутри института (бакалавриат, магистратура и т.д.).</summary>
    public class Degree
    {
        private Degree() { }

        public Guid Id { get; private set; }
        public TypeDegree TypeDegree { get; private set; }

        /// <summary>Курсы (по годам обучения) данной ступени.</summary>
        public List<Course> Courses { get; private set; }

        public Guid InstituteId { get; private set; }
        public Institute Institute { get; private set; }
    }
}
