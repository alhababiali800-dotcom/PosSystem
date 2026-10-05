using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pos.Application.Context;
using Pos.Application.Interfaces;
using Pos.Domain.Entities;

namespace Pos.Application.UseCases;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> LoginAsync(
        string username,
        string plainPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(plainPassword))
            return false;

        var repo = _unitOfWork.GetRepository<User>();

        var user = await repo.FirstOrDefaultAsync(
            u => u.Username == username && !u.IsDeleted,
            cancellationToken);

        if (user is null)
            return false;

        var incomingHash = ComputeSha256(plainPassword);
        if (!string.Equals(incomingHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
            return false;

        SessionManager.Login(user.Id, user.BranchId);
        return true;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}