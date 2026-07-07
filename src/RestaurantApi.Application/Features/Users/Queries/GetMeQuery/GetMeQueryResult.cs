namespace RestaurantApi.Application.Features.Users.Queries.GetMeQuery;

public record GetMeQueryResult(Guid Id, string Name, string Surname, string Email);