using System.DirectoryServices;

namespace _20strike;

static class AD
{
    public const string SamAccountNameProperty = "SamAccountName";
    public const string CanonicalNameProperty = "CN";

    public struct ADUser
    {
        public string CN { get; set; }
        public string SamAcountName { get; set; }
    }

    public static Dictionary<string, string> GetUsers()
    {
        List<ADUser> users = [];
        if (!OperatingSystem.IsWindows()) return [];
        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
        using (DirectoryEntry searchRoot = new(@$"LDAP://{domain.Name}"))
        using (DirectorySearcher directorySearcher = new(searchRoot))
        {
            // Set the filter
            directorySearcher.Filter = "(&(objectCategory=person)(objectClass=user))";

            // Set the properties to load.
            directorySearcher.PropertiesToLoad.Add(CanonicalNameProperty);
            directorySearcher.PropertiesToLoad.Add(SamAccountNameProperty);

            using SearchResultCollection searchResultCollection = directorySearcher.FindAll();
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

        return new(
            users.Where(u => !string.IsNullOrEmpty(u.SamAcountName))
            .DistinctBy(u => u.SamAcountName).OrderBy(u => u.SamAcountName)
            .Select(u => new KeyValuePair<string, string>(u.SamAcountName, u.CN)));
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
