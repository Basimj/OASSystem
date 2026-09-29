namespace OAS.Application.Sales.Common;

internal static class SalesEnumMap
{
    public static TTarget To<TTarget>(Enum value) where TTarget : struct, Enum =>
        (TTarget)Enum.ToObject(typeof(TTarget), Convert.ToByte(value));
}
