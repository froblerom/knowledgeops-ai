using KnowledgeOps.Application.Retrieval;

namespace KnowledgeOps.Application.Tests.Retrieval;

public sealed class RetrievalSettingsTests
{
    [Fact]
    public void RetrievalSettings_UsesSafeDefaults()
    {
        var settings = new RetrievalSettings();

        Assert.Equal(5, settings.NormalizeTopK(0));
        Assert.Equal(5, settings.NormalizeTopK(-1));
        Assert.Equal(20, settings.NormalizeTopK(100));
    }

    [Fact]
    public void RetrievalSettings_ClampsTopK()
    {
        var settings = new RetrievalSettings
        {
            DefaultTopK = 3,
            MaxTopK = 7
        };

        Assert.Equal(3, settings.NormalizeTopK(0));
        Assert.Equal(7, settings.NormalizeTopK(99));
        Assert.Equal(4, settings.NormalizeTopK(4));
    }

    [Fact]
    public void RetrievalSettings_DefaultTopKCannotExceedMaxTopK()
    {
        var settings = new RetrievalSettings
        {
            DefaultTopK = 50,
            MaxTopK = 10
        };

        Assert.Equal(10, settings.NormalizeTopK(0));
    }
}
