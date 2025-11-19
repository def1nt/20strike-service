using Microsoft.Data.Sqlite;
using System.Net;

namespace _20strike;

partial class Application
{
    public Application()
    {
        conn = new SqliteConnection("Data Source=data.db");
        conn.Open();
        taskhandler = new TaskHandler();
    }
    ~Application()
    {
        conn.Close();
    }

    public CancellationToken cancellationToken;
    readonly TaskHandler taskhandler;
    readonly SqliteConnection conn;
    readonly HttpListener listener = new();
    static int pollerProgress = 0;

    public void Stop()
    {
        Console.WriteLine("Stopping properly...");
        listener.Stop();
    }

    public async Task Start()
    {
        ParseArgs();
        _ = AutoUpdate();

        using (var fi = new StreamReader("./prefixes"))
        {
            while (!fi.EndOfStream)
            {
                listener.Prefixes.Add(fi.ReadLine()!);
            }
        }

        listener.Start();
        Console.WriteLine($"Listening at {listener.Prefixes.First()}...");

        while (listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await listener.GetContextAsync(); }
            catch (HttpListenerException) { continue; }
            catch (Exception e) { Console.WriteLine(e.Message); throw; }

            HttpListenerRequest request = context.Request;
            HttpListenerResponse Response = context.Response;

            if (request.RawUrl?.Contains("favicon.ico") ?? false) { Response.Close(); continue; }
            Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
            Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");

            if (request.RawUrl?.Contains("/v2/") ?? false) { ProcessRequestV2(context); continue; }
            Dictionary<string, string> req;
            try
            {
                string reqtext = new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
                Console.WriteLine(reqtext);
                req = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(reqtext)!;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);
                Response.Close();
                continue;
            }
            Response.ContentType = "text/plain";

            if (!req.ContainsKey("action")) { Response.Close(); continue; }
            if (!req.ContainsKey("target")) { Response.Close(); continue; }

            if (req["action"] == "stop")
            {
                Response.Close();
                listener.Close();
            }

            try
            {
                ProcessRequest(req, Response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                try { Response.Close(); }
                catch (Exception e) { Console.WriteLine("ERROR: " + e.Message); }
            }

        }
        listener.Close();
        await taskhandler.WaitAll();
        Console.WriteLine("Stopped.");
    }

    public async Task AutoUpdate()
    {
        PruneData();
        if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday && DateTime.Today.DayOfWeek != DayOfWeek.Saturday)
            await QueryAll();
        Console.WriteLine(DateTime.Now.ToString());
        await Task.Delay(new TimeSpan(3, 0, 0), cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;
        _ = AutoUpdate();
    }
}
