using System;
using System.Collections.Generic;
using System.Linq;

namespace Pos.Domain.Entities;

public static class Permissions
{
    public const string SalesCreate = "sales.create";
    public const string SalesDiscountOverride = "sales.discount.override";
    public const string PriceEdit = "price.edit";
    public const string StockAdjust = "stock.adjust";
    public const string TransferApprove = "transfer.approve";
    public const string UsersManage = "users.manage";
    public const string ReportsView = "reports.view";
    public const string ShiftClose = "shift.close";
}

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxDiscountPercent { get; set; }
    
    private readonly HashSet<RolePermission> _permissions = new();
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
}

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    
    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly HashSet<UserBranch> _assignedBranches = new();
    public IReadOnlyCollection<UserBranch> AssignedBranches => _assignedBranches;
}

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
}
