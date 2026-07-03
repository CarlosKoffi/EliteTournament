using CPElite.Contracts.Auth;
using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface ITokenService
{
    AuthToken Create(User user);
}
