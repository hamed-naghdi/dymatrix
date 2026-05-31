// Look at: https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Application/Common/Exceptions/ValidationException.cs

using FluentValidation.Results;

namespace DyMatrix.Application.Common.Exceptions;

public class AppValidationException : Exception
{
    public AppValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}