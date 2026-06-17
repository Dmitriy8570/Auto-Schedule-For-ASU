namespace Application.Solver.Solving;

/// <summary>Одно назначение в решении: индексы в осях модели (нагрузка, аудитория, слот).</summary>
public readonly record struct Assignment(int Workload, int Room, int Slot);
