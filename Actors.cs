using System.Management;

namespace _20strike;

partial class Application
{
    string InvokeMethod(string Computer, string Class, string Method, string Object = "")
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
            System.Console.WriteLine("ERROR: " + e.Message);
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
                try { o.InvokeMethod(Method, new object[] { }); return o.ToString(); }
                catch (Exception e)
                {
                    System.Console.WriteLine("ERROR: " + e.Message);
                    return e.Message;
                }
            }
        }
        return "Empty result";
    }

    bool ContainsPropWithValue(PropertyDataCollection props, string prop, string value)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (prop == "") return true;
        foreach (var property in props)
        {
            if (property.Name == prop && property.Value != null && getFromCIMObject(property.Type, property.Value) == value) return true;
        }
        return false;
    }
}