using DyMatrix.Domain.Entities;

namespace DyMatrix.Application.Common.Interfaces;

public interface ILlmService
{
    Task<string> GenerateMessageAsync(Notification notification, CancellationToken cancellationToken = default);
}