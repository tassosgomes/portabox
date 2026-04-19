using FluentValidation.TestHelper;
using PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class UploadOptInDocumentCommandValidatorTests
{
    private readonly UploadOptInDocumentCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldRejectUnsupportedContentType()
    {
        var command = BuildCommand() with { ContentType = "text/plain" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(current => current.ContentType);
    }

    [Fact]
    public void Validate_ShouldRejectFilesLargerThanTenMegabytes()
    {
        var command = BuildCommand() with { SizeBytes = UploadOptInDocumentCommandValidator.MaxFileSizeBytes + 1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(current => current.SizeBytes);
    }

    private static UploadOptInDocumentCommand BuildCommand()
    {
        return new UploadOptInDocumentCommand(
            Guid.NewGuid(),
            OptInDocumentKind.Ata,
            "application/pdf",
            "ata.pdf",
            1024,
            new MemoryStream([1, 2, 3, 4]),
            Guid.NewGuid());
    }
}
