namespace DyMatrix.Application.Common.Interfaces;

public interface INotificationForwarder
{
    Task ForwardAsync(string message, CancellationToken cancellationToken = default);
}