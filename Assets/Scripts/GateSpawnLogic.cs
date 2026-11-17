using UnityEngine;

// Чистая логика расчёта размеров и положения прохода в воротах (без зависимости от сцены).
public static class GateSpawnLogic
{
    public static float GetGap(float startGap, float minGap, float difficulty01) =>
        Mathf.Lerp(startGap, minGap, Mathf.Clamp01(difficulty01));

    public static float GetSpacing(float startSpacing, float minSpacing, float difficulty01) =>
        Mathf.Lerp(startSpacing, minSpacing, Mathf.Clamp01(difficulty01));

    public static float GetNextGapCenter(
        float lastCenter,
        float randomOffset01,
        float startMaxShift,
        float endMaxShift,
        float difficulty01,
        float gap,
        float fieldBottom,
        float fieldTop,
        float edgeMargin)
    {
        float d = Mathf.Clamp01(difficulty01);
        float shift = Mathf.Lerp(startMaxShift, endMaxShift, d);
        float half = gap * 0.5f;
        float minCenter = fieldBottom + edgeMargin + half;
        float maxCenter = fieldTop - edgeMargin - half;

        if (minCenter > maxCenter)
            return (fieldBottom + fieldTop) * 0.5f;

        return Mathf.Clamp(lastCenter + randomOffset01 * shift, minCenter, maxCenter);
    }
}
