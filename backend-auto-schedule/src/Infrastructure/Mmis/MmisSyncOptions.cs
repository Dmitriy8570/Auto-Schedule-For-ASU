namespace Infrastructure.Mmis;

/// <summary>Настройки синхронизации с MMIS (секция конфигурации "Mmis").</summary>
public sealed class MmisSyncOptions
{
    public const string SectionName = "Mmis";

    /// <summary>Строка подключения к MS SQL БД MMIS.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Включена ли фоновая синхронизация. Если строка подключения пуста — служба тоже бездействует.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Выполнить один прогон сразу при старте приложения.</summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>Локальное время ежедневного прогона в формате "HH:mm" (по умолчанию ночью).</summary>
    public string SyncAtTime { get; set; } = "03:00";

    /// <summary>
    /// Необязательная замена ночного расписания фиксированным интервалом в минутах
    /// (если задано &gt; 0, служба запускается каждые N минут вместо <see cref="SyncAtTime"/>).
    /// </summary>
    public int? SyncIntervalMinutes { get; set; }
}
