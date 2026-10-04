using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Documents;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.Documents;

public sealed record GetEmployeeDocumentsQuery(Guid EmployeeId) : IQuery<IReadOnlyList<EmployeeDocumentDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DocumentsView]; }
public sealed record GetEmployeeDocumentFileQuery(Guid DocumentId) : IQuery<EmployeeDocumentDownload?>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DocumentsView]; }
public sealed record EmployeeDocumentDownload(string FileName, string ContentType, byte[] Content);
public sealed record UploadEmployeeDocumentCommand(Guid EmployeeId, byte DocumentType, string Title, DateOnly? IssueDate, DateOnly? ExpiryDate, string? Notes, string OriginalFileName, string ContentType, byte[] Content) : INonTransactionalCommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DocumentsManage]; }
public sealed record UpdateEmployeeDocumentCommand(Guid DocumentId, UpdateEmployeeDocumentRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DocumentsManage]; }
public sealed record SetEmployeeDocumentStatusCommand(Guid DocumentId, SetEmployeeDocumentStatusRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DocumentsManage]; }

public sealed class UploadEmployeeDocumentValidator : AbstractValidator<UploadEmployeeDocumentCommand>
{
    public UploadEmployeeDocumentValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DocumentType).Must(v => Enum.IsDefined(typeof(EmployeeDocumentType), v));
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).Must(v => v is "application/pdf" or "image/jpeg" or "image/png").WithErrorCode("employee_document_type_not_allowed");
        RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 10 * 1024 * 1024).WithErrorCode("employee_document_file_too_large");
    }
}
public sealed class UpdateEmployeeDocumentValidator : AbstractValidator<UpdateEmployeeDocumentCommand>
{
    public UpdateEmployeeDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Request.DocumentType).Must(v => Enum.IsDefined(typeof(EmployeeDocumentType), v));
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }
    private static bool BeBase64(string value) { try { Convert.FromBase64String(value); return true; } catch { return false; } }
}

public sealed class SetEmployeeDocumentStatusValidator : AbstractValidator<SetEmployeeDocumentStatusCommand>
{
    public SetEmployeeDocumentStatusValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

internal static class EmployeeDocumentMapping
{
    public static EmployeeDocumentDto ToDto(EmployeeDocument x) => new(x.Id, x.DocumentCode, x.EmployeeId, (byte)x.DocumentType, x.Title, x.OriginalFileName, x.ContentType, x.FileSize, x.Sha256Hash, x.IssueDate, x.ExpiryDate, x.IsActive, x.Notes, Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc);
}
public sealed class GetEmployeeDocumentsQueryHandler(IReadRepository<EmployeeDocument, Guid> repository) : IRequestHandler<GetEmployeeDocumentsQuery, IReadOnlyList<EmployeeDocumentDto>>
{
    public async Task<IReadOnlyList<EmployeeDocumentDto>> Handle(GetEmployeeDocumentsQuery request, CancellationToken ct) => (await repository.ListAsync(new Specification<EmployeeDocument>().Where(x => x.EmployeeId == request.EmployeeId), ct)).OrderByDescending(x => x.CreatedAtUtc).Select(EmployeeDocumentMapping.ToDto).ToArray();
}
public sealed class GetEmployeeDocumentFileQueryHandler(IReadRepository<EmployeeDocument, Guid> repository, IEmployeeDocumentStore store) : IRequestHandler<GetEmployeeDocumentFileQuery, EmployeeDocumentDownload?>
{
    public async Task<EmployeeDocumentDownload?> Handle(GetEmployeeDocumentFileQuery request, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(request.DocumentId, ct) ?? throw new NotFoundException(nameof(EmployeeDocument), request.DocumentId);
        var file = await store.GetAsync(document.StorageKey, ct);
        return file is null ? null : new EmployeeDocumentDownload(document.OriginalFileName, file.ContentType, file.Content);
    }
}
public sealed class UploadEmployeeDocumentCommandHandler(IRepository<EmployeeDocument, Guid> repository, IReadRepository<Employee, Guid> employees, IEmployeeDocumentStore store, ISequenceNumberGenerator sequences, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<UploadEmployeeDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadEmployeeDocumentCommand request, CancellationToken ct)
    {
        _ = await employees.GetByIdAsync(request.EmployeeId, ct) ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);
        var number = await sequences.NextAsync("EmployeeDocumentCodeSequence", ct);
        var code = $"EDOC-{timeProvider.GetUtcNow().Year:0000}-{number:000000}";
        string? storageKey = null;
        try
        {
            storageKey = await store.SaveAsync(request.EmployeeId, request.OriginalFileName, request.ContentType, request.Content, ct);
            var hash = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
            var document = EmployeeDocument.Create(Guid.NewGuid(), code, request.EmployeeId, (EmployeeDocumentType)request.DocumentType, request.Title, storageKey, Path.GetFileName(request.OriginalFileName), request.ContentType, request.Content.LongLength, hash, request.IssueDate, request.ExpiryDate, request.Notes);
            await repository.AddAsync(document, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return document.Id;
        }
        catch
        {
            if (storageKey is not null) await store.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }
}
public sealed class UpdateEmployeeDocumentCommandHandler(IRepository<EmployeeDocument, Guid> repository) : IRequestHandler<UpdateEmployeeDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeDocumentCommand request, CancellationToken ct)
    {
        var item = await repository.GetForUpdateAsync(request.DocumentId, ct) ?? throw new NotFoundException(nameof(EmployeeDocument), request.DocumentId);
        if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(item.RowVersion)) throw new ConcurrencyException("The employee document was changed by another operation. Reload it and try again.");
        item.UpdateMetadata((EmployeeDocumentType)request.Request.DocumentType, request.Request.Title, request.Request.IssueDate, request.Request.ExpiryDate, request.Request.Notes);
        repository.Update(item);
        return item.Id;
    }
}
public sealed class SetEmployeeDocumentStatusCommandHandler(IRepository<EmployeeDocument, Guid> repository) : IRequestHandler<SetEmployeeDocumentStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetEmployeeDocumentStatusCommand request, CancellationToken ct)
    {
        var item = await repository.GetForUpdateAsync(request.DocumentId, ct) ?? throw new NotFoundException(nameof(EmployeeDocument), request.DocumentId);
        if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(item.RowVersion)) throw new ConcurrencyException("The employee document was changed by another operation. Reload it and try again.");
        item.SetActive(request.Request.IsActive);
        repository.Update(item);
        return item.Id;
    }
}
