namespace LogTableRenameTest.SharedKernel.Extensions;

public static class EnumerableExtensions
{
    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        => source switch
        {
            IReadOnlyCollection<T> readOnlyCollection => readOnlyCollection,
            null => throw new ArgumentNullException(nameof(source)),
            _ => source.ToList().AsReadOnly()
        };
}
