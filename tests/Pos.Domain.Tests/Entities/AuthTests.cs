using System;
using Pos.Domain.Entities;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.Entities;

public class AuthTests
{
    [Fact]
    public void User_Role_Evaluation_Checks_Permissions_And_Discount()
    {
        var role1 = new Role(Guid.NewGuid(), "Cashier", 10m, new[] { Permissions.SalesCreate });
        var role2 = new Role(Guid.NewGuid(), "Manager", 50m, new[] { Permissions.SalesDiscountOverride });
        
        var branchId = Guid.NewGuid();
        var user = new User(Guid.NewGuid(), "alice", new[] { role1, role2 }, new[] { branchId });
        
        user.HasPermission(Permissions.SalesCreate).ShouldBeTrue();
        user.HasPermission(Permissions.SalesDiscountOverride).ShouldBeTrue();
        user.HasPermission(Permissions.ReportsView).ShouldBeFalse();
        
        user.CanAccessBranch(branchId).ShouldBeTrue();
        user.CanAccessBranch(Guid.NewGuid()).ShouldBeFalse();
        
        user.GetMaxDiscountPercent().ShouldBe(50m);
    }
}
