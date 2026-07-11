using CareerAgent.Training;
using Dwuma.ML;
using Microsoft.Extensions.ML;

namespace Dwuma.Services;

public sealed class JobRankingService
{
    private readonly PredictionEnginePool<JobInteractionTrainingRow, JobPrediction> _pool;

    public JobRankingService(
        PredictionEnginePool<JobInteractionTrainingRow, JobPrediction> pool)
    {
        _pool = pool;
    }

    public float Predict(JobInteractionTrainingRow input)
    {
        JobPrediction prediction = _pool.Predict(
            modelName: "JobRanker",
            example: input);

        return Math.Clamp(prediction.Score, 0f, 1f);
    }
}