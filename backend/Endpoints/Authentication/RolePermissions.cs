using Domain.Users;

namespace Api.Authentication;

public static class RolePermissions
{
    private static readonly string[] AllPermissions =
    [
        Permissions.ReadUsers, Permissions.CreateUsers, Permissions.UpdateUsers, Permissions.DeleteUsers,
        Permissions.ReadCompanies, Permissions.CreateCompanies, Permissions.UpdateCompanies, Permissions.DeleteCompanies,
        Permissions.ReadSites, Permissions.CreateSites, Permissions.UpdateSites, Permissions.DeleteSites,
        Permissions.ReadExpenses, Permissions.CreateExpenses, Permissions.UpdateExpenses, Permissions.DeleteExpenses,
    ];

    private static readonly string[] ViewerPermissions =
    [
        Permissions.ReadUsers, Permissions.ReadCompanies, Permissions.ReadSites, Permissions.ReadExpenses,
    ];

    public static IReadOnlyList<string> For(Role role) =>
        role == Role.Admin ? AllPermissions : ViewerPermissions;
}