using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Sales.Services;

public sealed class LensVariantResolver(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<LensVariantDetail, Guid> details) : ILensVariantResolver
{
    public async Task<LensVariantResolution> ResolveAsync(
        LensVariantMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SeedProductVariantId == Guid.Empty)
            throw new ConflictException("sales_lens_variant_seed_required", "يجب تحديد عدسة/متغير منتج مرجعي لتحديد المقاس المخزني الصحيح.");

        var seed = await variants.GetByIdAsync(request.SeedProductVariantId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.SeedProductVariantId);

        var candidateVariants = await variants.ListAsync(
            new Specification<ProductVariant>().Where(x => x.ProductId == seed.ProductId && x.IsActive),
            cancellationToken);

        var candidateIds = candidateVariants.Select(x => x.Id).ToArray();
        if (candidateIds.Length == 0)
            throw new ConflictException("sales_lens_variant_not_found", "لا يوجد متغير عدسة فعال للمنتج المحدد.");

        var matchingDetails = await details.ListAsync(
            new Specification<LensVariantDetail>().Where(x =>
                candidateIds.Contains(x.ProductVariantId) &&
                x.IsActive &&
                x.SPH == request.SPH &&
                x.CYL == request.CYL &&
                x.ADD == request.ADD),
            cancellationToken);

        if (matchingDetails.Count == 0)
            throw new ConflictException(
                "sales_lens_variant_not_found",
                "لا يوجد SKU عدسة مخزني مطابق تمامًا لقيم SPH/CYL/ADD المطلوبة.");

        if (matchingDetails.Count > 1)
            throw new ConflictException(
                "sales_lens_variant_ambiguous",
                "يوجد أكثر من SKU عدسة مطابق للقياسات نفسها. يجب تصحيح بيانات LensVariantDetail.");

        var match = matchingDetails[0];
        return new LensVariantResolution(match.ProductVariantId, match.SPH, match.CYL, match.ADD);
    }
}
