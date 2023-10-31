using System.DirectoryServices;

namespace _20strike;

partial class Application
{
    public const string SamAccountNameProperty = "SamAccountName";
    public const string CanonicalNameProperty = "CN";

    public struct ADUser
    {
        public string CN { get; set; }
        public string SamAcountName { get; set; }
    }

    public Dictionary<string, string> GetUsers()
    {
            List<ADUser> users = new List<ADUser>();
            if (!OperatingSystem.IsWindows()) return new Dictionary<string, string>{};
            var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
            using (DirectoryEntry searchRoot = new DirectoryEntry(@$"LDAP://{domain.Name}"))
            using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
            {
                // Set the filter
                directorySearcher.Filter = "(&(objectCategory=person)(objectClass=user))";

                // Set the properties to load.
                directorySearcher.PropertiesToLoad.Add(CanonicalNameProperty);
                directorySearcher.PropertiesToLoad.Add(SamAccountNameProperty);

                using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                {
                    foreach (SearchResult searchResult in searchResultCollection)
                    {
                        // Create new ADUser instance
                        var user = new ADUser();

                        // Set CN if available.
                        if (searchResult.Properties[CanonicalNameProperty].Count > 0)
                            user.CN = searchResult.Properties[CanonicalNameProperty][0].ToString() ?? "Unknown";

                        // Set sAMAccountName if available
                        if (searchResult.Properties[SamAccountNameProperty].Count > 0)
                            user.SamAcountName = searchResult.Properties[SamAccountNameProperty][0].ToString()?.ToLower() ?? "Unknown";

                        // Add user to users list.
                        users.Add(user);
                    }
                }
            }

            return new Dictionary<string, string>(
                users.Where(u => !string.IsNullOrEmpty(u.SamAcountName))
                .DistinctBy(u => u.SamAcountName).OrderBy(u => u.SamAcountName)
                .Select(u => new KeyValuePair<string, string>(u.SamAcountName, u.CN)));
    }
}