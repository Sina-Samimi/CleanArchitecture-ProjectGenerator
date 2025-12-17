namespace LogTableRenameTest.Domain.Enums;

/// <summary>
/// نوع سوال تست
/// </summary>
public enum TestQuestionType
{
    /// <summary>
    /// چند گزینه‌ای (یک انتخاب)
    /// </summary>
    MultipleChoice = 1,
    
    /// <summary>
    /// چند گزینه‌ای (چند انتخاب)
    /// </summary>
    MultipleSelect = 2,
    
    /// <summary>
    /// درست/غلط
    /// </summary>
    TrueFalse = 3,
    
    /// <summary>
    /// مقیاس لیکرت
    /// </summary>
    LikertScale = 4,
    
    /// <summary>
    /// متنی کوتاه
    /// </summary>
    ShortText = 5,
    
    /// <summary>
    /// متنی بلند
    /// </summary>
    LongText = 6
}
