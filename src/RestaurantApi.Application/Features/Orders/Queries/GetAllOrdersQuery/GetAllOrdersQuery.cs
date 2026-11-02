using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;

// Panel akışı: tüm siparişleri (kullanıcı + masa) filtreleyerek listeleme sorgusu.
// Filtreler route query string'inden gelir ve hepsi opsiyoneldir; hiçbiri gönderilmezse tüm siparişler döner.
// Değerler bilinçli olarak string taşınır: geçersiz bir değer model binding hatası (400) değil,
// boş liste ile karşılanır (handler filtreleri kendisi çözümler).
public record GetAllOrdersQuery: IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>>
{
    public const string TypeTable = "table";
    public const string TypeUsers = "users";

    // Sipariş sahibi tipi filtresi.
    public string? Type { get; init; }

    // OrderStatus enum adı (ör. "Delivered"); geçersiz değer boş liste ile karşılanır.
    public string? OrderStatus { get; init; }

    // PaymentStatus enum adı (ör. "Paid"); geçersiz değer boş liste ile karşılanır.
    public string? PaymentStatus { get; init; }
}
