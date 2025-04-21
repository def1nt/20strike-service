using Microsoft.Win32;

namespace _20strike;

partial class Application
{
    private List<SoftwareInfo> PollSoftware(string computername)
    {
        if (!OperatingSystem.IsWindows()) return [];
        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        string path64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        RegistryKey anotherkey;
        try
        {
            anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computername);
        }
        catch (Exception e)
        {
            if (e is IOException) { Console.WriteLine($"ERROR: {e.Message}"); return []; }
            throw;
        }
        DBCleanup(computername, "Meta_Software");
        RegistryKey? key = anotherkey.OpenSubKey(path);
        List<SoftwareInfo> softwareinfo = [];
        if (key != null) softwareinfo = AddToDB(key, computername);

        anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computername);
        key = anotherkey.OpenSubKey(path64);
        if (key != null) softwareinfo = [.. softwareinfo, .. AddToDB(key, computername)];

        anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, computername);
        var users = anotherkey.GetSubKeyNames();
        foreach (var user in users)
        {
            try
            {
                using RegistryKey? usertree = anotherkey.OpenSubKey(user);
                if (usertree == null) continue;
                key = usertree.OpenSubKey(path);
                if (key != null) softwareinfo = [.. softwareinfo, .. AddToDB(key, computername)];
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                continue;
            }
        }
        // merge();
        return softwareinfo;
    }

    private List<SoftwareInfo> AddToDB(RegistryKey rk, string computername)
    {
        List<SoftwareInfo> softwareinfo_ = [];
        if (!OperatingSystem.IsWindows()) return softwareinfo_;
        var softwarelist = rk.GetSubKeyNames();
        static string unnull(object? x) => x != null ? x.ToString()! : "";
        foreach (string software in softwarelist)
        {
            using RegistryKey? softwareinfo = rk.OpenSubKey(software);
            if (softwareinfo == null) continue;

            string[] POIKeys = ["DisplayName", "DisplayVersion", "EstimatedSize", "InstallDate", "InstallLocation", "Publisher", "URLInfoAbout"];

            if (unnull(softwareinfo.GetValue("DisplayName")) == "") continue;
            Dictionary<string, string> softwareRecordTemp = [];
            foreach (string key in POIKeys)
            {
                softwareRecordTemp.Add(key, unnull(softwareinfo.GetValue(key)));
                DBInsert([computername, "Meta_Software", key, "String", unnull(softwareinfo.GetValue(key))]);
            }
            softwareinfo_.Add(new()
            {
                Name = softwareRecordTemp.TryGetValue("DisplayName", out string? name) ? name! : "",
                Version = softwareRecordTemp.TryGetValue("DisplayVersion", out string? version) ? version! : "",
                EstimatedSize = softwareRecordTemp.TryGetValue("EstimatedSize", out string? size) ? size! : "",
                InstallDate = softwareRecordTemp.TryGetValue("InstallDate", out string? date) ? date! : "",
                InstallLocation = softwareRecordTemp.TryGetValue("InstallLocation", out string? location) ? location! : "",
            });
        }
        return softwareinfo_;
    }
}
