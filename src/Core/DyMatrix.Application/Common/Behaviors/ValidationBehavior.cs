// Look at: https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Application/Common/Behaviours/ValidationBehaviour.cs

using DyMatrix.Application.Common.Exceptions;

namespace DyMatrix.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) 
            return await next(cancellationToken);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v =>
                v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures is { Count: > 0 })
            throw new AppValidationException(failures);

        return await next(cancellationToken);
    }
}