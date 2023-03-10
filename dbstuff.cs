using Microsoft.Data.Sqlite;
using System.Data;

namespace _20strike;

partial class Application
{
    static DataRowCollection rows = null!;

    void doconnect()
    {
        var conn = new SqliteConnection();
        conn.Open();
        conn.Close();
    }

    int dbinsert(string?[] fields)
    {
        SqliteCommand comm = new SqliteCommand();
        comm.CommandText = @"INSERT INTO data VALUES ($pc, $class, $param, $type, $value)";
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

    void cleanup(string Pc, string Class)
    {
        SqliteCommand comm = new SqliteCommand();
        comm.CommandText = @"DELETE FROM data WHERE pc=$pc AND class=$class";
        comm.Parameters.AddWithValue("$pc", Pc);
        comm.Parameters.AddWithValue("$class", Class);
        comm.Connection = conn;
        comm.ExecuteNonQuery();
    }

    object[] read(string computername, string classname)
    {
        if (classname == "brief") { return readBrief(computername); }
        SqliteCommand comm = new SqliteCommand();
        if (computername == "*") { computername = "%"; }
        if (classname == "*") { classname = "%"; }
        comm.CommandText = @"SELECT t.class, t.param, t.value FROM data t WHERE pc LIKE $pc AND class LIKE $class";
        comm.Parameters.AddWithValue("$pc", computername);
        comm.Parameters.AddWithValue("$class", classname);
        comm.Connection = conn;
        SqliteDataReader reader = comm.ExecuteReader();
        int c = reader.FieldCount;
        if (!reader.HasRows) return new object[]{};

        List<Dictionary<string, string>> d = new List<Dictionary<string, string>>(){};

        if (rows == null) rows = reader.GetSchemaTable().Rows; // Very slow so cached in global static

        while (reader.Read())
        {
            Dictionary<string, string> dt = new Dictionary<string, string>{};

            for (int i = 0; i < c; i++)
            {
                dt.Add((rows[i])[0].ToString()!, reader.GetString(i));
            }
            d.Add(dt);
        }
        reader.Close();
        return d.ToArray<Dictionary<string, string>>();
    }

    object[] readBrief(string computername)
    {
        SqliteCommand comm = new SqliteCommand();
        if (computername == "*") { computername = "%"; }
        comm.CommandText = @$"select t.class, t.param, t.value from data t
join schema_tab sc on sc.class=t.class and sc.param=t.param
where pc LIKE $pc";
        comm.Parameters.AddWithValue("$pc", computername);
        comm.Connection = conn;
        SqliteDataReader reader = comm.ExecuteReader();
        int c = reader.FieldCount;
        if (!reader.HasRows) return new object[]{};

        List<Dictionary<string, string>> d = new List<Dictionary<string, string>>(){};

        if (rows == null) rows = reader.GetSchemaTable().Rows; // Very slow so cached in global static

        while (reader.Read())
        {
            Dictionary<string, string> dt = new Dictionary<string, string>{};

            for (int i = 0; i < c; i++)
            {
                dt.Add((rows[i])[0].ToString()!, reader.GetString(i));
            }
            d.Add(dt);
        }
        reader.Close();
        return d.ToArray<Dictionary<string, string>>();
    }

}