using NUnit.Framework;

public class BeltslipLogicTests
{
    class FakeScoreStorage : IScoreStorage
    {
        public int Value;
        public int SaveCount;

        public FakeScoreStorage(int initial = 0) => Value = initial;
        public int GetInt(string key, int defaultValue) => Value;
        public void SetInt(string key, int value) => Value = value;
        public void Save() => SaveCount++;
    }

    [Test]
    public void GetGap_AtZeroAndOneDifficulty_ReturnsStartAndMin()
    {
        Assert.AreEqual(4.5f, GateSpawnLogic.GetGap(4.5f, 2.8f, 0f), 0.0001f);
        Assert.AreEqual(2.8f, GateSpawnLogic.GetGap(4.5f, 2.8f, 1f), 0.0001f);
    }

    [Test]
    public void GetNextGapCenter_WithExtremeOffsets_NeverExceedsFieldBounds()
    {
        const float bottom = -6.5f;
        const float top = 6.5f;
        const float margin = 1f;
        const float gap = 2.8f;
        float half = gap * 0.5f;

        float[] offsets = { -10f, -1f, 0f, 1f, 10f };
        foreach (float offset in offsets)
        {
            float center = GateSpawnLogic.GetNextGapCenter(
                lastCenter: 0f,
                randomOffset01: offset,
                startMaxShift: 2f,
                endMaxShift: 3.5f,
                difficulty01: 1f,
                gap: gap,
                fieldBottom: bottom,
                fieldTop: top,
                edgeMargin: margin);

            Assert.GreaterOrEqual(center - half, bottom + margin - 0.0001f);
            Assert.LessOrEqual(center + half, top - margin + 0.0001f);
        }
    }

    [Test]
    public void HighScore_TrySet_DoesNotOverwriteWithLowerOrEqualScore()
    {
        var storage = new FakeScoreStorage(initial: 10);
        var highScore = new HighScore(storage);

        Assert.IsFalse(highScore.TrySet(5));
        Assert.IsFalse(highScore.TrySet(10));
        Assert.AreEqual(10, highScore.Best);
        Assert.AreEqual(0, storage.SaveCount);

        Assert.IsTrue(highScore.TrySet(15));
        Assert.AreEqual(15, highScore.Best);
        Assert.AreEqual(1, storage.SaveCount);
    }
}
