namespace Attar.Domain.Enums;

/// <summary>
/// وضعیت آزمون کاربر
/// </summary>
public enum TestAttemptStatus
{
    /// <summary>
    /// در حال انجام
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// تکمیل شده
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// منقضی شده
    /// </summary>
    Expired = 3,
    
    /// <summary>
    /// لغو شده
    /// </summary>
    Cancelled = 4
}
