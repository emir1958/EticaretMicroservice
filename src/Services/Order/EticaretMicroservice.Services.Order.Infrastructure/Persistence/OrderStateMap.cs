using EticaretMicroservice.Services.Order.Application.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EticaretMicroservice.Services.Order.Infrastructure.Persistence;

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("OrderStates");

        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.BuyerId).HasMaxLength(450);
        entity.Property(x => x.RowVersion).IsRowVersion();

        // Bir siparişin tek bir süreci olabilir
        entity.HasIndex(x => x.OrderId).IsUnique();

        // Emniyet kontrolü sorgusu: "ödeme bekleyen ve süresi geçmiş instance'lar"
        entity.HasIndex(x => new { x.CurrentState, x.ExpiresAt });
    }
}
