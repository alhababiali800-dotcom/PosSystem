using System;

namespace Pos.Application.Context;

public static class SessionManager
{
    public static Guid CurrentUserId { get; private set; } = Guid.Empty;
    public static Guid CurrentBranchId { get; private set; } = Guid.Empty;
    public static bool IsLoggedIn { get; private set; } = false;

    public static void Login(Guid userId, Guid branchId)
    {
        CurrentUserId = userId;
        CurrentBranchId = branchId;
        IsLoggedIn = true;
    }

    public static void Logout()
    {
        CurrentUserId = Guid.Empty;
        CurrentBranchId = Guid.Empty;
        IsLoggedIn = false;
    }
}