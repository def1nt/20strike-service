namespace _20strike;

public sealed class ComputerInfo(string name)
{
    public string Name = name;
    // BIOS - name, vendor, version, release date
    public BIOSInfo? BIOS { get; set; }
    // Computer system - name, domain, user name
    public ComputerSystemInfo? ComputerSystem { get; set; }
    // Operating system - name, version, build number, architecture, install date, uptime, local time
    public OperatingSystemInfo? OperatingSystem { get; set; }
    // Processor - name, model, clock speed, cache size, number of cores, socket type
    public ProcessorInfo[]? Processor { get; set; }
    // Physical Memory - size, manufacturer, serial number, clock speed, capacity
    public PhysicalMemoryInfo[]? PhysicalMemory { get; set; }
    // Physical Disk - model, serial number, size, interface type
    public PhysicalDiskInfo[]? PhysicalDisk { get; set; }
    // Logical Disk - name, size, free space, drive type
    public LogicalDiskInfo[]? LogicalDisk { get; set; }
    // Video controller - name, driver version
    public VideoControllerInfo[]? VideoController { get; set; }
    // Monitor - name
    public MonitorInfo[]? Monitor { get; set; }
    // Network adapter - name, mac address
    public NetworkAdapterInfo[]? NetworkAdapter { get; set; }
    // Printer - name, paper size, port name
    public PrinterInfo[]? Printer { get; set; }
    // Software - name, version, install date, install location, estimated size
    public SoftwareInfo[]? Software { get; set; }
    // Processes - name, memory usage, cpu usage
    public ProcessInfo[]? Process { get; set; }
    // Map data
    public MapData? Location { get; set; }
}

public sealed class BIOSInfo(string name, string vendor, string version, string releaseDate)
{
    public string Name { get; set; } = name;
    public string Vendor { get; set; } = vendor;
    public string Version { get; set; } = version;
    public string ReleaseDate { get; set; } = releaseDate;
    public BIOSInfo() : this("", "", "", "") { }
}

public sealed class ComputerSystemInfo(string name, string domain, string userName)
{
    public string Name { get; set; } = name;
    public string Domain { get; set; } = domain;
    public string UserName { get; set; } = userName;
    public ComputerSystemInfo() : this("", "", "") { }
}

public sealed class OperatingSystemInfo(string name, string version, string buildNumber, string architecture, string installDate, string lastBootUp, string localTime)
{
    public string Name { get; set; } = name;
    public string Version { get; set; } = version;
    public string BuildNumber { get; set; } = buildNumber;
    public string Architecture { get; set; } = architecture;
    public string InstallDate { get; set; } = installDate;
    public string LastBootUpTime { get; set; } = lastBootUp;
    public string LocalTime { get; set; } = localTime;
    public OperatingSystemInfo() : this("", "", "", "", "", "", "") { }
}

public sealed class ProcessorInfo(string name, string clockSpeed, string cacheSize, string numberOfCores, string socketType)
{
    public string Name { get; set; } = name;
    public string ClockSpeed { get; set; } = clockSpeed;
    public string CacheSize { get; set; } = cacheSize;
    public string NumberOfCores { get; set; } = numberOfCores;
    public string SocketType { get; set; } = socketType;
    public ProcessorInfo() : this("", "", "", "", "") { }
}

public sealed class PhysicalMemoryInfo(string manufacturer, string serialNumber, string clockSpeed, string capacity)
{
    public string Capacity { get; set; } = capacity;
    public string Manufacturer { get; set; } = manufacturer;
    public string SerialNumber { get; set; } = serialNumber;
    public string ClockSpeed { get; set; } = clockSpeed;
    public PhysicalMemoryInfo() : this("", "", "", "") { }
}

public sealed class PhysicalDiskInfo(string model, string serialNumber, string size, string interfaceType)
{
    public string Model { get; set; } = model;
    public string SerialNumber { get; set; } = serialNumber;
    public string Size { get; set; } = size;
    public string InterfaceType { get; set; } = interfaceType;
    public PhysicalDiskInfo() : this("", "", "", "") { }
}

public sealed class LogicalDiskInfo(string name, string size, string freeSpace, string filesystem, string driveType)
{
    public string Name { get; set; } = name;
    public string Size { get; set; } = size;
    public string FreeSpace { get; set; } = freeSpace;
    public string FileSystem { get; set; } = filesystem;
    public string DriveType { get; set; } = driveType;
    public LogicalDiskInfo() : this("", "", "", "", "") { }
}

public sealed class VideoControllerInfo(string name, string driverVersion)
{
    public string Name { get; set; } = name;
    public string DriverVersion { get; set; } = driverVersion;
    public VideoControllerInfo() : this("", "") { }
}

public sealed class MonitorInfo(string name)
{
    public string Name { get; set; } = name;
    public MonitorInfo() : this("") { }
}

public sealed class NetworkAdapterInfo(string name, string macAddress)
{
    public string Name { get; set; } = name;
    public string MacAddress { get; set; } = macAddress;
    public NetworkAdapterInfo() : this("", "") { }
}

public sealed class PrinterInfo(string name, string paperSize, string portName)
{
    public string Name { get; set; } = name;
    public string PaperSize { get; set; } = paperSize;
    public string PortName { get; set; } = portName;
    public PrinterInfo() : this("", "", "") { }
}

public sealed class SoftwareInfo(string name, string version, string installDate, string installLocation, string estimatedSize)
{
    public string Name { get; set; } = name;
    public string Version { get; set; } = version;
    public string InstallDate { get; set; } = installDate;
    public string InstallLocation { get; set; } = installLocation;
    public string EstimatedSize { get; set; } = estimatedSize;
    public SoftwareInfo() : this("", "", "", "", "") { }
}

public sealed class ProcessInfo(string name, string processId, string workingSetSize, string creationDate)
{
    public string Name { get; set; } = name;
    public string ProcessId { get; set; } = processId;
    public string WorkingSetSize { get; set; } = workingSetSize;
    public string CreationDate { get; set; } = creationDate;
    public ProcessInfo() : this("", "", "", "") { }
}

public sealed class MapData(double x, double y, string description)
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public string Description { get; set; } = description;
    public MapData() : this(0.0, 0.0, "") { }
}
