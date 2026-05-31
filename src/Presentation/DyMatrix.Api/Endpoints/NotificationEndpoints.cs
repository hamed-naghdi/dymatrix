using DyMatrix.Application.Notifications;

namespace DyMatrix.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications")
            .WithTags("Notifications");

        group.MapPost("/", HandleAsync)
            .WithName("ReceiveNotification")
            .WithSummary("Receive a notification and forward it to Discord if level is warning or above.")
            .Produces<NotificationResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] NotificationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(request, cancellationToken);
        return Results.Accepted(value: response);
    }
}