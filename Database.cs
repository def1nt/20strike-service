using Microsoft.Data.Sqlite;
using System.Data;

namespace _20strike;

partial class Application
{
    private int DBInsert(string[] fields)
    {
        if (fields.Length != 5) return 0;
        using SqliteCommand comm = new(@"INSERT INTO data (pc, class, param, type, value) VALUES (@pc, @class, @param, @type, @value)");
        comm.Parameters.AddWithValue("@pc", fields[0]);
        comm.Parameters.AddWithValue("@class", fields[1]);
        comm.Parameters.AddWithValue("@param", fields[2]);
        comm.Parameters.AddWithValue("@type", fields[3]);
        comm.Parameters.AddWithValue("@value", fields[4]);
        comm.Connection = conn;
        return comm.ExecuteNonQuery();
    }

    private void DBCleanup(string Pc, string Class)
    {
        using SqliteCommand comm = new(@"DELETE FROM data WHERE pc=@pc AND class=@class", conn);
        comm.Parameters.AddWithValue("@pc", Pc);
        comm.Parameters.AddWithValue("@class", Class);
        comm.ExecuteNonQuery();
    }

    private object[] DBRead(string computername, string classname)
    {
        if (classname == "brief") { return DBReadBrief(computername); }
        using SqliteCommand comm = new(@"SELECT t.class, t.param, t.value FROM data t WHERE pc LIKE @pc AND class LIKE @class", conn);
        if (computername == "*") { computername = "%"; }
        if (classname == "*") { classname = "%"; }
        comm.Parameters.AddWithValue("@pc", computername);
        comm.Parameters.AddWithValue("@class", classname);
        using SqliteDataReader reader = comm.ExecuteReader();

        if (!reader.HasRows) return [];

        List<Dictionary<string, string>> d = [];

        while (reader.Read())
        {
            Dictionary<string, string> dt = [];
            dt.Add("class", reader.GetString(0));
            dt.Add("param", reader.GetString(1));
            dt.Add("value", reader.GetString(2));
            d.Add(dt);
        }
        return d.ToArray<Dictionary<string, string>>();
    }

    private object[] DBReadBrief(string computername)
    {
        using SqliteCommand comm = new();
        if (computername == "*") { computername = "%"; }
        comm.CommandText = @$"SELECT t.class, t.param, t.value FROM data t
JOIN schema_tab sc ON sc.class=t.class AND sc.param=t.param
WHERE pc LIKE @pc";
        comm.Parameters.AddWithValue("@pc", computername);
        comm.Connection = conn;
        using SqliteDataReader reader = comm.ExecuteReader();

        if (!reader.HasRows) return [];

        List<Dictionary<string, string>> d = [];

        while (reader.Read())
        {
            Dictionary<string, string> dt = [];
            dt.Add("class", reader.GetString(0));
            dt.Add("param", reader.GetString(1));
            dt.Add("value", reader.GetString(2));
            d.Add(dt);
        }
        return d.ToArray<Dictionary<string, string>>();
    }

    private void PruneData()
    {
        var computerNamesFact = GetComputers();

        string query = "SELECT DISTINCT pc FROM data";
        var computerNamesSaved = new List<string>();
        using (var cmd = new SqliteCommand(query, conn))
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                computerNamesSaved.Add(reader.GetString(0));
            }
        }

        var computerNamesToDelete = computerNamesSaved.Except(computerNamesFact);
        foreach (var computerName in computerNamesToDelete)
        {
            query = "DELETE FROM data WHERE pc = @pc";
            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@pc", computerName);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqliteCommand("VACUUM", conn))
        {
            cmd.ExecuteNonQuery();
        }
    }
}
