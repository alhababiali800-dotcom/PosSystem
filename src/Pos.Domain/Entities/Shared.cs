namespace Pos.Domain.Entities;

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    QR,
    Credit
}

public enum SaleState
{
    Draft,
    Completed
}

public record TaxRate(string Name, decimal Rate, bool IsInclusive);
