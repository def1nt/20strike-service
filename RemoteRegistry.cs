using Microsoft.Win32;

namespace _20strike;

partial class Application
{
    static readonly string[] POIKeys = ["DisplayName", "DisplayVersion", "EstimatedSize", "InstallDate", "InstallLocation", "Publisher", "URLInfoAbout"];

    private List<SoftwareInfo> PollSoftware(string computername)
    {
        if (!OperatingSystem.IsWindows()) return [];
        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        string path64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        try
        {
            List<SoftwareInfo> softwareinfo = [];

            using RegistryKey HKLMRootKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computername);
            {
                DBCleanup(computername, "Meta_Software");
                using RegistryKey? key32 = HKLMRootKey.OpenSubKey(path);
                if (key32 is not null) softwareinfo = AddToDB(key32, computername);

                using RegistryKey? key64 = HKLMRootKey.OpenSubKey(path64);
                if (key64 is not null) softwareinfo.AddRange(AddToDB(key64, computername));
            }

            using RegistryKey HKURootKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, computername);
            {
                var users = HKURootKey.GetSubKeyNames();
                foreach (var user in users)
                {
                    try
                    {
                        using RegistryKey? usertree = HKURootKey.OpenSubKey(user);
                        if (usertree == null) continue;
                        using RegistryKey? key = usertree.OpenSubKey(path);
                        if (key != null) softwareinfo.AddRange(AddToDB(key, computername));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR: User {user} registry access failed: {e.Message}");
                        continue;
                    }
                }
            }
            return softwareinfo;
        }
        catch (Exception e)
        {
            if (e is IOException) { Console.WriteLine($"ERROR: {e.Message}"); return []; }
            throw;
        }
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

            if (unnull(softwareinfo.GetValue("DisplayName")) == "") continue;
            Dictionary<string, string> softwareRecordTemp = [];
            foreach (string key in POIKeys)
            {
                string value = unnull(softwareinfo.GetValue(key));
                softwareRecordTemp.Add(key, value);
                DBInsert([computername, "Meta_Software", key, "String", value]);
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
