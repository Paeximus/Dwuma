namespace Dwuma.Services;

public sealed class InteractionRatingService
{
    public float Calculate(
        bool clicked,
        bool saved,
        bool applied,
        bool dismissed)
    {
        float rating = 0f;

        if (clicked)
            rating += 0.15f;

        if (saved)
            rating += 0.25f;

        if (applied)
            rating += 0.60f;

        if (dismissed)
            rating -= 0.40f;

        return Math.Clamp(rating, 0f, 1f);
    }
}