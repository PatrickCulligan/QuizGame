using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbFile = args.Length > 0 ? args[0] : "quizgame.db";
if (!File.Exists(dbFile))
{
    Console.Error.WriteLine($"Database file not found: {dbFile}");
    return;
}

try
{
    using var conn = new SqliteConnection($"Data Source={dbFile}");
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "ALTER TABLE Players ADD COLUMN IsAdmitted INTEGER NOT NULL DEFAULT 0;";
    cmd.ExecuteNonQuery();
    Console.WriteLine("Column IsAdmitted added successfully (or already present).");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Failed to alter table: " + ex.Message);
}
