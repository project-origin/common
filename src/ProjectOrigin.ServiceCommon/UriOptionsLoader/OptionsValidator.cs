
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Options;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

internal class OptionsValidator<TOption> : IOptionsValidator<TOption> where TOption : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IValidateOptions<TOption>> _validators;

    public OptionsValidator(IServiceProvider serviceProvider, IEnumerable<IValidateOptions<TOption>> validators)
    {
        _serviceProvider = serviceProvider;
        _validators = validators;
    }

    public void Validate(TOption options)
    {
        ValidateAttributes(options);
        ValidateIValidateOptions(options);
    }

    private void ValidateAttributes(TOption options)
    {
        var context = new ValidationContext(options, _serviceProvider, null);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            throw new OptionsValidationException(typeof(TOption).Name, typeof(TOption), results.Select(r => r.ErrorMessage!));
        }
    }

    private void ValidateIValidateOptions(TOption options)
    {
        if (options is IValidateOptions<TOption> validator)
        {
            var validationResult = validator.Validate(typeof(TOption).Name, options);
            if (validationResult.Failed)
            {
                throw new OptionsValidationException(typeof(TOption).Name, typeof(TOption), [validationResult.FailureMessage]);
            }
        }

        var failedValidations = _validators
            .Select(x => x.Validate(typeof(TOption).Name, options))
            .Where(x => x.Failed);

        if (failedValidations.Any())
            throw new OptionsValidationException(typeof(TOption).Name, typeof(TOption), failedValidations.Select(x => x.FailureMessage!));
    }

}
