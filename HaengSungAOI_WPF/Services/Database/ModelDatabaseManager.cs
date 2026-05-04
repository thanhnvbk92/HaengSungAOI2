using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Database
{
    public class ModelDatabaseManager : IModelDatabaseManager
    {
        private const string DatabaseFileName = "Models.db";
        private readonly string _connectionString;

        // Column definitions: name -> SQL type with default
        private static readonly Dictionary<string, string> ColumnDefinitions = new Dictionary<string, string>
        {
            // Core fields
            {"Id", "INTEGER PRIMARY KEY AUTOINCREMENT"},
            {"Name", "TEXT NOT NULL UNIQUE"},
            {"Description", "TEXT"},
            {"CreatedDate", "DATETIME DEFAULT CURRENT_TIMESTAMP"},
            {"ModifiedDate", "DATETIME DEFAULT CURRENT_TIMESTAMP"},
            {"IsActive", "BOOLEAN DEFAULT 0"},
            {"VisionSolutionPath", "TEXT DEFAULT ''"},
            {"VisionSolutionName", "TEXT DEFAULT 'Default.SOL'"},
        };

        // Float columns with their default values (all REAL type)
        private static readonly Dictionary<string, float> FloatColumnDefaults = new Dictionary<string, float>
        {
            // Infeed Robot Z positions
            {"PCBInfeedPick_Z", 0}, {"PCBInfeedPlace_Z", 0},
            // Infeed Robot speeds (legacy)
            {"PCBInfeedPick_Speed", 1000}, {"PCBInfeedPlace_Speed", 1000},
            {"PCBInfeedPick_Acceleration", 0.1f}, {"PCBInfeedPlace_Acceleration", 0.1f},
            {"PCBInfeedPick_Deceleration", 0.1f}, {"PCBInfeedPlace_Deceleration", 0.1f},
            // Infeed positions
            {"PCBInfeed_IdleX", 0}, {"PCBInfeed_IdleY", 0}, {"PCBInfeed_IdleZ", 50}, {"PCBInfeed_IdleR", 0},
            {"PCBInfeed_PickupX", 100}, {"PCBInfeed_PickupY", 100}, {"PCBInfeed_PickupZ", 10}, {"PCBInfeed_PickupR", 0},
            {"PCBInfeed_PreparePlaceX", 200}, {"PCBInfeed_PreparePlaceY", 200}, {"PCBInfeed_PreparePlaceZ", 50}, {"PCBInfeed_PreparePlaceR", 0},
            {"PCBInfeed_PlaceX", 200}, {"PCBInfeed_PlaceY", 200}, {"PCBInfeed_PlaceZ", 10}, {"PCBInfeed_PlaceR", 0},
            // Infeed individual speeds
            {"PCBInfeed_Idle_SpeedX", 1000}, {"PCBInfeed_Idle_SpeedY", 1000}, {"PCBInfeed_Idle_SpeedR", 1000},
            {"PCBInfeed_Idle_Accel", 0.1f}, {"PCBInfeed_Idle_Decel", 0.1f},
            {"PCBInfeed_Pickup_SpeedX", 1000}, {"PCBInfeed_Pickup_SpeedY", 1000}, {"PCBInfeed_Pickup_SpeedR", 1000},
            {"PCBInfeed_Pickup_Accel", 0.1f}, {"PCBInfeed_Pickup_Decel", 0.1f},
            {"PCBInfeed_Place_SpeedX", 1000}, {"PCBInfeed_Place_SpeedY", 1000}, {"PCBInfeed_Place_SpeedR", 1000},
            {"PCBInfeed_Place_Accel", 0.1f}, {"PCBInfeed_Place_Decel", 0.1f},
            // Transfer positions
            {"PCBTransfer_IdleX", 0}, {"PCBTransfer_IdleZ", 50},
            {"PCBTransfer_PreparePickupX", 120}, {"PCBTransfer_PreparePickupZ", 30},
            {"PCBTransfer_PickupX", 150}, {"PCBTransfer_PickupZ", 10},
            {"PCBTransfer_PreparePlaceX", 220}, {"PCBTransfer_PreparePlaceZ", 30},
            {"PCBTransfer_PlaceX", 250}, {"PCBTransfer_PlaceZ", 10},
            {"PCBTransfer_NGX", 200}, {"PCBTransfer_NGZ", 135},
            {"PCBTransfer_Speed", 1000}, {"PCBTransfer_Acceleration", 0.1f}, {"PCBTransfer_Deceleration", 0.1f},
            // Transfer individual speeds
            {"PCBTransfer_Idle_SpeedX", 1000}, {"PCBTransfer_Idle_SpeedZ", 1000},
            {"PCBTransfer_Idle_Accel", 0.1f}, {"PCBTransfer_Idle_Decel", 0.1f},
            {"PCBTransfer_PreparePickup_SpeedX", 1000}, {"PCBTransfer_PreparePickup_SpeedZ", 1000},
            {"PCBTransfer_PreparePickup_Accel", 0.1f}, {"PCBTransfer_PreparePickup_Decel", 0.1f},
            {"PCBTransfer_Pickup_SpeedX", 1000}, {"PCBTransfer_Pickup_SpeedZ", 1000},
            {"PCBTransfer_Pickup_Accel", 0.1f}, {"PCBTransfer_Pickup_Decel", 0.1f},
            {"PCBTransfer_PreparePlace_SpeedX", 1000}, {"PCBTransfer_PreparePlace_SpeedZ", 1000},
            {"PCBTransfer_PreparePlace_Accel", 0.1f}, {"PCBTransfer_PreparePlace_Decel", 0.1f},
            {"PCBTransfer_Place_SpeedX", 1000}, {"PCBTransfer_Place_SpeedZ", 1000},
            {"PCBTransfer_Place_Accel", 0.1f}, {"PCBTransfer_Place_Decel", 0.1f},
            {"PCBTransfer_NG_SpeedX", 1000}, {"PCBTransfer_NG_SpeedZ", 1000},
            {"PCBTransfer_NG_Accel", 0.1f}, {"PCBTransfer_NG_Decel", 0.1f},
            // Outfeed positions
            {"PCBOutfeed_IdleX", 0}, {"PCBOutfeed_IdleY", 0}, {"PCBOutfeed_IdleZ", 50},
            {"PCBOutfeed_PickupX", 200}, {"PCBOutfeed_PickupY", 200}, {"PCBOutfeed_PickupZ", 10},
            {"PCBOutfeed_PlaceOK1X", 300}, {"PCBOutfeed_PlaceOK1Y", 300}, {"PCBOutfeed_PlaceOK1Z", 10},
            {"PCBOutfeed_PlaceOK2X", 300}, {"PCBOutfeed_PlaceOK2Y", 300}, {"PCBOutfeed_PlaceOK2Z", 10},
            {"PCBOutfeed_PlaceOK3X", 300}, {"PCBOutfeed_PlaceOK3Y", 300}, {"PCBOutfeed_PlaceOK3Z", 10},
            {"PCBOutfeed_PlaceOK4X", 300}, {"PCBOutfeed_PlaceOK4Y", 300}, {"PCBOutfeed_PlaceOK4Z", 10},
            {"PCBOutfeed_PlaceOK5X", 300}, {"PCBOutfeed_PlaceOK5Y", 300}, {"PCBOutfeed_PlaceOK5Z", 10},
            {"PCBOutfeed_PlaceOK6X", 300}, {"PCBOutfeed_PlaceOK6Y", 300}, {"PCBOutfeed_PlaceOK6Z", 10},
            {"PCBOutfeed_PlaceNGX", 400}, {"PCBOutfeed_PlaceNGY", 400}, {"PCBOutfeed_PlaceNGZ", 10},
            {"PCBOutfeed_PickupTrayX", 500}, {"PCBOutfeed_PickupTrayY", 500}, {"PCBOutfeed_PickupTrayZ", 10},
            {"PCBOutfeed_PlaceTrayX", 600}, {"PCBOutfeed_PlaceTrayY", 600}, {"PCBOutfeed_PlaceTrayZ", 10},
            {"PCBOutfeed_PlaceX", 300}, {"PCBOutfeed_PlaceY", 300}, {"PCBOutfeed_PlaceZ", 10},
            // Outfeed individual speeds
            {"PCBOutfeed_Idle_SpeedX", 1000}, {"PCBOutfeed_Idle_SpeedY", 1000},
            {"PCBOutfeed_Idle_Accel", 0.1f}, {"PCBOutfeed_Idle_Decel", 0.1f},
            {"PCBOutfeed_Pickup_SpeedX", 1000}, {"PCBOutfeed_Pickup_SpeedY", 1000},
            {"PCBOutfeed_Pickup_Accel", 0.1f}, {"PCBOutfeed_Pickup_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK1_SpeedX", 1000}, {"PCBOutfeed_PlaceOK1_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK1_Accel", 0.1f}, {"PCBOutfeed_PlaceOK1_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK2_SpeedX", 1000}, {"PCBOutfeed_PlaceOK2_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK2_Accel", 0.1f}, {"PCBOutfeed_PlaceOK2_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK3_SpeedX", 1000}, {"PCBOutfeed_PlaceOK3_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK3_Accel", 0.1f}, {"PCBOutfeed_PlaceOK3_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK4_SpeedX", 1000}, {"PCBOutfeed_PlaceOK4_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK4_Accel", 0.1f}, {"PCBOutfeed_PlaceOK4_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK5_SpeedX", 1000}, {"PCBOutfeed_PlaceOK5_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK5_Accel", 0.1f}, {"PCBOutfeed_PlaceOK5_Decel", 0.1f},
            {"PCBOutfeed_PlaceOK6_SpeedX", 1000}, {"PCBOutfeed_PlaceOK6_SpeedY", 1000},
            {"PCBOutfeed_PlaceOK6_Accel", 0.1f}, {"PCBOutfeed_PlaceOK6_Decel", 0.1f},
            {"PCBOutfeed_PlaceNG_SpeedX", 1000}, {"PCBOutfeed_PlaceNG_SpeedY", 1000},
            {"PCBOutfeed_PlaceNG_Accel", 0.1f}, {"PCBOutfeed_PlaceNG_Decel", 0.1f},
            {"PCBOutfeed_PickupTray_SpeedX", 1000}, {"PCBOutfeed_PickupTray_SpeedY", 1000},
            {"PCBOutfeed_PickupTray_Accel", 0.1f}, {"PCBOutfeed_PickupTray_Decel", 0.1f},
            {"PCBOutfeed_PlaceTray_SpeedX", 1000}, {"PCBOutfeed_PlaceTray_SpeedY", 1000},
            {"PCBOutfeed_PlaceTray_Accel", 0.1f}, {"PCBOutfeed_PlaceTray_Decel", 0.1f},
            // Inspect 1
            {"Inspect1_Focus1", 0}, {"Inspect1_Focus2", 5}, {"Inspect1_Focus3", 10},
            {"Inspect1_Rotate1", 0}, {"Inspect1_Rotate2", 90}, {"Inspect1_Rotate3", 180},
            {"Inspect1_IdleZ", 0}, {"Inspect1_IdleR", 0},
            {"Inspect1_Speed", 1000}, {"Inspect1_AccTime", 0.1f}, {"Inspect1_DecTime", 0.1f},
            {"Inspect1_Idle_SpeedZ", 1000}, {"Inspect1_Idle_SpeedC", 1000},
            {"Inspect1_Idle_Accel", 0.1f}, {"Inspect1_Idle_Decel", 0.1f},
            {"Inspect1_Focus1_SpeedZ", 1000}, {"Inspect1_Focus1_SpeedC", 1000},
            {"Inspect1_Focus1_Accel", 0.1f}, {"Inspect1_Focus1_Decel", 0.1f},
            {"Inspect1_Focus2_SpeedZ", 1000}, {"Inspect1_Focus2_SpeedC", 1000},
            {"Inspect1_Focus2_Accel", 0.1f}, {"Inspect1_Focus2_Decel", 0.1f},
            {"Inspect1_Focus3_SpeedZ", 1000}, {"Inspect1_Focus3_SpeedC", 1000},
            {"Inspect1_Focus3_Accel", 0.1f}, {"Inspect1_Focus3_Decel", 0.1f},
            // Inspect 2
            {"Inspect2_Focus1", 0}, {"Inspect2_Focus2", 5}, {"Inspect2_Focus3", 10},
            {"Inspect2_Rotate1", 0}, {"Inspect2_Rotate2", 90}, {"Inspect2_Rotate3", 180},
            {"Inspect2_IdleZ", 0}, {"Inspect2_IdleR", 0},
            {"Inspect2_Speed", 1000}, {"Inspect2_AccTime", 0.1f}, {"Inspect2_DecTime", 0.1f},
            {"Inspect2_Idle_SpeedZ", 1000}, {"Inspect2_Idle_SpeedC", 1000},
            {"Inspect2_Idle_Accel", 0.1f}, {"Inspect2_Idle_Decel", 0.1f},
            {"Inspect2_Focus1_SpeedZ", 1000}, {"Inspect2_Focus1_SpeedC", 1000},
            {"Inspect2_Focus1_Accel", 0.1f}, {"Inspect2_Focus1_Decel", 0.1f},
            {"Inspect2_Focus2_SpeedZ", 1000}, {"Inspect2_Focus2_SpeedC", 1000},
            {"Inspect2_Focus2_Accel", 0.1f}, {"Inspect2_Focus2_Decel", 0.1f},
            {"Inspect2_Focus3_SpeedZ", 1000}, {"Inspect2_Focus3_SpeedC", 1000},
            {"Inspect2_Focus3_Accel", 0.1f}, {"Inspect2_Focus3_Decel", 0.1f},
            // Inspect 2 Unload position
            {"Inspect2_UnloadZ", 0}, {"Inspect2_UnloadC", 0},
            {"Inspect2_Unload_SpeedZ", 1000}, {"Inspect2_Unload_SpeedC", 1000},
            {"Inspect2_Unload_Accel", 0.1f}, {"Inspect2_Unload_Decel", 0.1f},
        };

        public ModelDatabaseManager()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Build CREATE TABLE SQL from column definitions
                var columnDefs = new List<string>();
                foreach (var col in ColumnDefinitions)
                    columnDefs.Add($"{col.Key} {col.Value}");
                foreach (var col in FloatColumnDefaults)
                    columnDefs.Add($"{col.Key} REAL DEFAULT {col.Value}");

                string createTableSql = $"CREATE TABLE IF NOT EXISTS Models ({string.Join(", ", columnDefs)});";

                using (var command = new SQLiteCommand(createTableSql, connection))
                    command.ExecuteNonQuery();

                CheckAndAddMissingColumns(connection);
                CreateDefaultModelIfNotExists(connection);
            }
        }

        private void CheckAndAddMissingColumns(SQLiteConnection connection)
        {
            try
            {
                // Get existing columns
                var existingColumns = new HashSet<string>();
                using (var command = new SQLiteCommand("PRAGMA table_info(Models)", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        existingColumns.Add(reader["name"].ToString());
                }

                // Add missing float columns
                foreach (var col in FloatColumnDefaults)
                {
                    if (!existingColumns.Contains(col.Key))
                    {
                        try
                        {
                            using (var cmd = new SQLiteCommand($"ALTER TABLE Models ADD COLUMN {col.Key} REAL DEFAULT {col.Value}", connection))
                                cmd.ExecuteNonQuery();
                        }
                        catch { /* Column may already exist */ }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelDB] Error in CheckAndAddMissingColumns: {ex.Message}");
            }
        }

        private void CreateDefaultModelIfNotExists(SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Models", connection))
            {
                if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                {
                    var defaultModel = new Models.PCBModel
                    {
                        Name = "Default Model",
                        Description = "Default PCB model configuration",
                        IsActive = true
                    };
                    SaveModelInternal(defaultModel, connection);
                }
            }
        }

        public List<Models.PCBModel> GetAllModels()
        {
            var models = new List<Models.PCBModel>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Models ORDER BY Name", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        models.Add(MapReaderToModel(reader));
                }
            }
            return models;
        }

        public Models.PCBModel GetModelById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Models WHERE Id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapReaderToModel(reader);
                    }
                }
            }
            return null;
        }

        public Models.PCBModel GetActiveModel()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Models WHERE IsActive = 1 LIMIT 1", connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return MapReaderToModel(reader);
                }
            }
            return null;
        }

        public void SaveModel(PCBModel model)
        {
            SaveModelInternal(model, null);
        }

        private void SaveModelInternal(PCBModel model, SQLiteConnection connection = null)
        {
            bool shouldCloseConnection = connection == null;
            if (connection == null)
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
            }

            try
            {
                // Get all saveable columns (exclude Id for insert)
                var columns = GetModelColumns();

                string sql;
                if (model.Id == 0)
                {
                    // INSERT
                    var insertCols = columns.Where(c => c != "Id").ToList();
                    sql = $"INSERT INTO Models ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertCols.Select(c => "@" + c))})";
                }
                else
                {
                    // UPDATE
                    var updateCols = columns.Where(c => c != "Id").ToList();
                    sql = $"UPDATE Models SET {string.Join(", ", updateCols.Select(c => $"{c} = @{c}"))} WHERE Id = @Id";
                }

                using (var command = new SQLiteCommand(sql, connection))
                {
                    AddModelParameters(command, model);
                    int i = command.ExecuteNonQuery();

                    if (model.Id == 0)
                        model.Id = (int)connection.LastInsertRowId;
                }
            }
            finally
            {
                if (shouldCloseConnection)
                    connection.Close();
            }
        }

        public void DeleteModel(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("DELETE FROM Models WHERE Id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetActiveModel(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var cmd1 = new SQLiteCommand("UPDATE Models SET IsActive = 0", connection))
                    cmd1.ExecuteNonQuery();
                using (var cmd2 = new SQLiteCommand("UPDATE Models SET IsActive = 1 WHERE Id = @id", connection))
                {
                    cmd2.Parameters.AddWithValue("@id", id);
                    cmd2.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Get list of all column names for Models table
        /// </summary>
        private List<string> GetModelColumns()
        {
            var columns = ColumnDefinitions.Keys.ToList();
            columns.AddRange(FloatColumnDefaults.Keys);
            return columns;
        }

        /// <summary>
        /// Map SQLiteDataReader to PCBModel using reflection
        /// </summary>
        private Models.PCBModel MapReaderToModel(SQLiteDataReader reader)
        {
            var model = new Models.PCBModel();

            // Map core properties
            model.Id = Convert.ToInt32(reader["Id"]);
            model.Name = reader["Name"]?.ToString() ?? "";
            model.Description = SafeGetString(reader, "Description", "");
            model.CreatedDate = DateTime.Parse(reader["CreatedDate"].ToString());
            model.ModifiedDate = DateTime.Parse(reader["ModifiedDate"].ToString());
            model.IsActive = Convert.ToBoolean(reader["IsActive"]);
            model.VisionSolutionPath = SafeGetString(reader, "VisionSolutionPath", "");
            model.VisionSolutionName = SafeGetString(reader, "VisionSolutionName", "Default.SOL");

            // Map float fields using reflection
            var modelType = typeof(Models.PCBModel);
            foreach (var col in FloatColumnDefaults)
            {
                var value = SafeGetFloat(reader, col.Key, col.Value);

                // Try property first, then field
                var prop = modelType.GetProperty(col.Key);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(model, value);
                }
                else
                {
                    var field = modelType.GetField(col.Key);
                    if (field != null)
                        field.SetValue(model, value);
                }
            }

            return model;
        }

        /// <summary>
        /// Add all model parameters to SQLiteCommand using reflection
        /// </summary>
        private void AddModelParameters(SQLiteCommand command, Models.PCBModel model)
        {
            command.Parameters.AddWithValue("@Id", model.Id);
            command.Parameters.AddWithValue("@Name", model.Name ?? "");
            command.Parameters.AddWithValue("@Description", model.Description ?? "");
            command.Parameters.AddWithValue("@CreatedDate", model.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@IsActive", model.IsActive);
            command.Parameters.AddWithValue("@VisionSolutionPath", model.VisionSolutionPath ?? "");
            command.Parameters.AddWithValue("@VisionSolutionName", model.VisionSolutionName ?? "Default.SOL");

            // Add float parameters using reflection
            var modelType = typeof(Models.PCBModel);
            foreach (var col in FloatColumnDefaults)
            {
                float value = col.Value; // default

                // Try property first, then field
                var prop = modelType.GetProperty(col.Key);
                if (prop != null)
                {
                    value = (float)prop.GetValue(model);
                }
                else
                {
                    var field = modelType.GetField(col.Key);
                    if (field != null)
                        value = (float)field.GetValue(model);
                }

                command.Parameters.AddWithValue("@" + col.Key, value);
            }
        }

        /// <summary>
        /// Safely get string value from reader with default
        /// </summary>
        private string SafeGetString(SQLiteDataReader reader, string column, string defaultValue)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? defaultValue : value.ToString();
            }
            catch { return defaultValue; }
        }

        /// <summary>
        /// Safely get float value from reader with default
        /// </summary>
        private float SafeGetFloat(SQLiteDataReader reader, string column, float defaultValue)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? defaultValue : Convert.ToSingle(value);
            }
            catch { return defaultValue; }
        }
    }
}



