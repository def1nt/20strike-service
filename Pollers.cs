using System.Management;
using System.DirectoryServices;

namespace _20strike;

partial class Application
{
    async Task<int> QueryAll()
    {
        List<string> computers = GetComputers();
        Dictionary<string, string> users = GetUsers(); // Do I really need it here, if it's requeried on every request?

        int total = computers.Count, current = 0;
        pollerProgress = 0;

        foreach (string? Computer in computers)
        {
            current++;
            await Task.Delay(60000, cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            taskhandler.AddAction(() => QueryComputer(Computer)); // TODO утекает и не проверяется завершение
            pollerProgress = current * 100 / total;
        }
        Console.WriteLine("Done updating");
        return current;
    }

    int QueryComputer(string? Computer)
    {
        List<string> classes = GetClasses();

        foreach (string? Class in classes)
        {
            Console.WriteLine($"\nComputer: {Computer}, Class: {Class}");
            try
            {
                QueryComputerClass(Computer, Class);
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
        return 0;
    }

    int QueryComputerClass(string? computername, string? classname)
    {
        if (!OperatingSystem.IsWindows()) return 1;
        if (computername == null || classname == null) return 1;
        if (classname == "Meta_Software") return PollSoftware(computername);
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
            return 1;
        }
        cleanup(computername, classname);
        int c = 0;
        foreach (ManagementObject o in mo)
        {
            if (c++ > 9 && classname == "Win32_NTLogEvent") break;
            PropertyDataCollection props = o.Properties;
            List<string?[]> props_processed = new() { };  // Deal with these nulls!
            foreach (var p in props)
            {
                if (p.Value == null) continue;

                string t = p.Type.ToString();

                string? s = GetFromCIMObject(p.Type, p.Value);
                if (classname == "Win32_NTLogEvent" && p.Name == "EventType" && s != "1") { c--; goto managementObjectsLoop; }
                props_processed.Add(new string?[5] { computername, classname, p.Name, t, s });
            }
            foreach (var p in props_processed) dbinsert(p);
            managementObjectsLoop:;
        }
        // merge();
        return 0;
    }

    string? GetFromCIMObject(CimType type, object value)
    {
        if (!OperatingSystem.IsWindows()) return "ERROR WRONG OS";
        return type switch  // But sometimes this is not a scalar but an array HMMMMMMMMMMM
        {
            CimType.String => ObjectToString<string>(value),
            CimType.UInt8 => ObjectToString<System.Byte>(value),
            CimType.UInt16 => ObjectToString<System.UInt16>(value),
            CimType.UInt32 => ObjectToString<System.UInt32>(value),
            CimType.UInt64 => ObjectToString<System.UInt64>(value),
            CimType.SInt16 => ObjectToString<System.Int16>(value),
            CimType.SInt32 => ObjectToString<System.Int32>(value),
            CimType.SInt64 => ObjectToString<System.Int64>(value),
            CimType.Boolean => ObjectToString<System.Boolean>(value),
            CimType.DateTime => value.ToString(),
            CimType.Reference => value.ToString(),
            _ => "ERROR NIY"
        };
    }

    private string ObjectToString<Type>(object o)
    {
        return o.GetType().IsArray ? ArrayToString<Type>(o) :
            (string.IsNullOrEmpty(((Type)o).ToString()) ? "" : ((Type)o).ToString()!);
    }

    private string ArrayToString<Type>(object array)
    {
        string res = "[";
        foreach (Type a in (array as Array)!)
        {
            res += ((Type)a).ToString() + ", ";
        }
        res += "]";
        res = res.Replace(", ]", "]");
        return res;
    }

    private List<string> GetComputers()
    {
        // var fcomputers = new StreamReader("./computers");
        // List<string> computers = new List<string> { };
        // while (!fcomputers.EndOfStream)
        //     computers.Add(fcomputers.ReadLine()!);

        // fcomputers.Close();
        // return computers;
        return GetADComputers();
    }

    private List<string> GetClasses()
    {
        var fclasses = new StreamReader("./classes");
        List<string> classes = new() { };
        while (!fclasses.EndOfStream)
            classes.Add(fclasses.ReadLine()!);

        fclasses.Close();
        return classes;
    }

    private List<string> GetADComputers()
    {
        List<string> computerNames = new();
        if (!OperatingSystem.IsWindows()) return computerNames;

        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();

        using (DirectoryEntry entry = new(@$"LDAP://{domain.Name}"))
        {
            using (DirectorySearcher mySearcher = new(entry))
            {
                mySearcher.Filter = "(objectClass=computer)";
                mySearcher.SizeLimit = 0;
                mySearcher.PageSize = 250;
                mySearcher.PropertiesToLoad.Add("name");

                foreach (SearchResult resEnt in mySearcher.FindAll())
                {
                    if (resEnt.Properties["name"].Count > 0)
                    {
                        string computerName = (string)resEnt.Properties["name"][0];
                        computerNames.Add(computerName);
                    }
                }
            }
        }

        computerNames.Sort();
        return computerNames;
    }
}
