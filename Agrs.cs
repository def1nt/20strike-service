using System.Diagnostics;

namespace _20strike;

partial class Application
{
    public static void ParseArgs()
    {
        string[] args = Environment.CommandLine.Split();
        if (args.Length == 1)
        {
            ShowHelpAndExit();
        }
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "help": ShowHelpAndExit(); return;
                case "start": return;
                case "register": _ = RegisterService(); return;
                case "unregister": _ = UnregisterService(); return;
                default:
                    Console.WriteLine($"Unkown command: '{args[i]}', terminating");
                    ShowHelpAndExit();
                    break;
            }
        }
    }

    public static void ShowHelpAndExit()
    {
        Console.WriteLine("20-strike backend service");
        Console.WriteLine("Command line parameters:");
        Console.WriteLine("help \t\t : or without args to display this");
        Console.WriteLine("start \t\t : actually start the service");
        Console.WriteLine("register \t : register system service");
        Console.WriteLine("unregister \t : unregister system service");
        Environment.Exit(1);
        return;
    }

    public static async Task RegisterService()
    {
        var p = new Process
        {
            StartInfo =
            {
                FileName = "sc.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = $"create 20strike binPath=\"{Environment.CommandLine.Split()[0]} start\" start= auto"
            }
        };
        if (p.Start())
        {
            await p.WaitForExitAsync();
            if (p.ExitCode == 0)
            {
                Console.WriteLine("Probably registered...\nIMPORTANT: go to services and check login credentials\nexiting...");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Registration failed, probably check for current user rights and run escalated");
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("Could not start cmd or sc.exe to register the service.");
            Environment.Exit(1);
        }
    }

    public static async Task UnregisterService()
    {
        var p = new Process
        {
            StartInfo =
            {
                FileName = "sc.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = "delete 20strike"
            }
        };
        if (p.Start())
        {
            await p.WaitForExitAsync();
            if (p.ExitCode == 0)
            {
                Console.WriteLine("Probably unregistered, exiting...");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Unregistration failed, probably check for current user rights and run escalated");
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("Could not start cmd or sc.exe to unregister the service.");
            Environment.Exit(1);
        }
    }
}
