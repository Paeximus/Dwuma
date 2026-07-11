using CareerAgent.Training;
using Dwuma.Training.DataExport;
using Microsoft.ML;

string projectDirectory = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

string dataDirectory = Path.Combine(
    projectDirectory,
    "Data");

string modelDirectory = Path.Combine(
    projectDirectory,
    "Models");

string dataPath = Path.Combine(
    dataDirectory,
    "user_job_interactions.csv");

string modelPath = Path.Combine(
    modelDirectory,
    "JobRanker.zip");

Directory.CreateDirectory(dataDirectory);
Directory.CreateDirectory(modelDirectory);

string connectionString =
    "Server=.\\SQLEXPRESS;" +
    "Database=DWUMA;" +
    "Integrated Security=True;" +
    "TrustServerCertificate=True;" +
    "MultipleActiveResultSets=True";

Console.WriteLine("Exporting interaction data from SQL Server...");

var exporter = new InteractionDataExporter(
    connectionString);

int exportedRows = await exporter.ExportAsync(dataPath);

Console.WriteLine($"Exported {exportedRows} rows.");
Console.WriteLine($"CSV saved to: {dataPath}");

if (exportedRows < 2)
{
    Console.WriteLine(
        "Not enough interaction rows to train the model.");

    return;
}

var mlContext = new MLContext(seed: 42);

if (!File.Exists(dataPath))
{
    throw new FileNotFoundException(
        $"Training dataset was not found at: {dataPath}");
}

var data = mlContext.Data
    .LoadFromTextFile<JobInteractionTrainingRow>(
        path: dataPath,
        hasHeader: true,
        separatorChar: ',');

long rowCount = data.GetRowCount() ?? 0;

Console.WriteLine($"Training rows loaded: {rowCount}");

if (rowCount < 10)
{
    Console.WriteLine(
        "At least 10 rows are recommended for a basic test.");

    return;
}

var split = mlContext.Data.TrainTestSplit(
    data,
    testFraction: 0.20,
    seed: 42);

var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(
        outputColumnName: "UserEncoded",
        inputColumnName: nameof(JobInteractionTrainingRow.UserId))
    .Append(mlContext.Transforms.Categorical.OneHotEncoding(
        outputColumnName: "JobEncoded",
        inputColumnName: nameof(JobInteractionTrainingRow.JobId)))
    .Append(mlContext.Transforms.Concatenate(
        "Features",
        "UserEncoded",
        "JobEncoded",
        nameof(JobInteractionTrainingRow.SkillMatch),
        nameof(JobInteractionTrainingRow.LocationMatch),
        nameof(JobInteractionTrainingRow.ExperienceMatch),
        nameof(JobInteractionTrainingRow.IndustryMatch),
        nameof(JobInteractionTrainingRow.SalaryMatch),
        nameof(JobInteractionTrainingRow.JobAgeDays),
        nameof(JobInteractionTrainingRow.Clicked),
        nameof(JobInteractionTrainingRow.Saved),
        nameof(JobInteractionTrainingRow.Applied)))
    .Append(mlContext.Regression.Trainers.LightGbm(
        labelColumnName: "Label",
        featureColumnName: "Features"));

Console.WriteLine("Training job-ranking model...");

var model = pipeline.Fit(split.TrainSet);

var predictions = model.Transform(split.TestSet);

var metrics = mlContext.Regression.Evaluate(
    predictions,
    labelColumnName: "Label",
    scoreColumnName: "Score");

Console.WriteLine($"R²:   {metrics.RSquared:F4}");
Console.WriteLine($"RMSE: {metrics.RootMeanSquaredError:F4}");
Console.WriteLine($"MAE:  {metrics.MeanAbsoluteError:F4}");

mlContext.Model.Save(
    model,
    split.TrainSet.Schema,
    modelPath);

Console.WriteLine($"Saved model to: {modelPath}");
Console.WriteLine($"Model exists: {File.Exists(modelPath)}");

if (File.Exists(modelPath))
{
    Console.WriteLine(
        $"Model size: {new FileInfo(modelPath).Length} bytes");
}