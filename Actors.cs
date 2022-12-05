using System.Management;

namespace _20strike;

partial class Application
{
    void InvokeMethod(string Computer, string Class, string Method, string Object = "")
    {
        if (!OperatingSystem.IsWindows()) return;
        var mp = new ManagementPath($@"\\{Computer}\root\cimv2:{Class}");
        var mc = new ManagementClass(mp);
        ManagementObjectCollection mo;
        try
        {
            mo = mc.GetInstances();
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e.Message);
            return;
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
                try { o.InvokeMethod(Method, new object[] { }); }
                catch (Exception e)
                {
                    System.Console.WriteLine("ERROR: " + e.Message);
                }
            }
        }
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