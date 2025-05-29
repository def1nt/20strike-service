using System.Management;

namespace _20strike;

partial class Application
{
    private async Task QueryAll()
    {
        string[] computers = GetComputers();
        Dictionary<string, string> users = AD.GetUsers(); // Do I really need it here, if it's requeried on every request?

        int total = computers.Length, current = 0;
        pollerProgress = 0;

        foreach (string? Computer in computers)
        {
            current++;
            await Task.Delay(30000, cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            taskhandler.AddAction(() => QueryComputer(Computer)); // TODO утекает и не проверяется завершение
            pollerProgress = current * 100 / total;
        }
        Console.WriteLine("Done updating");
    }

    private void QueryComputer(string? Computer)
    {
        if (Computer == null) return;
        string[] classes = GetClasses();
        ComputerInfo computerInfo = new(Computer);

        foreach (string? Class in classes)
        {
            Console.WriteLine($"\nComputer: {Computer}, Class: {Class}");
            try
            {
                QueryComputerClass(Computer, Class, computerInfo);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Console.WriteLine($"ERROR Не удалось подключиться к {Computer}");
                break;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"ERROR Нет доступа к {Computer}");
                break;
            }
            catch (Exception error)
            {
                Console.WriteLine($"ERROR Неизвестная ошибка: {error.Message}");
            }
        }
        new Repository(computerInfo).Save();
    }

    private void QueryComputerClass(string? computername, string? classname, ComputerInfo computerInfo)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (computername == null || classname == null) return;
        if (classname == "Meta_Software")
        {
            var t = PollSoftware(computername);
            computerInfo.Software = [.. t];
            return;
        }
        string WMIProvider = "cimv2";
        if (classname == "WmiMonitorID") WMIProvider = "wmi";
        var mp = new ManagementPath($@"\\{computername}\root\{WMIProvider}:{classname}");
        var mc = new ManagementClass(mp);
        ManagementObjectCollection mo;
        try
        {
            mo = mc.GetInstances();
        }
        catch (ManagementException)
        {
            Console.WriteLine($"ERROR Unsupported class {classname} on {computername}");
            return;
        }
        DBCleanup(computername, classname);
        int c = 0;
        foreach (ManagementObject o in mo)
        {
            if (c++ > 9 && classname == "Win32_NTLogEvent") break;
            PropertyDataCollection props = o.Properties;
            List<string[]> props_processed = [];  // Deal with these nulls!
            foreach (var p in props)
            {
                if (p.Value == null) continue;

                string t = p.Type.ToString();

                string s = GetFromCIMObject(p.Type, p.Value);
                if (classname == "Win32_NTLogEvent" && p.Name == "EventType" && s != "1") { c--; goto managementObjectsLoop; }
                props_processed.Add([computername, classname, p.Name, t, s]);
            }
            ProcessManagementObject(o, classname, computerInfo);
            foreach (var p in props_processed) DBInsert(p);
            managementObjectsLoop:;
        }
    }

    private static string GetFromCIMObject(CimType type, object value)
    {
        if (!OperatingSystem.IsWindows()) return "ERROR WRONG OS";
        return type switch  // But sometimes this is not a scalar but an array HMMMMMMMMMMM
        {
            CimType.String => ObjectToString(value),
            CimType.UInt8 or CimType.UInt16 or CimType.UInt32 or CimType.UInt64 => ObjectToString(value),
            CimType.SInt16 or CimType.SInt32 or CimType.SInt64 => ObjectToString(value),
            CimType.Boolean => ObjectToString(value),
            CimType.DateTime => value.ToString() ?? "null value",
            CimType.Reference => value.ToString() ?? "null value",
            _ => "UNKNOWN TYPE"
        };
    }

    private static string ObjectToString(object o)
    {
        if (o.GetType().IsArray)
        {
            var enumerable = o as System.Collections.IEnumerable;
            return "[" + string.Join(", ", enumerable.Cast<object>().Select(x => x.ToString())) + "]";
        }
        return string.IsNullOrEmpty(o.ToString()) ? "" : o.ToString() ?? "";
    }

    private static string[] GetComputers()
    {
        string[] computers = [];
        if (File.Exists("./computers"))
        {
            computers = File.ReadAllLines("./computers")
            .SelectMany(c => c == "@currentdomain" ? AD.GetComputers() : [c])
            .ToArray();
        }
        return [.. computers];
    }

    private static string[] GetClasses()
    {
        return [.. File.ReadAllLines("./classes")];
    }

    private static void ProcessManagementObject(ManagementObject o, string classname, ComputerInfo computerInfo)
    {
        if (!OperatingSystem.IsWindows()) return;
        switch (classname)
        {
            case "Win32_BIOS":
                computerInfo.BIOS ??= new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    Vendor = o["Manufacturer"]?.ToString() ?? "",
                    Version = o["Version"]?.ToString() ?? "",
                    ReleaseDate = o["ReleaseDate"]?.ToString() ?? "",
                };
                break;
            case "Win32_ComputerSystem":
                computerInfo.ComputerSystem ??= new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    Domain = o["Domain"]?.ToString() ?? "",
                    UserName = o["UserName"]?.ToString() ?? "",
                };
                break;
            case "Win32_OperatingSystem":
                computerInfo.OperatingSystem ??= new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    Version = o["Version"]?.ToString() ?? "",
                    BuildNumber = o["BuildNumber"]?.ToString() ?? "",
                    Architecture = o["OSArchitecture"]?.ToString() ?? "",
                    InstallDate = o["InstallDate"]?.ToString() ?? "",
                    LastBootUpTime = o["LastBootUpTime"]?.ToString() ?? "",
                    LocalTime = o["LocalDateTime"]?.ToString() ?? "",
                };
                break;
            case "Win32_Processor":
                computerInfo.Processor ??= [];
                ProcessorInfo pi = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    ClockSpeed = o["CurrentClockSpeed"]?.ToString() ?? "",
                    CacheSize = o["L3CacheSize"]?.ToString() ?? "",
                    NumberOfCores = o["NumberOfCores"]?.ToString() ?? "",
                    SocketType = o["SocketDesignation"]?.ToString() ?? ""
                };
                computerInfo.Processor = [.. computerInfo.Processor, pi];
                break;
            case "Win32_PhysicalMemory":
                computerInfo.PhysicalMemory ??= [];
                PhysicalMemoryInfo pmi = new()
                {
                    Capacity = o["Capacity"]?.ToString() ?? "",
                    Manufacturer = o["Manufacturer"]?.ToString() ?? "",
                    SerialNumber = o["SerialNumber"]?.ToString() ?? "",
                    ClockSpeed = o["Speed"]?.ToString() ?? ""
                };
                computerInfo.PhysicalMemory = [.. computerInfo.PhysicalMemory, pmi];
                break;
            case "Win32_DiskDrive":
                computerInfo.PhysicalDisk ??= [];
                PhysicalDiskInfo pdi = new()
                {
                    Model = o["Model"]?.ToString() ?? "",
                    SerialNumber = o["SerialNumber"]?.ToString() ?? "",
                    Size = o["Size"]?.ToString() ?? "",
                    InterfaceType = o["InterfaceType"]?.ToString() ?? ""
                };
                computerInfo.PhysicalDisk = [.. computerInfo.PhysicalDisk, pdi];
                break;
            case "Win32_LogicalDisk":
                computerInfo.LogicalDisk ??= [];
                LogicalDiskInfo ldi = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    Size = o["Size"]?.ToString() ?? "",
                    FreeSpace = o["FreeSpace"]?.ToString() ?? "",
                    FileSystem = o["FileSystem"]?.ToString() ?? "Unknown",
                    DriveType = o["DriveType"]?.ToString() ?? ""
                };
                computerInfo.LogicalDisk = [.. computerInfo.LogicalDisk, ldi];
                break;
            case "Win32_VideoController":
                computerInfo.VideoController ??= [];
                VideoControllerInfo vci = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    DriverVersion = o["DriverVersion"]?.ToString() ?? ""
                };
                computerInfo.VideoController = [.. computerInfo.VideoController, vci];
                break;
            case "WmiMonitorID":
                computerInfo.Monitor ??= [];
                MonitorInfo mi = new()
                {
                    Name = o["InstanceName"]?.ToString() ?? ""
                };
                computerInfo.Monitor = [.. computerInfo.Monitor, mi];
                break;
            case "Win32_NetworkAdapter":
                computerInfo.NetworkAdapter ??= [];
                NetworkAdapterInfo nai = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    MacAddress = o["MACAddress"]?.ToString() ?? ""
                };
                computerInfo.NetworkAdapter = [.. computerInfo.NetworkAdapter, nai];
                break;
            case "Win32_Printer":
                computerInfo.Printer ??= [];
                PrinterInfo pri = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    PaperSize = o["PrinterPaperNames"]?.ToString() ?? "",
                    PortName = o["PortName"]?.ToString() ?? ""
                };
                computerInfo.Printer = [.. computerInfo.Printer, pri];
                break;
            case "Win32_Process":
                computerInfo.Process ??= [];
                ProcessInfo psi = new()
                {
                    Name = o["Name"]?.ToString() ?? "",
                    ProcessId = o["ProcessId"]?.ToString() ?? "",
                    WorkingSetSize = o["WorkingSetSize"]?.ToString() ?? "",
                    CreationDate = o["CreationDate"]?.ToString() ?? ""
                };
                computerInfo.Process = [.. computerInfo.Process, psi];
                break;
            default:
                break;
        }
    }
}
