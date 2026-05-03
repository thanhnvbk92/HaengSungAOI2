using HaengSungAOI_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using VM.Core;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace HaengSungAOI_WPF.Services.Database
{
    /// <summary>
    /// Manages inspection history data using SQLite database with file path references
    /// Images are stored in E:\History\Data\Images
    /// </summary>
    public class InspectionHistoryManager
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly string _imageStoragePath;

        public InspectionHistoryManager()
        {
            // Create database in E:\History\Data directory
            string historyDataFolder = @"E:\History\Data";
            Directory.CreateDirectory(historyDataFolder);
            
            // Create image storage directory
            _imageStoragePath = @"E:\History\Data\Images";
            Directory.CreateDirectory(_imageStoragePath);
            
            _databasePath = Path.Combine(historyDataFolder, "InspectionHistory.db");
            _connectionString = $"Data Source={_databasePath};Version=3;Journal Mode=WAL;BusyTimeout=5000;";
            
            InitializeDatabase();
        }

        /// <summary>
        /// Get the image storage path for saving inspection images
        /// </summary>
        public string ImageStoragePath => _imageStoragePath;

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Create InspectionResults table
                    string createInspectionResultsTable = @"
                        CREATE TABLE IF NOT EXISTS InspectionResults (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            STT INTEGER,
                            PCBCode TEXT NOT NULL,
                            ModelName TEXT,
                            InspectionDateTime TEXT NOT NULL,
                            Result TEXT NOT NULL,
                            TotalDefects INTEGER DEFAULT 0,
                            TotalOK INTEGER DEFAULT 0,
                            TotalNG INTEGER DEFAULT 0,
                            OperatorName TEXT,
                            InspectionTime REAL DEFAULT 0.0,
                            ImagePath TEXT,
                            ReportPath TEXT,
                            CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
                        )";

                    using (var command = new SQLiteCommand(createInspectionResultsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Create DefectResults table
                    string createDefectResultsTable = @"
                        CREATE TABLE IF NOT EXISTS DefectResults (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InspectionResultId INTEGER,
                            Camera TEXT,
                            ErrorType TEXT,
                            Coordinates TEXT,
                            ImagePath TEXT,
                            Status TEXT,
                            Confidence REAL DEFAULT 0.0,
                            Description TEXT,
                            FOREIGN KEY (InspectionResultId) REFERENCES InspectionResults (Id)
                        )";

                    using (var command = new SQLiteCommand(createDefectResultsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Create indexes for better performance
                    string createIndexes = @"
                        CREATE INDEX IF NOT EXISTS idx_inspection_datetime ON InspectionResults (InspectionDateTime DESC);
                        CREATE INDEX IF NOT EXISTS idx_inspection_result ON InspectionResults (Result);
                        CREATE INDEX IF NOT EXISTS idx_inspection_model ON InspectionResults (ModelName);
                        CREATE INDEX IF NOT EXISTS idx_defect_inspection_id ON DefectResults (InspectionResultId);
                    ";

                    using (var command = new SQLiteCommand(createIndexes, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize inspection history database: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save a new inspection result to the database
        /// </summary>
        /// <param name="inspectionResult">The inspection result to save</param>
        /// <returns>The ID of the saved inspection result</returns>
        public int SaveInspectionResult(InspectionResult inspectionResult)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Generate STT if not set
                            if (inspectionResult.STT == 0)
                            {
                                inspectionResult.STT = GetNextSTT();
                            }

                            // Insert inspection result
                            string insertInspectionSql = @"
                                INSERT INTO InspectionResults 
                                (STT, PCBCode, ModelName, InspectionDateTime, Result, TotalDefects, TotalOK, TotalNG, 
                                 OperatorName, InspectionTime, ImagePath, ReportPath, CreatedDate)
                                VALUES 
                                (@STT, @PCBCode, @ModelName, @InspectionDateTime, @Result, @TotalDefects, @TotalOK, @TotalNG,
                                 @OperatorName, @InspectionTime, @ImagePath, @ReportPath, @CreatedDate);
                                SELECT last_insert_rowid();";

                            int inspectionId;
                            using (var command = new SQLiteCommand(insertInspectionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@STT", inspectionResult.STT);
                                command.Parameters.AddWithValue("@PCBCode", inspectionResult.PCBCode ?? "");
                                command.Parameters.AddWithValue("@ModelName", inspectionResult.ModelName ?? "");
                                command.Parameters.AddWithValue("@InspectionDateTime", inspectionResult.InspectionDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@Result", inspectionResult.Result ?? "");
                                command.Parameters.AddWithValue("@TotalDefects", inspectionResult.TotalDefects);
                                command.Parameters.AddWithValue("@TotalOK", inspectionResult.TotalOK);
                                command.Parameters.AddWithValue("@TotalNG", inspectionResult.TotalNG);
                                command.Parameters.AddWithValue("@OperatorName", inspectionResult.OperatorName ?? "");
                                command.Parameters.AddWithValue("@InspectionTime", inspectionResult.InspectionTime);
                                command.Parameters.AddWithValue("@ImagePath", inspectionResult.ImagePath ?? "");
                                command.Parameters.AddWithValue("@ReportPath", inspectionResult.ReportPath ?? "");
                                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                inspectionId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            inspectionResult.Id = inspectionId;

                            // Insert defects
                            if (inspectionResult.Defects != null && inspectionResult.Defects.Count > 0)
                            {
                                string insertDefectSql = @"
                                    INSERT INTO DefectResults 
                                    (InspectionResultId, Camera, ErrorType, Coordinates, ImagePath, Status, Confidence, Description)
                                    VALUES 
                                    (@InspectionResultId, @Camera, @ErrorType, @Coordinates, @ImagePath, @Status, @Confidence, @Description)";

                                foreach (var defect in inspectionResult.Defects)
                                {
                                    using (var command = new SQLiteCommand(insertDefectSql, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@InspectionResultId", inspectionId);
                                        command.Parameters.AddWithValue("@Camera", defect.Camera ?? "");
                                        command.Parameters.AddWithValue("@ErrorType", defect.ErrorType ?? "");
                                        command.Parameters.AddWithValue("@Coordinates", defect.Coordinates ?? "");
                                        command.Parameters.AddWithValue("@ImagePath", defect.ImagePath ?? "");
                                        command.Parameters.AddWithValue("@Status", defect.Status ?? "");
                                        command.Parameters.AddWithValue("@Confidence", defect.Confidence);
                                        command.Parameters.AddWithValue("@Description", defect.Description ?? "");

                                        command.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return inspectionId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save inspection result: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update an existing inspection result in the database
        /// </summary>
        /// <param name="inspectionResult">The inspection result to update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateInspectionResult(InspectionResult inspectionResult)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update inspection result
                            string updateInspectionSql = @"
                                UPDATE InspectionResults SET
                                    PCBCode = @PCBCode,
                                    Result = @Result,
                                    TotalDefects = @TotalDefects,
                                    TotalOK = @TotalOK,
                                    TotalNG = @TotalNG,
                                    InspectionTime = @InspectionTime,
                                    ImagePath = @ImagePath,
                                    ReportPath = @ReportPath
                                WHERE Id = @Id";

                            using (var command = new SQLiteCommand(updateInspectionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@PCBCode", inspectionResult.PCBCode ?? "");
                                command.Parameters.AddWithValue("@Result", inspectionResult.Result ?? "");
                                command.Parameters.AddWithValue("@TotalDefects", inspectionResult.TotalDefects);
                                command.Parameters.AddWithValue("@TotalOK", inspectionResult.TotalOK);
                                command.Parameters.AddWithValue("@TotalNG", inspectionResult.TotalNG);
                                command.Parameters.AddWithValue("@InspectionTime", inspectionResult.InspectionTime);
                                command.Parameters.AddWithValue("@ImagePath", inspectionResult.ImagePath ?? "");
                                command.Parameters.AddWithValue("@ReportPath", inspectionResult.ReportPath ?? "");
                                command.Parameters.AddWithValue("@Id", inspectionResult.Id);

                                int rowsAffected = command.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    return false; // No record was updated
                                }
                            }

                            // Delete existing defects for this inspection
                            string deleteDefectsSql = "DELETE FROM DefectResults WHERE InspectionResultId = @InspectionResultId";
                            using (var command = new SQLiteCommand(deleteDefectsSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@InspectionResultId", inspectionResult.Id);
                                command.ExecuteNonQuery();
                            }

                            // Insert updated defects
                            if (inspectionResult.Defects != null && inspectionResult.Defects.Count > 0)
                            {
                                string insertDefectSql = @"
                                    INSERT INTO DefectResults 
                                    (InspectionResultId, Camera, ErrorType, Coordinates, ImagePath, Status, Confidence, Description)
                                    VALUES 
                                    (@InspectionResultId, @Camera, @ErrorType, @Coordinates, @ImagePath, @Status, @Confidence, @Description)";

                                foreach (var defect in inspectionResult.Defects)
                                {
                                    using (var command = new SQLiteCommand(insertDefectSql, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@InspectionResultId", inspectionResult.Id);
                                        command.Parameters.AddWithValue("@Camera", defect.Camera ?? "");
                                        command.Parameters.AddWithValue("@ErrorType", defect.ErrorType ?? "");
                                        command.Parameters.AddWithValue("@Coordinates", defect.Coordinates ?? "");
                                        command.Parameters.AddWithValue("@ImagePath", defect.ImagePath ?? "");
                                        command.Parameters.AddWithValue("@Status", defect.Status ?? "");
                                        command.Parameters.AddWithValue("@Confidence", defect.Confidence);
                                        command.Parameters.AddWithValue("@Description", defect.Description ?? "");

                                        command.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update inspection result: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get inspection results with optional filtering and paging
        /// </summary>
        public List<InspectionResult> GetInspectionResults(DateTime? fromDate = null, DateTime? toDate = null, 
            string result = null, string modelName = null, int limit = 1000, int offset = 0)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var whereClauses = new List<string>();
                    var parameters = new List<SQLiteParameter>();

                    if (fromDate.HasValue)
                    {
                        whereClauses.Add("InspectionDateTime >= @FromDate");
                        parameters.Add(new SQLiteParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    if (toDate.HasValue)
                    {
                        whereClauses.Add("InspectionDateTime <= @ToDate");
                        parameters.Add(new SQLiteParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    if (!string.IsNullOrEmpty(result))
                    {
                        whereClauses.Add("Result = @Result");
                        parameters.Add(new SQLiteParameter("@Result", result));
                    }

                    if (!string.IsNullOrEmpty(modelName))
                    {
                        whereClauses.Add("ModelName = @ModelName");
                        parameters.Add(new SQLiteParameter("@ModelName", modelName));
                    }

                    string whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    string sql = $@"
                        SELECT * FROM InspectionResults 
                        {whereClause}
                        ORDER BY InspectionDateTime DESC 
                        LIMIT @Limit OFFSET @Offset";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                        command.Parameters.AddWithValue("@Limit", limit);
                        command.Parameters.AddWithValue("@Offset", offset);

                        var results = new List<InspectionResult>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var inspectionResult = new InspectionResult
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    STT = Convert.ToInt32(reader["STT"]),
                                    PCBCode = reader["PCBCode"].ToString(),
                                    ModelName = reader["ModelName"] == DBNull.Value ? "" : reader["ModelName"].ToString(),
                                    InspectionDateTime = DateTime.Parse(reader["InspectionDateTime"].ToString()),
                                    Result = reader["Result"].ToString(),
                                    TotalDefects = Convert.ToInt32(reader["TotalDefects"]),
                                    TotalOK = Convert.ToInt32(reader["TotalOK"]),
                                    TotalNG = Convert.ToInt32(reader["TotalNG"]),
                                    OperatorName = reader["OperatorName"] == DBNull.Value ? "" : reader["OperatorName"].ToString(),
                                    InspectionTime = Convert.ToDouble(reader["InspectionTime"]),
                                    ImagePath = reader["ImagePath"] == DBNull.Value ? "" : reader["ImagePath"].ToString(),
                                    ReportPath = reader["ReportPath"] == DBNull.Value ? "" : reader["ReportPath"].ToString()
                                };

                                // Load defects for this inspection result
                                inspectionResult.Defects = GetDefectsForInspection(inspectionResult.Id);

                                results.Add(inspectionResult);
                            }
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get inspection results: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get defects for a specific inspection result
        /// </summary>
        public List<DefectResult> GetDefectsForInspection(int inspectionResultId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT * FROM DefectResults WHERE InspectionResultId = @InspectionResultId ORDER BY Id";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@InspectionResultId", inspectionResultId);

                        var defects = new List<DefectResult>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var defect = new DefectResult
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    InspectionResultId = Convert.ToInt32(reader["InspectionResultId"]),
                                    Camera = reader["Camera"] == DBNull.Value ? "" : reader["Camera"].ToString(),
                                    ErrorType = reader["ErrorType"] == DBNull.Value ? "" : reader["ErrorType"].ToString(),
                                    Coordinates = reader["Coordinates"] == DBNull.Value ? "" : reader["Coordinates"].ToString(),
                                    ImagePath = reader["ImagePath"] == DBNull.Value ? "" : reader["ImagePath"].ToString(),
                                    Status = reader["Status"] == DBNull.Value ? "" : reader["Status"].ToString(),
                                    Confidence = Convert.ToDouble(reader["Confidence"]),
                                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString()
                                };

                                defects.Add(defect);
                            }
                        }

                        return defects;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get defects for inspection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get inspection statistics for a date range
        /// </summary>
        public InspectionStatistics GetStatistics(DateTime? fromDate = null, DateTime? toDate = null, string modelName = null)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var whereClauses = new List<string>();
                    var parameters = new List<SQLiteParameter>();

                    if (fromDate.HasValue)
                    {
                        whereClauses.Add("InspectionDateTime >= @FromDate");
                        parameters.Add(new SQLiteParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    if (toDate.HasValue)
                    {
                        whereClauses.Add("InspectionDateTime <= @ToDate");
                        parameters.Add(new SQLiteParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    if (!string.IsNullOrEmpty(modelName))
                    {
                        whereClauses.Add("ModelName = @ModelName");
                        parameters.Add(new SQLiteParameter("@ModelName", modelName));
                    }

                    string whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    string sql = $@"
                        SELECT 
                            COUNT(*) as TotalInspections,
                            SUM(CASE WHEN Result = 'PASS' THEN 1 ELSE 0 END) as PassCount,
                            SUM(CASE WHEN Result = 'FAIL' THEN 1 ELSE 0 END) as FailCount,
                            SUM(TotalDefects) as TotalDefectsCount,
                            AVG(InspectionTime) as AverageInspectionTime
                        FROM InspectionResults {whereClause}";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new InspectionStatistics
                                {
                                    TotalInspections = Convert.ToInt32(reader["TotalInspections"]),
                                    PassCount = Convert.ToInt32(reader["PassCount"]),
                                    FailCount = Convert.ToInt32(reader["FailCount"]),
                                    TotalDefects = Convert.ToInt32(reader["TotalDefectsCount"]),
                                    AverageInspectionTime = reader["AverageInspectionTime"] == DBNull.Value ? 0.0 : Convert.ToDouble(reader["AverageInspectionTime"])
                                };
                            }
                        }
                    }
                }

                return new InspectionStatistics();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get inspection statistics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete old inspection records (older than specified days)
        /// </summary>
        public int DeleteOldRecords(int daysToKeep)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                            string cutoffDateString = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss");

                            // Get image paths that will be deleted
                            var imagePaths = new List<string>();
                            string getImagePathsSql = "SELECT ImagePath FROM InspectionResults WHERE InspectionDateTime < @CutoffDate AND ImagePath IS NOT NULL AND ImagePath != ''";
                            using (var command = new SQLiteCommand(getImagePathsSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@CutoffDate", cutoffDateString);
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        imagePaths.Add(reader["ImagePath"].ToString());
                                    }
                                }
                            }

                            // Delete defects first (foreign key constraint)
                            string deleteDefectsSql = @"
                                DELETE FROM DefectResults 
                                WHERE InspectionResultId IN (
                                    SELECT Id FROM InspectionResults 
                                    WHERE InspectionDateTime < @CutoffDate
                                )";

                            using (var command = new SQLiteCommand(deleteDefectsSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@CutoffDate", cutoffDateString);
                                command.ExecuteNonQuery();
                            }

                            // Delete inspection results
                            string deleteInspectionsSql = "DELETE FROM InspectionResults WHERE InspectionDateTime < @CutoffDate";

                            int inspectionsDeleted;
                            using (var command = new SQLiteCommand(deleteInspectionsSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@CutoffDate", cutoffDateString);
                                inspectionsDeleted = command.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            // Delete associated image files
                            foreach (var imagePath in imagePaths)
                            {
                                try
                                {
                                    if (File.Exists(imagePath))
                                    {
                                        File.Delete(imagePath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //Console.WriteLine($"Warning: Could not delete image file {imagePath}: {ex.Message}");
                                }
                            }

                            return inspectionsDeleted;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete old records: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get distinct model names from inspection history
        /// </summary>
        public List<string> GetDistinctModelNames()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT DISTINCT ModelName FROM InspectionResults WHERE ModelName IS NOT NULL AND ModelName != '' ORDER BY ModelName";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        var modelNames = new List<string>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                modelNames.Add(reader["ModelName"].ToString());
                            }
                        }
                        return modelNames;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get distinct model names: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate a unique image file path for saving inspection images
        /// </summary>
        /// <param name="pcbCode">PCB code for the inspection</param>
        /// <param name="fileExtension">File extension (e.g., .jpg, .png)</param>
        /// <returns>Full path for the image file</returns>
        public string GenerateImagePath(string pcbCode, string fileExtension = ".jpg")
        {
            try
            {
                string dateFolder = DateTime.Now.ToString("yyyyMMdd");
                string dailyImageFolder = Path.Combine(_imageStoragePath, dateFolder);
                Directory.CreateDirectory(dailyImageFolder);

                string fileName = $"{pcbCode}_{DateTime.Now:HHmmss_fff}{fileExtension}";
                return Path.Combine(dailyImageFolder, fileName);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error generating image path: {ex.Message}");
                return Path.Combine(_imageStoragePath, $"{pcbCode}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}");
            }
        }

        /// <summary>
        /// Save inspection image to the storage directory and return the file path
        /// </summary>
        /// <param name="imageData">Image data as byte array</param>
        /// <param name="pcbCode">PCB code for the inspection</param>
        /// <param name="fileExtension">File extension (e.g., .jpg, .png)</param>
        /// <returns>Full path of the saved image file</returns>
        public string SaveInspectionImage(byte[] imageData, string pcbCode, string fileExtension = ".jpg")
        {
            try
            {
                string imagePath = GenerateImagePath(pcbCode, fileExtension);
                File.WriteAllBytes(imagePath, imageData);
                return imagePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save inspection image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the next available STT (Sequential Transaction Number)
        /// </summary>
        /// <returns>The next STT value</returns>
        public int GetNextSTT()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT COALESCE(MAX(STT), 0) + 1 FROM InspectionResults";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get next STT: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save the final inspection result (called once at the end of the inspection)
        /// </summary>
        /// <param name="inspectionResult">The final inspection result to save</param>
        /// <returns>The ID of the saved inspection result</returns>
        public int SaveFinalInspectionResult(InspectionResult inspectionResult)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check if an inspection result already exists for the given STT
                            string checkSql = "SELECT COUNT(*) FROM InspectionResults WHERE STT = @STT";
                            using (var checkCommand = new SQLiteCommand(checkSql, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@STT", inspectionResult.STT);
                                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                                if (count > 0)
                                {
                                    throw new InvalidOperationException($"An inspection result with STT {inspectionResult.STT} already exists.");
                                }
                            }

                            // Generate STT if not set
                            if (inspectionResult.STT == 0)
                            {
                                inspectionResult.STT = GetNextSTT();
                            }

                            // Insert inspection result
                            string insertInspectionSql = @"
                                INSERT INTO InspectionResults 
                                (STT, PCBCode, ModelName, InspectionDateTime, Result, TotalDefects, TotalOK, TotalNG, 
                                 OperatorName, InspectionTime, ImagePath, ReportPath, CreatedDate)
                                VALUES 
                                (@STT, @PCBCode, @ModelName, @InspectionDateTime, @Result, @TotalDefects, @TotalOK, @TotalNG,
                                 @OperatorName, @InspectionTime, @ImagePath, @ReportPath, @CreatedDate);
                                SELECT last_insert_rowid();";

                            int inspectionId;
                            using (var command = new SQLiteCommand(insertInspectionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@STT", inspectionResult.STT);
                                command.Parameters.AddWithValue("@PCBCode", inspectionResult.PCBCode ?? "");
                                command.Parameters.AddWithValue("@ModelName", inspectionResult.ModelName ?? "");
                                command.Parameters.AddWithValue("@InspectionDateTime", inspectionResult.InspectionDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@Result", inspectionResult.Result ?? "");
                                command.Parameters.AddWithValue("@TotalDefects", inspectionResult.TotalDefects);
                                command.Parameters.AddWithValue("@TotalOK", inspectionResult.TotalOK);
                                command.Parameters.AddWithValue("@TotalNG", inspectionResult.TotalNG);
                                command.Parameters.AddWithValue("@OperatorName", inspectionResult.OperatorName ?? "");
                                command.Parameters.AddWithValue("@InspectionTime", inspectionResult.InspectionTime);
                                command.Parameters.AddWithValue("@ImagePath", inspectionResult.ImagePath ?? "");
                                command.Parameters.AddWithValue("@ReportPath", inspectionResult.ReportPath ?? "");
                                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                inspectionId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            inspectionResult.Id = inspectionId;

                            // Insert defects
                            if (inspectionResult.Defects != null && inspectionResult.Defects.Count > 0)
                            {
                                string insertDefectSql = @"
                                    INSERT INTO DefectResults 
                                    (InspectionResultId, Camera, ErrorType, Coordinates, ImagePath, Status, Confidence, Description)
                                    VALUES 
                                    (@InspectionResultId, @Camera, @ErrorType, @Coordinates, @ImagePath, @Status, @Confidence, @Description)";

                                foreach (var defect in inspectionResult.Defects)
                                {
                                    using (var command = new SQLiteCommand(insertDefectSql, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@InspectionResultId", inspectionId);
                                        command.Parameters.AddWithValue("@Camera", defect.Camera ?? "");
                                        command.Parameters.AddWithValue("@ErrorType", defect.ErrorType ?? "");
                                        command.Parameters.AddWithValue("@Coordinates", defect.Coordinates ?? "");
                                        command.Parameters.AddWithValue("@ImagePath", defect.ImagePath ?? "");
                                        command.Parameters.AddWithValue("@Status", defect.Status ?? "");
                                        command.Parameters.AddWithValue("@Confidence", defect.Confidence);
                                        command.Parameters.AddWithValue("@Description", defect.Description ?? "");

                                        command.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return inspectionId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save final inspection result: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the last inspection result for ID initialization
        /// </summary>
        public InspectionResult GetLastInspectionResult()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT * FROM InspectionResults ORDER BY STT DESC LIMIT 1";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new InspectionResult
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    STT = Convert.ToInt32(reader["STT"]),
                                    PCBCode = reader["PCBCode"].ToString(),
                                    ModelName = reader["ModelName"] == DBNull.Value ? "" : reader["ModelName"].ToString(),
                                    InspectionDateTime = DateTime.Parse(reader["InspectionDateTime"].ToString()),
                                    Result = reader["Result"].ToString(),
                                    TotalDefects = Convert.ToInt32(reader["TotalDefects"]),
                                    TotalOK = Convert.ToInt32(reader["TotalOK"]),
                                    TotalNG = Convert.ToInt32(reader["TotalNG"]),
                                    OperatorName = reader["OperatorName"] == DBNull.Value ? "" : reader["OperatorName"].ToString(),
                                    InspectionTime = Convert.ToDouble(reader["InspectionTime"]),
                                    ImagePath = reader["ImagePath"] == DBNull.Value ? "" : reader["ImagePath"].ToString(),
                                    ReportPath = reader["ReportPath"] == DBNull.Value ? "" : reader["ReportPath"].ToString()
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get last inspection result: {ex.Message}", ex);
            }
        }

        public bool IsInspectionSTTExisted(int stt)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var whereClauses = new List<string>();
                    var parameters = new List<SQLiteParameter>();

                    parameters.Add(new SQLiteParameter("@STT", stt));


                    string sql = $@"
                        SELECT 
                            COUNT(*) as TotalInspections
                        FROM InspectionResults WHERE STT = @STT";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int TotalInspections = Convert.ToInt32(reader["TotalInspections"]);
                                if (TotalInspections > 0) return true;
                                else return false; 
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get inspection statistics: {ex.Message}", ex);
            }
            return false;
        }
    }

    /// <summary>
    /// Statistics summary for inspection results
    /// </summary>
    public class InspectionStatistics
    {
        public int TotalInspections { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int TotalDefects { get; set; }
        public double AverageInspectionTime { get; set; }

        public double PassRate => TotalInspections > 0 ? (double)PassCount / TotalInspections * 100 : 0;
        public double FailRate => TotalInspections > 0 ? (double)FailCount / TotalInspections * 100 : 0;
        public string PassRateString => $"{PassRate:F1}%";
        public string FailRateString => $"{FailRate:F1}%";
        public string AverageInspectionTimeString => $"{AverageInspectionTime:F2}s";
    }
}