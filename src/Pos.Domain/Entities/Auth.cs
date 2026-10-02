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

public class Role
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal MaxDiscountPercent { get; }
    
    private readonly HashSet<string> _permissions;
    public IReadOnlySet<string> Permissions => _permissions;

    public Role(Guid id, string name, decimal maxDiscountPercent, IEnumerable<string> permissions)
    {
        Id = id;
        Name = name;
        MaxDiscountPercent = maxDiscountPercent;
        _permissions = new HashSet<string>(permissions);
    }
}

public class User
{
    public Guid Id { get; }
    public string Username { get; }
    
    private readonly List<Role> _roles;
    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    private readonly HashSet<Guid> _assignedBranchIds;
    public IReadOnlySet<Guid> AssignedBranchIds => _assignedBranchIds;

    public User(Guid id, string username, IEnumerable<Role> roles, IEnumerable<Guid> assignedBranchIds)
    {
        Id = id;
        Username = username;
        _roles = roles.ToList();
        _assignedBranchIds = new HashSet<Guid>(assignedBranchIds);
    }

    public bool HasPermission(string permissionKey)
    {
        return _roles.Any(r => r.Permissions.Contains(permissionKey));
    }

    public bool CanAccessBranch(Guid branchId)
    {
        return _assignedBranchIds.Contains(branchId);
    }

    public decimal GetMaxDiscountPercent()
    {
        if (_roles.Count == 0) return 0m;
        return _roles.Max(r => r.MaxDiscountPercent);
    }
}
