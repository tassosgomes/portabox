using FluentValidation;

namespace PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;

public sealed class UploadOptInDocumentCommandValidator : AbstractValidator<UploadOptInDocumentCommand>
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public UploadOptInDocumentCommandValidator()
    {
        RuleFor(command => command.TenantId)
            .NotEmpty();

        RuleFor(command => command.Kind)
            .IsInEnum();

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(IsSupportedContentType)
            .WithMessage("Content type must be one of: application/pdf, image/jpeg, image/png.");

        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes);

        RuleFor(command => command.Stream)
            .NotNull();

        RuleFor(command => command.UploadedByUserId)
            .NotEmpty();
    }

    private static bool IsSupportedContentType(string? contentType)
    {
        return contentType is "application/pdf" or "image/jpeg" or "image/png";
    }
}
