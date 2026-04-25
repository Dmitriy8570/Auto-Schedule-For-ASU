using Google.OrTools.Sat;

namespace Application.solver.model
{
    /// <summary>
    /// Модель SAT-задачи составления расписания.
    /// Содержит входные данные, трёхмерный массив булевых переменных и список слагаемых целевой функции.
    /// </summary>
    public class ScheduleModel
    {
        public ScheduleData Data { get; }

        /// <summary>
        /// Трёхмерный массив булевых переменных: [нагрузка, аудитория, временной слот].
        /// Lessons[w, r, t] == 1 означает, что нагрузка w проводится в аудитории r в слоте t.
        /// </summary>
        public BoolVar[,,] Lessons { get; }

        public CpModel Model { get; }

        /// <summary>Слагаемые целевой функции (штрафы), которые солвер минимизирует.</summary>
        public List<LinearExpr> Expr { get; }

        public ScheduleModel(ScheduleData data)
        {
            Data = data;
            Model = new CpModel();
            Lessons = new BoolVar[Data.SemesterWorkloads.Count, Data.Classrooms.Count, Data.TimeSlots.Count];
            Expr = new List<LinearExpr>();
        }
    }
}
