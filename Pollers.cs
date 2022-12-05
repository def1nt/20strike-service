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
            pollerProgress = (int)(current * 100 / total);
        }
        System.Console.WriteLine("Done updating");
        return current;
    }

    int QueryComputer(string? Computer)
    {
        List<string> classes = GetClasses();

        foreach (string? Class in classes)
        {
            System.Console.WriteLine($"\nComputer: {Computer}, Class: {Class}");
            try
            {
                QueryComputerClass(Computer, Class);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                System.Console.WriteLine($"ERROR Не удалось подключиться к {Computer}");
                break;
            }
            catch (System.UnauthorizedAccessException)
            {
                System.Console.WriteLine($"ERROR Нет доступа к {Computer}");
                break;
            }
            catch (Exception error)
            {
                System.Console.WriteLine($"ERROR Неизвестная ошибка: {error.Message}");
            }
        }
        return 0;
    }

    int QueryComputerClass(string? computername, string? classname)
    {
        if (!OperatingSystem.IsWindows()) return 1;
        if (computername == null || classname == null) return 1;
        if (classname == "Meta_Software") return PollSoftware(computername);
        var mp = new ManagementPath($@"\\{computername}\root\cimv2:{classname}");
        var mc = new ManagementClass(mp);
        ManagementObjectCollection mo;
        try
        {
            mo = mc.GetInstances();
        }
        catch (System.Management.ManagementException)
        {
            System.Console.WriteLine($"ERROR Unsupported class {classname} on {computername}");
            return 1;
        }
        cleanup(computername, classname);
        foreach (ManagementObject o in mo)
        {
            PropertyDataCollection props = o.Properties;

            foreach (var p in props)
            {
                if (p.Value == null) continue;

                string t = p.Type.ToString();

                string? s = getFromCIMObject(p.Type, p.Value);

                dbinsert(new string?[] { computername, classname, p.Name, t, s });
            }
        }
        // merge();
        return 0;
    }

    string? getFromCIMObject(CimType type, object value)
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
            CimType.Reference => (value).ToString(),
            _ => "ERROR NIY"
        };
    }

    string ObjectToString<type>(object o)
    {
        return (o.GetType().IsArray ? ArrayToString<type>(o) :
            (string.IsNullOrEmpty(((type)o).ToString()) ? "" : ((type)o).ToString()!));
    }

    string ArrayToString<type>(object array)
    {
        string res = "[";
        foreach (type a in (array as Array)!)
        {
            res += (((type)a).ToString() + ", ");
        }
        res += "]";
        res = res.Replace(", ]", "]");
        return res;
    }

    List<string> GetComputers()
    {
        // var fcomputers = new StreamReader("./computers");
        // List<string> computers = new List<string> { };
        // while (!fcomputers.EndOfStream)
        //     computers.Add(fcomputers.ReadLine()!);

        // fcomputers.Close();
        // return computers;
        return GetADComputers();
    }

    List<string> GetClasses()
    {
        var fclasses = new StreamReader("./classes");
        List<string> classes = new List<string> { };
        while (!fclasses.EndOfStream)
            classes.Add(fclasses.ReadLine()!);

        fclasses.Close();
        return classes;
    }

    List<string> GetADComputers()
    {
        List<string> computerNames = new List<string>();
        if (!OperatingSystem.IsWindows()) return computerNames;

        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();

        using (DirectoryEntry entry = new DirectoryEntry(@$"LDAP://{domain.Name}"))
        {
            using (DirectorySearcher mySearcher = new DirectorySearcher(entry))
            {
                mySearcher.Filter = ("(objectClass=computer)");
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