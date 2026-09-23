using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
namespace OAS.Infrastructure.Accounting.Persistence.Configurations;
public sealed class CustomerConfiguration:IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("tbl_Customers","dbo"); b.HasKey(x=>x.Id); b.ConfigureAccountingAudit(); b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.CustomerCode).HasMaxLength(32).IsRequired(); b.Property(x=>x.AccountId).IsRequired(); b.Property(x=>x.EntityType).HasConversion<byte>().IsRequired();
        b.Property(x=>x.NameAr).HasMaxLength(150).IsRequired(); b.Property(x=>x.NameEn).HasMaxLength(150); b.Property(x=>x.TradeName).HasMaxLength(150);
        b.Property(x=>x.NationalId).HasMaxLength(50); b.Property(x=>x.CommercialRegistrationNo).HasMaxLength(50); b.Property(x=>x.TaxNumber).HasMaxLength(50);
        b.Property(x=>x.DateOfBirth).HasColumnType("date"); b.Property(x=>x.Gender).HasConversion<byte>().IsRequired();
        b.Property(x=>x.CreditLimit).HasColumnType("decimal(19,4)").IsRequired(); b.Property(x=>x.PaymentTermDays).IsRequired(); b.Property(x=>x.CustomerSince).HasColumnType("date");
        b.Property(x=>x.IsCreditAllowed).IsRequired(); b.Property(x=>x.IsActive).IsRequired(); b.Property(x=>x.Notes).HasMaxLength(1000); b.Property(x=>x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.OwnsOne(x=>x.ContactInfo,c=>{
            c.Property(x=>x.ContactPersonName).HasColumnName("ContactPersonName").HasMaxLength(150); c.Property(x=>x.ContactPersonTitle).HasColumnName("ContactPersonTitle").HasMaxLength(100);
            c.Property(x=>x.Phone).HasColumnName("Phone").HasMaxLength(32); c.Property(x=>x.Mobile).HasColumnName("Mobile").HasMaxLength(32); c.HasIndex(x=>x.Mobile).HasDatabaseName("IX_Customers_Mobile"); c.Property(x=>x.AlternatePhone).HasColumnName("AlternatePhone").HasMaxLength(32); c.Property(x=>x.WhatsAppNumber).HasColumnName("WhatsAppNumber").HasMaxLength(32);
            c.Property(x=>x.Email).HasColumnName("Email").HasMaxLength(256); c.Property(x=>x.Website).HasColumnName("Website").HasMaxLength(300); c.Property(x=>x.PreferredContactMethod).HasColumnName("PreferredContactMethod").HasConversion<byte>().IsRequired();
            c.OwnsOne(x=>x.Address,a=>{a.Property(x=>x.Country).HasColumnName("Country").HasMaxLength(100);a.Property(x=>x.Governorate).HasColumnName("Governorate").HasMaxLength(100);a.Property(x=>x.City).HasColumnName("City").HasMaxLength(100);a.Property(x=>x.District).HasColumnName("District").HasMaxLength(100);a.Property(x=>x.Street).HasColumnName("Street").HasMaxLength(150);a.Property(x=>x.Building).HasColumnName("Building").HasMaxLength(100);a.Property(x=>x.PostalCode).HasColumnName("PostalCode").HasMaxLength(24);a.Property(x=>x.AddressDetails).HasColumnName("AddressDetails").HasMaxLength(300);});
        });
        b.HasIndex(x=>x.CustomerCode).IsUnique().HasDatabaseName("UX_Customers_CustomerCode"); b.HasIndex(x=>x.AccountId).IsUnique().HasDatabaseName("UX_Customers_AccountId");
        b.HasIndex(x=>x.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL").HasDatabaseName("UX_Customers_NationalId"); b.HasIndex(x=>x.TaxNumber).IsUnique().HasFilter("[TaxNumber] IS NOT NULL").HasDatabaseName("UX_Customers_TaxNumber"); b.HasIndex(x=>x.CommercialRegistrationNo).IsUnique().HasFilter("[CommercialRegistrationNo] IS NOT NULL").HasDatabaseName("UX_Customers_CommercialRegistrationNo");
        b.HasIndex(x=>x.NameAr).HasDatabaseName("IX_Customers_NameAr"); b.HasIndex(x=>x.IsActive).HasDatabaseName("IX_Customers_IsActive"); b.HasIndex(x=>x.EntityType).HasDatabaseName("IX_Customers_EntityType");
        b.HasOne<Account>().WithMany().HasForeignKey(x=>x.AccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Customers_Account");
    }
}
