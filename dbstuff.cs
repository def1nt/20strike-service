using Microsoft.Data.Sqlite;
using System.Data;

namespace _20strike;

partial class Application
{
    static DataRowCollection rows = null!;

    private int DBInsert(string?[] fields)
    {
        SqliteCommand comm = new(@"INSERT INTO data VALUES ($pc, $class, $param, $type, $value)");
        comm.Parameters.AddWithValue("$pc", fields[0]);
        comm.Parameters.AddWithValue("$class", fields[1]);
        comm.Parameters.AddWithValue("$param", fields[2]);
        comm.Parameters.AddWithValue("$type", fields[3]);
        comm.Parameters.AddWithValue("$value", fields[4]);
        comm.Connection = conn;
        return comm.ExecuteNonQuery();
    }

    // void merge()
    // {
    //     SqliteCommand comm = new SqliteCommand();
    //     comm.CommandText = @"INSERT INTO data (pc, class, param, type, value)
    //             SELECT pc, class, param, type, value FROM temp;
    //             DELETE FROM temp";
    //     comm.Connection = conn;
    //     comm.ExecuteNonQuery();
    // }

    private void DBCleanup(string Pc, string Class)
    {
        SqliteCommand comm = new(@"DELETE FROM data WHERE pc=$pc AND class=$class", conn);
        comm.Parameters.AddWithValue("$pc", Pc);
        comm.Parameters.AddWithValue("$class", Class);
        comm.ExecuteNonQuery();
    }

    private object[] DBRead(string computername, string classname)
    {
        if (classname == "brief") { return DBReadBrief(computername); }
        SqliteCommand comm = new(@"SELECT t.class, t.param, t.value FROM data t WHERE pc LIKE $pc AND class LIKE $class", conn);
        if (computername == "*") { computername = "%"; }
        if (classname == "*") { classname = "%"; }
        comm.Parameters.AddWithValue("$pc", computername);
        comm.Parameters.AddWithValue("$class", classname);
        SqliteDataReader reader = comm.ExecuteReader();
        int c = reader.FieldCount;
        if (!reader.HasRows) return [];

        List<Dictionary<string, string>> d = [];

        rows ??= reader.GetSchemaTable().Rows; // Very slow so cached in global static

        while (reader.Read())
        {
            Dictionary<string, string> dt = [];

            for (int i = 0; i < c; i++)
            {
                dt.Add(rows[i][0].ToString()!, reader.GetString(i));
            }
            d.Add(dt);
        }
        reader.Close();
        return d.ToArray<Dictionary<string, string>>();
    }

    private object[] DBReadBrief(string computername)
    {
        SqliteCommand comm = new();
        if (computername == "*") { computername = "%"; }
        comm.CommandText = @$"SELECT t.class, t.param, t.value FROM data t
JOIN schema_tab sc ON sc.class=t.class AND sc.param=t.param
WHERE pc LIKE $pc";
        comm.Parameters.AddWithValue("$pc", computername);
        comm.Connection = conn;
        SqliteDataReader reader = comm.ExecuteReader();
        int c = reader.FieldCount;
        if (!reader.HasRows) return [];

        List<Dictionary<string, string>> d = [];

        rows ??= reader.GetSchemaTable().Rows; // Very slow so cached in global static

        while (reader.Read())
        {
            Dictionary<string, string> dt = [];

            for (int i = 0; i < c; i++)
            {
                dt.Add(rows[i][0].ToString()!, reader.GetString(i));
            }
            d.Add(dt);
        }
        reader.Close();
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
            query = $"DELETE FROM data WHERE pc = '{computerName}'";
            using var cmd = new SqliteCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqliteCommand("VACUUM", conn))
        {
            cmd.ExecuteNonQuery();
        }
    }
}
