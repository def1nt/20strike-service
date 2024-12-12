using System.Management;

namespace _20strike;

partial class Application
{
    private static string InvokeMethod(string Computer, string Class, string Method, string Object = "")
    {
        if (!OperatingSystem.IsWindows()) return "Wrong OS";
        var mp = new ManagementPath($@"\\{Computer}\root\cimv2:{Class}");
        var mc = new ManagementClass(mp);
        ManagementObjectCollection mo;
        try
        {
            mo = mc.GetInstances();
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR: " + e.Message);
            return e.Message;
        }

        foreach (ManagementObject o in mo)
        {
            if (ContainsPropWithValue(o.Properties,
                    Class switch
                    {
                        "Win32_Service" => "Name",
                        _ => ""
                    },
                    Object))
            {
                try { _ = o.InvokeMethod(Method, []); return o.ToString(); }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                    return e.Message;
                }
            }
        }
        return "Empty result";
    }

    private static bool ContainsPropWithValue(PropertyDataCollection props, string prop, string value)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (prop == "") return true;
        foreach (var property in props)
        {
            if (property.Name == prop && property.Value != null && GetFromCIMObject(property.Type, property.Value) == value) return true;
        }
        return false;
    }
}
