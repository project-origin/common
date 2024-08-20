namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

internal interface IOptionsValidator<TOption> where TOption : class
{
    public void Validate(TOption options);
}
