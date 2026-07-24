using IssueTracker.Core.Models;

namespace IssueTracker.Core.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}