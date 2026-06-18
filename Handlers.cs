using System.Net;
using System.Text;
using System.Text.Json;

namespace _20strike;

partial class Application
{
    void ProcessRequest(Dictionary<string, string> request, HttpListenerResponse response)
    {
        // "action" and "target" keys checked by caller!

        if (request["action"] == "read")
        {
            var target = request["target"];

            if (target == "computers")
            {
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Serialize(GetComputers())));
            }
            if (target == "classes")
            {
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Serialize(GetClasses())));
            }
            if (target == "users")
            {
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Serialize(AD.GetUsers())));
            }
            if (target == "info")
            {
                string computername = "";
                string classname = "";
                if (!request.ContainsKey("pc") || string.IsNullOrEmpty(computername = request["pc"])) computername = "*"; // Trying to one-line two checks and assignment
                if (!request.ContainsKey("class") || string.IsNullOrEmpty(classname = request["class"])) classname = "*";
                var data = DBRead(computername, classname);
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Serialize(data)));
            }
        }

        if (request["action"] == "update")
        {
            var target = request["target"];
            if (!taskhandler.AllReady())
            {
                response.OutputStream.Write(Encoding.UTF8.GetBytes($"Already updating {pollerProgress}%"));
            }
            else
            {
                if (!string.IsNullOrEmpty(target))
                    taskhandler.AddAction(() => QueryComputer(target));
                response.OutputStream.Write(Encoding.UTF8.GetBytes("Started update"));
            }
        }

        if (request["action"] == "invoke")
        {
            var computername = request["target"]; // This is still unsafe like hell
            var classname = request["class"];
            var objectname = request["object"];
            var methodname = request["method"];
            string result = InvokeMethod(computername, classname, methodname, objectname);
            response.OutputStream.Write(Encoding.UTF8.GetBytes(result));
        }

        // response.Close(); // Closed by caller, not our business
    }

    public static void ProcessRequestV2(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            // Use AbsolutePath to strip query strings - no ?parameters
            var path = request.Url?.AbsolutePath ?? "";
            if (!path.StartsWith("/v2", StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                WriteRaw(response, "Path not found");
                return;
            }

            var segments = path.AsSpan("/v2".Length)
                .Trim('/')
                .ToString()
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length == 0)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                WriteRaw(response, "Path not found");
                return;
            }

            var method = request.HttpMethod;
            var resource = segments[0].ToLowerInvariant();

            switch (resource)
            {
                case "computers":
                    HandleComputers(context, segments, method);
                    break;
                case "location":
                    HandleLocation(context, segments, method);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WriteRaw(response, "Path not found");
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            WriteRaw(response, ex.Message);
        }
        finally
        {
            response.Close();
        }
    }

    // /v2/computers[/{name}]
    private static void HandleComputers(HttpListenerContext context, string[] segments, string method)
    {
        var response = context.Response;

        switch (method)
        {
            case "OPTIONS":
                response.StatusCode = (int)HttpStatusCode.OK;
                WriteRaw(response, "OK");
                break;

            case "GET":
                if (segments.Length == 2)
                {
                    // GET /v2/computers/{computerName}
                    var computerName = segments[1];
                    var data = Repository.Load($"{computerName}.json");
                    if (data is not null)
                    {
                        WriteJson(response, data);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        WriteRaw(response, "Computer not found");
                    }
                }
                else
                {
                    // GET /v2/computers
                    var computers = GetComputers()
                        .Select(name => new ComputerData(name, Repository.Load($"{name}.json")?.Location));
                    WriteJson(response, computers);
                }
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                WriteRaw(response, "Method not allowed");
                break;
        }
    }

    // /v2/location/{name}
    private static void HandleLocation(HttpListenerContext context, string[] segments, string method)
    {
        var response = context.Response;

        if (segments.Length < 2)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            WriteRaw(response, "Computer name required");
            return;
        }

        var computerName = segments[1];

        switch (method)
        {
            case "OPTIONS":
                response.StatusCode = (int)HttpStatusCode.OK;
                WriteRaw(response, "OK");
                break;

            case "GET":
                var info = Repository.Load($"{computerName}.json");
                var location = info?.Location;
                if (location is null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WriteRaw(response, "Computer not found");
                }
                else
                {
                    WriteJson(response, location);
                }
                break;

            case "POST":
            case "PUT":
                var mapData = JsonSerializer.Deserialize<MapData>(context.Request.InputStream);
                info = Repository.Load($"{computerName}.json");
                if (info is null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WriteRaw(response, "Computer not found");
                }
                else
                {
                    info.Location = mapData;
                    new Repository(info).Save();
                    WriteRaw(response, "Location saved");
                }
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                WriteRaw(response, "Method not allowed");
                break;
        }
    }

    private static void WriteRaw(HttpListenerResponse response, string text)
    {
        var buffer = Encoding.UTF8.GetBytes(text);
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private static void WriteJson(HttpListenerResponse response, object data)
    {
        WriteRaw(response, JsonSerializer.Serialize(data));
    }

    private static string Serialize(object data) => JsonSerializer.Serialize(data);

    public record ComputerData(string Name, MapData? Location);
}
