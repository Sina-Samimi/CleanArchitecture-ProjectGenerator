using System.Globalization;
using System.Text;
using Arsis.Application.Assessments;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class MatrixLoader
{
    private readonly ILogger<MatrixLoader> _logger;

    public MatrixLoader(ILogger<MatrixLoader> logger)
    {
        _logger = logger;
    }

    public (Matrix<double> matrix, IReadOnlyList<string> rowIds, IReadOnlyList<string> columnIds) LoadMatrix(string path, string matrixName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException($"Path for matrix '{matrixName}' cannot be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Matrix file '{matrixName}' was not found.", path);
        }

        _logger.LogInformation("Loading matrix {MatrixName} from {Path}", matrixName, path);

        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length < 2)
        {
            throw new InvalidOperationException($"Matrix '{matrixName}' must contain at least one data row.");
        }

        var header = SplitCsvLine(lines[0]);
        if (header.Length < 2)
        {
            throw new InvalidOperationException($"Matrix '{matrixName}' header must contain at least one column.");
        }

        var columnIds = header.Skip(1).Select(id => id.Trim()).ToList();
        var rowIds = new List<string>();
        var data = new double[lines.Length - 1, columnIds.Count];

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i]);
            if (parts.Length < 1)
            {
                continue;
            }

            var rowId = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(rowId))
            {
                throw new InvalidOperationException($"Matrix '{matrixName}' contains a row without identifier at line {i + 1}.");
            }

            rowIds.Add(rowId);

            for (var j = 1; j < header.Length; j++)
            {
                var valueString = j < parts.Length ? parts[j] : "0";
                if (!double.TryParse(valueString, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
                {
                    value = 0d;
                }

                data[i - 1, j - 1] = value;
            }
        }

        var matrix = Matrix<double>.Build.DenseOfArray(data);
        return (matrix, rowIds, columnIds);
    }

    public IReadOnlyDictionary<string, int> LoadSkillLevels(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path for skill levels cannot be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Skill levels file was not found.", path);
        }

        _logger.LogInformation("Loading skill levels from {Path}", path);

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i]);
            if (parts.Length < 2)
            {
                continue;
            }

            var skillId = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(skillId))
            {
                continue;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
            {
                level = 1;
            }

            result[skillId] = level;
        }

        return result;
    }

    private static string[] SplitCsvLine(string line)
    {
        return line
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(token => token.Trim('\"'))
            .ToArray();
    }
}
