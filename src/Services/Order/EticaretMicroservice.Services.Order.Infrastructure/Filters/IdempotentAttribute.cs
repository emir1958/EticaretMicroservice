using EticaretMicroservice.Services.Order.Domain.Entities;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EticaretMicroservice.Services.Order.Infrastructure.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    // Kaydedilen cevap, MVC'nin ilk cevabı yazdığı biçimle (camelCase) aynı olmalı: istemci tekrar cevabını da aynı okur
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Header'da Idempotency-Key var mı?
        if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var rawKey) ||
            !Guid.TryParse(rawKey.FirstOrDefault(), out var idempotencyKey))
        {
            // Header yoksa akış normal devam eder (opsiyonel) veya hata dönülebilir
            await next();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<OrderDbContext>();

        // 2. Anahtar tabloda aranır
        var existingRequest = await dbContext.IdempotentRequests
            .FirstOrDefaultAsync(x => x.Id == idempotencyKey);

        if (existingRequest != null)
        {
            // A. İşlem devam ediyorsa ikinci tıklama engellenir
            if (!existingRequest.IsCompleted)
            {
                context.Result = new ConflictObjectResult(new
                {
                    message = "Bu sipariş işlemi şu anda yürütülüyor. Lütfen bekleyin."
                });
                return;
            }

            // B. İşlem daha önce başarıyla tamamlandıysa, ilk cevap doğrudan dönülür (Mükerrer sipariş oluşmaz)
            context.Result = new ContentResult
            {
                StatusCode = existingRequest.StatusCode,
                ContentType = "application/json",
                Content = existingRequest.ResponseBody
            };
            return;
        }

        // 3. İlk defa gelen anahtar "IsCompleted = false" olarak rezerve edilir
        var actionName = context.ActionDescriptor.RouteValues.TryGetValue("action", out var act)
    ? $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
    : "CreateOrder";

        var idempotentRecord = new IdempotentRequest
        {
            Id = idempotencyKey,
            OperationName = actionName,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        dbContext.IdempotentRequests.Add(idempotentRecord);
        await dbContext.SaveChangesAsync();

        // 4. Controller metodu çalıştırılır
        var executedContext = await next();

        // 5. Yalnızca başarılı (2xx) cevap kaydedilir: sonraki tekrarlar ilk cevabı aynen alır
        if (executedContext.Exception is null
            && executedContext.Result is ObjectResult { StatusCode: null or (>= 200 and < 300) } objectResult)
        {
            idempotentRecord.StatusCode = objectResult.StatusCode ?? 200;
            idempotentRecord.ResponseBody = JsonSerializer.Serialize(objectResult.Value, ResponseJsonOptions);
            idempotentRecord.IsCompleted = true;

            await dbContext.SaveChangesAsync();
            return;
        }

        // Başarısız istek anahtarı tüketmemeli: aksi halde istemci aynı anahtarla tekrar denediğinde
        // "işlem yürütülüyor" 409'una takılır. ExecuteDelete change tracker'ı kullanmaz; hata sonrası
        // context'te yarım kalmış Order kayıtları olabilir ve SaveChanges onları yeniden yazmaya çalışırdı.
        dbContext.Entry(idempotentRecord).State = EntityState.Detached;
        await dbContext.IdempotentRequests.Where(x => x.Id == idempotencyKey).ExecuteDeleteAsync();
    }
}