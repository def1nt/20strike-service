using System.Text.Json;

namespace _20strike;

public class Repository(ComputerInfo data)
{
    private readonly ComputerInfo data = data;
    private readonly string path = Path.Join(Directory.GetCurrentDirectory(), "data");
    private static readonly JsonSerializerOptions options = new() { WriteIndented = true, IncludeFields = true };

    public void Save()
    {
        SaveTo(data.ComputerSystem?.Name + ".json");
    }

    public void SaveTo(string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        File.WriteAllText(Path.Join(path, filename), JsonSerializer.Serialize(data, options));
    }
}
