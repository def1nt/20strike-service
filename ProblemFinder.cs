namespace _20strike;

partial class Application
{
    private static ProblemInfo[] FindProblems(string computerName)
    {
        var computerInfo = Repository.Load($"{computerName}.json");
        if (computerInfo is null) return [new("Invalid computer name", "Computer not found")];

        return [.. new ProblemInfo?[]{
            CheckIfDataExists(computerInfo.ComputerSystem),
            CheckLastSeen(computerInfo.OperatingSystem),
            CheckOSVersion(computerInfo.OperatingSystem),
            }.OfType<ProblemInfo>(),
            .. CheckDiskHealth(computerInfo.PhysicalDisk),
            .. CheckFreeDiskSpace(computerInfo.LogicalDisk),
        ];
    }

    private static ProblemInfo? CheckIfDataExists(ComputerSystemInfo? systemInfo)
    {
        if (systemInfo is null) return new ProblemInfo("Data not found", "Computer system data not found");
        else return null;
    }

    private static ProblemInfo? CheckLastSeen(OperatingSystemInfo? osInfo)
    {
        if (osInfo == null) return new ProblemInfo("Data not found", "Operating system data not found");
        if (!DateTime.TryParseExact(osInfo.LocalTime[..14], // Without +TZ part
                                    "yyyyMMddHHmmss",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None,
                                    out var localtime)) return new ProblemInfo("Invalid value", "Local Time is invalid date");
        var since = DateTime.Now - localtime;
        if (since.TotalDays > 30) return new ProblemInfo("Last seen", $"Last seen {since.TotalDays:N0} days ago");
        return null;
    }

    private static ProblemInfo? CheckOSVersion(OperatingSystemInfo? osInfo)
    {
        if (osInfo == null) return new ProblemInfo("Data not found", "Operating system data not found");
        if (!int.TryParse(osInfo.BuildNumber, out int buildNumber)) return new ProblemInfo("Invalid build number", "Build number is not a valid integer");
        if (buildNumber < 9600) return new ProblemInfo("OS version", "OS version is older than Windows 8");
        return null;
    }

    private static ProblemInfo[] CheckDiskHealth(PhysicalDiskInfo[]? diskInfo)
    {
        if (diskInfo == null || diskInfo.Length == 0) return [new ProblemInfo("Data not found", "Disk data not found")];
        ProblemInfo[] problems = [];
        foreach (var disk in diskInfo)
        {
            if (disk.Status != "OK") problems = [.. problems, new ProblemInfo("Disk health", $"Disk {disk.Model} is not healthy or SMART data is missing")];
        }
        return problems;
    }

    private static ProblemInfo[] CheckFreeDiskSpace(LogicalDiskInfo[]? diskInfo)
    {
        if (diskInfo == null || diskInfo.Length == 0) return [new ProblemInfo("Data not found", "Disk data not found")];
        ProblemInfo[] problems = [];
        foreach (var disk in diskInfo.Where(d => d.DriveType == "3")) // Drive type is 3 for fixed drives
        {
            if (!long.TryParse(disk.FreeSpace, out long freeSpace))
            {
                problems = [.. problems, new ProblemInfo("Invalid value", $"Free space for disk {disk.Name} is not a valid integer")];
                continue;
            }
            if (!long.TryParse(disk.Size, out long size))
            {
                problems = [.. problems, new ProblemInfo("Invalid value", $"Size for disk {disk.Name} is not a valid integer")];
                continue;
            }
            var percentage = freeSpace * 100 / size;
            if (percentage < 5) problems = [.. problems, new ProblemInfo("Free disk space", $"Disk {disk.Name} has {percentage}% free space")];
        }
        return problems;
    }
}

record ProblemInfo(string Name, string Description);
