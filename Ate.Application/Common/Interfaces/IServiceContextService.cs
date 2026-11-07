using Ate.Application.Features.ServiceContext.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IServiceContextService
{
    Result<ServiceContextDto> GetCurrent();
}
