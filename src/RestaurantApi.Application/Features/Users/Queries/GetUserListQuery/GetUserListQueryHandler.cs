using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;

public class GetUserListQueryHandler
    : IRequestHandler<GetUserListQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>>
{
    private readonly ILogger<GetUserListQueryHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserListQueryHandler(ILogger<GetUserListQueryHandler> logger, IUserRepository userRepository,
        IMapper mapper)
    {
        _logger = logger;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>> Handle(
        GetUserListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Panel kullanıcı listeleme işlemi başlıyor.");

        List<GetUserListQueryResult> users = await _userRepository.GetAllAsQueryable()
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Surname)
            .ProjectTo<GetUserListQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, List<string>> roleNames = await _userRepository
            .GetRoleNamesByUserIdsAsync(users.Select(user => user.Id).ToList(), cancellationToken);

        foreach (GetUserListQueryResult user in users)
        {
            user.Roles = roleNames.GetValueOrDefault(user.Id) ?? new List<string>();
        }

        _logger.LogInformation("Panel kullanıcı listesi hazırlandı. UserCount:{UserCount}", users.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>(data: users);
    }
}
