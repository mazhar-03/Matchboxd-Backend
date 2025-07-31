using Matchboxd.API.Models;

namespace Matchboxd.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}