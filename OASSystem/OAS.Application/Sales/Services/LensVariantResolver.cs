using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Sales.Services;

public sealed class LensVariantResolver(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<LensDetails, Guid> details) : ILensVariantResolver
{
    public async Task<LensVariantResolution> ResolveAsync(
        LensVariantMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var exact = await TryResolveExactAsync(request, cancellationToken);
        if (exact is null)
            throw new ConflictException(
                "sales_lens_variant_not_defined",
                "لا يوجد SKU عدسة معرف يطابق قيم SPH/CYL/ADD المطلوبة. أنشئ قياس العدسة في المخزون أولاً حتى يمكن بيعه أو طلب توريده.");
        return exact;
    }

    public async Task<LensVariantResolution?> TryResolveExactAsync(
        LensVariantMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SeedProductVariantId == Guid.Empty)
            throw new ConflictException("sales_lens_variant_seed_required", "يجب تحديد عدسة/متغير منتج مرجعي لتحديد القياس الصحيح.");

        var seed = await variants.GetByIdAsync(request.SeedProductVariantId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.SeedProductVariantId);

        var candidateVariants = await variants.ListAsync(
            new Specification<ProductVariant>().Where(x => x.ProductId == seed.ProductId && x.IsActive),
            cancellationToken);
        var candidateIds = candidateVariants.Select(x => x.Id).ToArray();
        if (candidateIds.Length == 0)
            return null;

        var matchingDetails = await details.ListAsync(
            new Specification<LensDetails>().Where(x =>
                candidateIds.Contains(x.ProductVariantId) &&
                x.IsActive &&
                x.SPH == request.SPH &&
                x.CYL == request.CYL &&
                x.ADD == request.ADD),
            cancellationToken);

        if (matchingDetails.Count == 0)
            return null;
        if (matchingDetails.Count > 1)
            throw new ConflictException(
                "sales_lens_variant_ambiguous",
                "يوجد أكثر من SKU عدسة بنفس قيم SPH/CYL/ADD للمنتج نفسه. يجب تصحيح بيانات العدسات.");

        var match = matchingDetails[0];
        return new LensVariantResolution(match.ProductVariantId, match.SPH, match.CYL, match.ADD);
    }
}
