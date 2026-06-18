using System.DirectoryServices;

namespace _20strike;

static class AD
{
    public const string SamAccountNameProperty = "SamAccountName";
    public const string CanonicalNameProperty = "CN";

    private struct ADUser
    {
        public string CN { get; set; }
        public string SamAccountName { get; set; }
    }

    public static Dictionary<string, string> GetUsers()
    {
        List<ADUser> users = [];
        if (!OperatingSystem.IsWindows()) return [];
        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
        using (DirectoryEntry searchRoot = new(@$"LDAP://{domain.Name}"))
        using (DirectorySearcher directorySearcher = new(searchRoot))
        {
            directorySearcher.Filter = "(&(objectCategory=person)(objectClass=user))";

            directorySearcher.PropertiesToLoad.Add(CanonicalNameProperty);
            directorySearcher.PropertiesToLoad.Add(SamAccountNameProperty);

            using SearchResultCollection searchResultCollection = directorySearcher.FindAll();
            foreach (SearchResult searchResult in searchResultCollection)
            {
                var user = new ADUser();

                // Set CN if available.
                if (searchResult.Properties[CanonicalNameProperty].Count > 0)
                    user.CN = searchResult.Properties[CanonicalNameProperty][0].ToString() ?? "Unknown";

                // Set sAMAccountName if available
                if (searchResult.Properties[SamAccountNameProperty].Count > 0)
                    user.SamAccountName = searchResult.Properties[SamAccountNameProperty][0].ToString()?.ToLower() ?? "Unknown";

                users.Add(user);
            }
        }

        return new(
            users.Where(u => !string.IsNullOrEmpty(u.SamAccountName))
            .OrderBy(u => u.SamAccountName)
            .Select(u => new KeyValuePair<string, string>(u.SamAccountName, u.CN)));
    }

    public static List<string> GetComputers()
    {
        List<string> computerNames = [];
        if (!OperatingSystem.IsWindows()) return computerNames;

        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();

        using (DirectoryEntry entry = new(@$"LDAP://{domain.Name}"))
        {
            using DirectorySearcher mySearcher = new(entry);
            mySearcher.Filter = "(objectClass=computer)";
            mySearcher.SizeLimit = 0;
            mySearcher.PageSize = 250;
            mySearcher.PropertiesToLoad.Add("name");

            using SearchResultCollection myResults = mySearcher.FindAll();
            foreach (SearchResult resEnt in myResults)
            {
                if (resEnt.Properties["name"].Count > 0)
                {
                    string computerName = (string)resEnt.Properties["name"][0];
                    computerNames.Add(computerName);
                }
            }
        }

        computerNames.Sort();
        return computerNames;
    }
}
