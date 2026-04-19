using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class OptInDocumentTests
{
    [Fact]
    public void Create_ShouldNormalizeAndPersistDocumentMetadata()
    {
        var uploadedAt = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(uploadedAt);

        var document = OptInDocument.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OptInDocumentKind.Ata,
            " condominios/test/opt-in/doc.pdf ",
            " application/pdf ",
            1024,
            new string('A', 64),
            Guid.NewGuid(),
            clock);

        Assert.Equal("condominios/test/opt-in/doc.pdf", document.StorageKey);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(new string('a', 64), document.Sha256);
        Assert.Equal(uploadedAt, document.UploadedAt);
    }

    [Fact]
    public void Create_ShouldRejectNonPositiveSize()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => OptInDocument.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OptInDocumentKind.Termo,
            "condominios/test/opt-in/doc.pdf",
            "application/pdf",
            0,
            new string('b', 64),
            Guid.NewGuid(),
            TimeProvider.System));

        Assert.Equal("sizeBytes", exception.ParamName);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
