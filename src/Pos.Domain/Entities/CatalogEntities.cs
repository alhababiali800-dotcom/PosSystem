using System;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Unit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
}

public class ProductBarcode : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
}

public class PriceList : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class ProductPrice : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid PriceListId { get; set; }
    public Money Price { get; set; }
}

public enum CustomerType { Retail, Wholesale }

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public Money CreditLimit { get; set; }
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Shift : BaseEntity
{
    public Guid DeviceId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Money OpeningCash { get; set; }
    public Money CountedCash { get; set; }
    public Money ExpectedCash { get; set; }
    public Money Difference { get; set; }
}

public class Expense : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public Money Amount { get; set; }
}
