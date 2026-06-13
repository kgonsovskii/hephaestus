using FubarDev.FtpServer.AccountManagement;

namespace DataFtp;

internal sealed class DataFtpMembershipProvider : IMembershipProvider
{
    public Task<MemberValidationResult> ValidateUserAsync(string username, string password)
    {
        if (string.Equals(username, DataFtpConstants.UserName, StringComparison.Ordinal)
            && password == DataFtpConstants.Password)
        {
            var user = new DataFtpUser(username);
            return Task.FromResult(
                new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, user));
        }

        return Task.FromResult(new MemberValidationResult(MemberValidationStatus.InvalidLogin));
    }

    private sealed class DataFtpUser : IFtpUser
    {
        public DataFtpUser(string name) => Name = name;

        public string Name { get; }

        public bool IsInGroup(string group) => false;
    }
}
