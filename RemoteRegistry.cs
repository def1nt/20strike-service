using Microsoft.Win32;

namespace _20strike;

partial class Application
{
    int PollSoftware(string computername)
    {
        if (!OperatingSystem.IsWindows()) return 1;
        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        string path64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        RegistryKey anotherkey;
        try
        {
            anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computername);
        }
        catch (Exception e)
        {
            if (e is IOException) { Console.WriteLine($"ERROR: {e.Message}"); return 1; }
            throw;
        }
        cleanup(computername, "Meta_Software");
        RegistryKey? key = anotherkey.OpenSubKey(path);
        if (key != null) AddToDB(key, computername);

        anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computername);
        key = anotherkey.OpenSubKey(path64);
        if (key != null) AddToDB(key, computername);

        anotherkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, computername);
        var users = anotherkey.GetSubKeyNames();
        foreach (var user in users)
        {
            try
            {
                using RegistryKey? usertree = anotherkey.OpenSubKey(user);
                if (usertree == null) continue;
                key = usertree.OpenSubKey(path);
                if (key != null) AddToDB(key, computername);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                continue;
            }
        }
        // merge();
        return 0;
    }

    void AddToDB(RegistryKey rk, string computername)
    {
        if (!OperatingSystem.IsWindows()) return;
        var softwarelist = rk.GetSubKeyNames();
        static string unnull(object? x) => x != null ? x.ToString()! : "";
        foreach (string software in softwarelist)
        {
            using RegistryKey? softwareinfo = rk.OpenSubKey(software);
            if (softwareinfo == null) continue;

            string[] POIKeys = { "DisplayName", "DisplayVersion", "EstimatedSize", "InstallDate", "InstallLocation", "Publisher", "URLInfoAbout" };

            if (unnull(softwareinfo.GetValue("DisplayName")) == "") continue;
            foreach (string key in POIKeys)
            {
                dbinsert(new string?[] { computername, "Meta_Software", key, "String", unnull(softwareinfo.GetValue(key)) });
            }
        }
    }
}
