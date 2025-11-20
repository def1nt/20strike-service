using System.Net;
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
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(Serialize(GetComputers())));
            }
            if (target == "classes")
            {
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(Serialize(GetClasses())));
            }
            if (target == "users")
            {
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(Serialize(AD.GetUsers())));
            }
            if (target == "info")
            {
                string computername = "";
                string classname = "";
                if (!request.ContainsKey("pc") || string.IsNullOrEmpty(computername = request["pc"])) computername = "*"; // Trying to one-line two checks and assignment
                if (!request.ContainsKey("class") || string.IsNullOrEmpty(classname = request["class"])) classname = "*";
                var data = DBRead(computername, classname);
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(Serialize(data)));
            }
        }

        if (request["action"] == "update")
        {
            var target = request["target"];
            if (!taskhandler.AllReady())
            {
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes($"Already updating {pollerProgress}%"));
            }
            else
            {
                if (!string.IsNullOrEmpty(target))
                    taskhandler.AddAction(() => QueryComputer(target));
                response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes("Started update"));
            }
        }

        if (request["action"] == "invoke")
        {
            var computername = request["target"]; // This is still unsafe like hell
            var classname = request["class"];
            var objectname = request["object"];
            var methodname = request["method"];
            string result = InvokeMethod(computername, classname, methodname, objectname);
            response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(result));
        }

        // response.Close(); // Closed by caller, not our business
    }

    public void ProcessRequestV2(HttpListenerContext context)
    {
        var path = context.Request.RawUrl ?? "";
        if (!path.Contains("/v2")) return;
        path = path.Replace("/v2", "").Trim('/');

        string json;
        try
        {
            if (path.StartsWith("computers", StringComparison.CurrentCultureIgnoreCase))   // /computers
            {
                switch (context.Request.HttpMethod)
                {
                    case "OPTIONS":
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        json = "OK";
                        break;
                    case "GET":
                        // If we have computer name
                        if (path.Contains('/') && path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 2)
                        {
                            var computer = path.Replace("computers/", "");
                            var data = Repository.Load($"{computer}.json");
                            if (data is not null)
                            {
                                json = Serialize(data);
                            }
                            else
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                json = "Computer not found";
                            }
                        }
                        // If we don't
                        else
                        {
                            json = Serialize(GetComputers().Select(name => { var data = Repository.Load($"{name}.json"); return new ComputerData(name, data?.Location); }));
                        }
                        break;
                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        json = "Method not allowed";
                        break;
                }
            }
            else if (path.StartsWith("location/", StringComparison.CurrentCultureIgnoreCase))   // /location/computer
            {
                switch (context.Request.HttpMethod)
                {
                    case "OPTIONS":
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        json = "OK";
                        break;
                    case "GET":
                        var computer = path.Replace("location/", "");
                        var info = Repository.Load($"{computer}.json");
                        var location = info?.Location;
                        if (location is null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            json = "Computer not found";
                        }
                        else
                        {
                            json = Serialize(location);
                        }
                        break;
                    case "POST":
                    case "PUT":
                        computer = path.Replace("location/", "");
                        location = JsonSerializer.Deserialize<MapData>(context.Request.InputStream);
                        info = Repository.Load($"{computer}.json");
                        if (info is null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            json = "Computer not found";
                        }
                        else
                        {
                            info.Location = location;
                            new Repository(info).Save();
                            json = "Location saved";
                        }
                        break;
                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        json = "Method not allowed";
                        break;
                }
            }
            else   // unknown path
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                json = "Path not found";
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            json = ex.Message;
        }
        context.Response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(json));
        context.Response.Close();
    }

    private static string Serialize(object data) => JsonSerializer.Serialize(data);

    public record ComputerData(string Name, MapData? Location);
}
