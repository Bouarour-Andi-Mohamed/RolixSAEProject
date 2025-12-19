namespace RolixSAEProject.Helpers;

public static class CurrencyHelper
{
    public static string Normalize(string? currency)
    {
        currency = (currency ?? "EUR").ToUpperInvariant();
        return currency is "EUR" or "CHF" or "USD" ? currency : "EUR";
    }

    public static string Symbol(string currency) => currency switch
    {
        "CHF" => "CHF",
        "USD" => "$",
        _ => "€"
    };
}
