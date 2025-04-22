using System.Text.Json;

namespace _20strike;

public class Repository(ComputerInfo data)
{
    private readonly ComputerInfo data = data;
    private static readonly string path = Path.Join(Directory.GetCurrentDirectory(), "data");
    private static readonly JsonSerializerOptions options = new() { WriteIndented = true, IncludeFields = true };

    public void Save()
    {
        if (data.Name == null) return;
        SaveTo(data.Name + ".json");
    }

    private void SaveTo(string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var olddata = Load(filename);
        var newdata = Merge(olddata ?? new(data.Name));
        File.WriteAllText(Path.Join(path, filename), JsonSerializer.Serialize(newdata, options));
    }

    private static ComputerInfo? Load(string filename)
    {
        if (!File.Exists(Path.Join(path, filename))) return null;
        return JsonSerializer.Deserialize<ComputerInfo>(File.ReadAllText(Path.Join(path, filename)), options)!;
    }

    private ComputerInfo Merge(ComputerInfo old)
    {
        if (data.BIOS != null) old.BIOS = data.BIOS;
        if (data.ComputerSystem != null) old.ComputerSystem = data.ComputerSystem;
        if (data.OperatingSystem != null) old.OperatingSystem = data.OperatingSystem;
        if (data.Processor != null) old.Processor = data.Processor;
        if (data.PhysicalMemory != null) old.PhysicalMemory = data.PhysicalMemory;
        if (data.PhysicalDisk != null) old.PhysicalDisk = data.PhysicalDisk;
        if (data.LogicalDisk != null) old.LogicalDisk = data.LogicalDisk;
        if (data.VideoController != null) old.VideoController = data.VideoController;
        if (data.Monitor != null) old.Monitor = data.Monitor;
        if (data.NetworkAdapter != null) old.NetworkAdapter = data.NetworkAdapter;
        if (data.Printer != null) old.Printer = data.Printer;
        if (data.Software != null) old.Software = data.Software;
        if (data.Process != null) old.Process = data.Process;
        return old;
    }
}
