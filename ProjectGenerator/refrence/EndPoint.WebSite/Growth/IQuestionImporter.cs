namespace EndPoint.WebSite.Growth;

public interface IQuestionImporter
{
    Task ImportCliftonAsync(string xlsxPath, CancellationToken cancellationToken);

    Task ImportPvqAsync(string csvPath, CancellationToken cancellationToken);
}
