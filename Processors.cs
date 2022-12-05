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
                response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes(serialize(GetComputers())));
            }
            if (target == "classes")
            {
                response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes(serialize(GetClasses())));
            }
            if (target == "users")
            {
                response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes(serialize(GetUsers())));
            }
            if (target == "info")
            {
                string computername = "";
                string classname = "";
                if (!request.ContainsKey("pc") || string.IsNullOrEmpty(computername = request["pc"])) computername = "*"; // Trying to one-line two checks and assignment
                if (!request.ContainsKey("class") || string.IsNullOrEmpty(classname = request["class"])) classname = "*";
                var data = read(computername, classname);
                var bytes = System.Text.ASCIIEncoding.UTF8.GetBytes(serialize(data));
                response.OutputStream.Write(bytes);
            }
        }

        if (request["action"] == "update")
        {
            var target = request["target"];
            if (!taskhandler.AllReady())
            {
                response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes($"Already updating {pollerProgress}%"));
            }
            else
            {
                if (!string.IsNullOrEmpty(target))
                    taskhandler.AddAction(() => QueryComputer(target));
                response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes("Started update"));
            }
        }

        if (request["action"] == "invoke")
        {
            var computername = request["target"]; // This is still unsafe like hell
            var classname = request["class"];
            var objectname = request["object"];
            var methodname = request["method"];
            InvokeMethod(computername, classname, methodname, objectname);
            response.OutputStream.Write(System.Text.ASCIIEncoding.UTF8.GetBytes("Done"));
        }

        // response.Close(); // Closed by caller, not our business
    }

    string serialize(object data) => JsonSerializer.Serialize(data);
}