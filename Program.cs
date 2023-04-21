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
    TaskHandler taskhandler;
    SqliteConnection conn;
    HttpListener listener = new HttpListener();
    static int pollerProgress = 0;

    public void stop()
    {
        System.Console.WriteLine("Stopping properly...");
        listener.Stop();
    }

    public async Task start()
    {
        ParseArgs();
        AutoUpdate();

        using (var fi = new StreamReader("./prefixes"))
        {
            while (!fi.EndOfStream)
            {
                listener.Prefixes.Add(fi.ReadLine()!);
            }
        }

        listener.Start();
        System.Console.WriteLine($"Listening at {listener.Prefixes.First()}...");

        while (listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await listener.GetContextAsync(); }
            catch (HttpListenerException) { continue; }
            catch (Exception e) { System.Console.WriteLine(e.Message); throw; }

            var request = context.Request;
            var Response = context.Response;

            Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
            Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");

            Dictionary<string, string> req;
            try
            {
                string reqtext = (new StreamReader(request.InputStream, request.ContentEncoding)).ReadToEnd();
                System.Console.WriteLine(reqtext);
                req = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(reqtext)!;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR: " + e.Message);
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
                System.Console.WriteLine(e.Message);
            }
            finally
            {
                try { Response.Close(); }
                catch (Exception e) { System.Console.WriteLine("ERROR: " + e.Message); }
            }

        }
        listener.Close();
        await taskhandler.WaitAll();
        System.Console.WriteLine("Stopped.");
    }

    public async void AutoUpdate()
    {
        PruneData();
        if (!new System.DayOfWeek[] { System.DayOfWeek.Saturday, System.DayOfWeek.Sunday }.Contains(DateTime.Today.DayOfWeek))
            await QueryAll();
        System.Console.WriteLine(DateTime.Now.ToString());
        await Task.Delay(new TimeSpan(3, 0, 0), cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;
        AutoUpdate();
    }
}
