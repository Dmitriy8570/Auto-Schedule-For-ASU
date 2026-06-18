namespace Application.Common.Exceptions;

/// <summary>
/// Вход не выполнен: неверные учётные данные либо отсутствие членства в требуемой
/// группе доступа. Обрабатывается как 401 Unauthorized.
/// </summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);
