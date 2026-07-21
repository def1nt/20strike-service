namespace _20strike;

partial class Application
{
    private static ProblemInfo[] FindProblems(string computerName)
    {
        var computerInfo = Repository.Load($"{computerName}.json");
        if (computerInfo is null) return [new("Invalid computer name", "Computer not found")];

        return [.. new ProblemInfo?[]{
            CheckIfDataExists(computerInfo.ComputerSystem),
            CheckLastBootUp(computerInfo.OperatingSystem),
            CheckOSVersion(computerInfo.OperatingSystem),
            CheckDiskHealth(computerInfo.PhysicalDisk),
            CheckFreeDiskSpace(computerInfo.LogicalDisk)
        }.OfType<ProblemInfo>()];
    }

    private static ProblemInfo? CheckIfDataExists(ComputerSystemInfo? systemInfo) { throw new NotImplementedException(); }
    private static ProblemInfo? CheckLastBootUp(OperatingSystemInfo? osInfo) { throw new NotImplementedException(); }
    private static ProblemInfo? CheckOSVersion(OperatingSystemInfo? osInfo) { throw new NotImplementedException(); }
    private static ProblemInfo? CheckDiskHealth(PhysicalDiskInfo[]? diskInfo) { throw new NotImplementedException(); }
    private static ProblemInfo? CheckFreeDiskSpace(LogicalDiskInfo[]? diskInfo) { throw new NotImplementedException(); }
}

record ProblemInfo(string Name, string Description);
