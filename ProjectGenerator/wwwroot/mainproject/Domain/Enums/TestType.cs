namespace Attar.Domain.Enums;

/// <summary>
/// نوع تست
/// </summary>
public enum TestType
{
    /// <summary>
    /// تست عمومی با سوالات دلخواه
    /// </summary>
    General = 1,
    
    /// <summary>
    /// تست DISC
    /// </summary>
    Disc = 2,
    
    /// <summary>
    /// تست کلیفتون (StrengthsFinder)
    /// </summary>
    Clifton = 3,
    
    /// <summary>
    /// تست هوش ریون (Raven's Matrices)
    /// </summary>
    Raven = 4,
    
    /// <summary>
    /// تست شخصیت شناسی
    /// </summary>
    Personality = 5,

    /// <summary>
    /// آزمون ترکیبی کلیفتون و شوارتز
    /// </summary>
    CliftonSchwartz = 6
}
