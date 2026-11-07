using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Restaurants;
using Ate.Application.Features.ServiceContext.DTOs;
using Ate.Domain.Common;

namespace Ate.Infrastructure.Services;

public class ServiceContextService : IServiceContextService, IScopedDependency
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public ServiceContextService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public Result<ServiceContextDto> GetCurrent()
    {
        var slot = ServiceDay.Resolve(_dateTimeProvider.BusinessNow);

        return Result.Success(new ServiceContextDto(
            DateTimeConstants.DefaultBusinessTimeZoneId,
            _dateTimeProvider.UtcNow,
            slot.ServiceDate.ToString(DateTimeConstants.StandardDateFormat),
            MealTypeCodes.ToCode(slot.MealType),
            slot.IsTomorrow));
    }
}
