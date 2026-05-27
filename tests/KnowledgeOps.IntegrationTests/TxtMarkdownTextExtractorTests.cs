using System.Text;
using KnowledgeOps.Application.Documents;
using KnowledgeOps.Infrastructure.Documents;

namespace KnowledgeOps.IntegrationTests;

public sealed class TxtMarkdownTextExtractorTests
{
    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/markdown")]
    [InlineData("text/plain; charset=utf-8")]
    [InlineData("TEXT/PLAIN")]
    public void Supports_KnownContentTypes_ReturnsTrue(string contentType)
    {
        var extractor = new TxtMarkdownTextExtractor();

        Assert.True(extractor.Supports(contentType));
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("image/png")]
    public void Supports_UnknownContentTypes_ReturnsFalse(string contentType)
    {
        var extractor = new TxtMarkdownTextExtractor();

        Assert.False(extractor.Supports(contentType));
    }

    [Fact]
    public async Task ExtractAsync_ReturnsStreamContent()
    {
        var extractor = new TxtMarkdownTextExtractor();
        var content = "Hello, world!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await extractor.ExtractAsync(stream);

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ExtractAsync_NormalizesWindowsLineEndings()
    {
        var extractor = new TxtMarkdownTextExtractor();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("line1\r\nline2\r\nline3"));

        var result = await extractor.ExtractAsync(stream);

        Assert.Equal("line1\nline2\nline3", result);
        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public async Task ExtractAsync_NormalizesOldMacLineEndings()
    {
        var extractor = new TxtMarkdownTextExtractor();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("line1\rline2\rline3"));

        var result = await extractor.ExtractAsync(stream);

        Assert.Equal("line1\nline2\nline3", result);
        Assert.DoesNotContain("\r", result);
    }
}
