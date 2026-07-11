using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace Dwuma.Training.DataExport;

public sealed class InteractionDataExporter
{
    private readonly string _connectionString;

    public InteractionDataExporter(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "The database connection string cannot be empty.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<int> ExportAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "The CSV output path cannot be empty.",
                nameof(outputPath));
        }

        string? outputDirectory =
            Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        const string sql = """
    SELECT
        CAST(JI.UserId AS NVARCHAR(50)) AS UserId,
        CAST(JI.JobListingId AS NVARCHAR(50)) AS JobId,

        ISNULL(JI.SkillMatch, 0) AS SkillMatch,
        ISNULL(JI.LocationMatch, 0) AS LocationMatch,
        ISNULL(JI.ExperienceMatch, 0) AS ExperienceMatch,
        ISNULL(JI.IndustryMatch, 0) AS IndustryMatch,
        ISNULL(JI.SalaryMatch, 0) AS SalaryMatch,
        ISNULL(JI.JobAgeDays, 0) AS JobAgeDays,

        CASE
            WHEN JI.Clicked = 1 THEN 1.0
            ELSE 0.0
        END AS Clicked,

        CASE
            WHEN JI.Saved = 1 THEN 1.0
            ELSE 0.0
        END AS Saved,

        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM APPLICATIONS AS A
                WHERE A.user_id = JI.UserId
                  AND A.job_listing_id = JI.JobListingId
            )
            THEN 1.0
            ELSE 0.0
        END AS Applied,

        ISNULL(JI.Rating, 0) AS Rating

    FROM JOB_INTERACTIONS AS JI
    ORDER BY JI.Id;
    """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.CommandTimeout = 60;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        await using var writer =
            new StreamWriter(
                outputPath,
                append: false,
                encoding: new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync(
            "UserId,JobId,SkillMatch,LocationMatch," +
            "ExperienceMatch,IndustryMatch,SalaryMatch," +
            "JobAgeDays,Clicked,Saved,Applied,Rating");

        int exportedRows = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            string csvRow = string.Join(
                ",",
                EscapeCsv(GetString(reader, "UserId")),
                EscapeCsv(GetString(reader, "JobId")),
                FormatNumber(reader["SkillMatch"]),
                FormatNumber(reader["LocationMatch"]),
                FormatNumber(reader["ExperienceMatch"]),
                FormatNumber(reader["IndustryMatch"]),
                FormatNumber(reader["SalaryMatch"]),
                FormatNumber(reader["JobAgeDays"]),
                FormatNumber(reader["Clicked"]),
                FormatNumber(reader["Saved"]),
                FormatNumber(reader["Applied"]),
                FormatNumber(reader["Rating"]));

            await writer.WriteLineAsync(csvRow);

            exportedRows++;
        }

        await writer.FlushAsync(cancellationToken);

        return exportedRows;
    }

    private static string GetString(
        SqlDataReader reader,
        string columnName)
    {
        object value = reader[columnName];

        return value == DBNull.Value
            ? string.Empty
            : Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatNumber(object value)
    {
        if (value == DBNull.Value)
        {
            return "0";
        }

        float number = Convert.ToSingle(
            value,
            CultureInfo.InvariantCulture);

        return number.ToString(
            CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}