using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
namespace OAS.Domain.Sales.Entities;
public sealed class OpticalProductionMaterial : Entity<Guid>
{
 private OpticalProductionMaterial(){}
 private OpticalProductionMaterial(Guid id,Guid jobId,Guid productVariantId,decimal quantity,string? notes){if(id==Guid.Empty||jobId==Guid.Empty||productVariantId==Guid.Empty)throw new DomainException("Production material identifiers are required.");if(quantity<=0)throw new DomainException("Production material quantity must be greater than zero.");Id=id;OpticalProductionJobId=jobId;ProductVariantId=productVariantId;Quantity=decimal.Round(quantity,3);Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim();}
 public Guid OpticalProductionJobId{get;private set;} public Guid ProductVariantId{get;private set;} public decimal Quantity{get;private set;} public decimal? UnitCostSnapshot{get;private set;} public decimal? TotalCostSnapshot{get;private set;} public string? Notes{get;private set;}
 public static OpticalProductionMaterial Create(Guid id,Guid jobId,Guid variantId,decimal qty,string? notes=null)=>new(id,jobId,variantId,qty,notes);
 public void SetIssueCost(decimal unitCost){if(unitCost<0)throw new DomainException("Production material unit cost cannot be negative.");UnitCostSnapshot=decimal.Round(unitCost,4);TotalCostSnapshot=decimal.Round(Quantity*unitCost,4);}
}
