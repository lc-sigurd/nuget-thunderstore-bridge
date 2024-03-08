using NuGet.Packaging.Core;

namespace Build.Extensions;

public static class PackageIdentityExtensions
{
    public static string FormatPackageName(this PackageIdentity identity)
    {
        return identity.Id
            .Replace(".", "_")
            .Replace("-", "_");
    }
}
