using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using HaengSungAOI_WPF.Models;
using System.Collections.Generic;

namespace HaengSungAOI_WPF.Services.Database
{
    public class AutoVisionDbService
    {
        private readonly string _mmesConnectionString;
        private readonly string _hsmesConnectionString;

        public AutoVisionDbService()
        {
            _mmesConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? "Server=10.7.10.6;Database=mex_mes;User=root;Password=ivihaengsung@1;AllowLoadLocalInfile=true";
            _hsmesConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["HsmesConnection"]?.ConnectionString
                 ?? "User Id=INFINITY21_JSMES;Password=INFINITY21_JSMES;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=10.7.10.56)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=HSEVNPDB)))";

            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.ToString() == "10.224.142.119")
                    {
                        _mmesConnectionString = _mmesConnectionString.Replace("10.7.10.6", "10.224.143.244");
                        _hsmesConnectionString = _hsmesConnectionString.Replace("10.7.10.56", "10.224.143.111");

                        break;
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi DNS, dùng connection string ban đầu
            }
        }

        public async Task<bool> InsertVisionResultAsync(TbAutoVisionResult result)
        {
            MySqlConnection connection = null;
            try
            {
                connection = new MySqlConnection(_mmesConnectionString);
                await connection.OpenAsync();
                var query = @"
                    INSERT INTO tb_auto_vision_result 
                    (pid, machine_id, work_order, station, result, ebr, image_path, inspection_time, tack_time) 
                    VALUES (@pid, @machineId, @workOrder, @station, @result, @ebr, @imagePath, @inspectionTime, @tackTime)";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@pid", result.Pid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@machineId", result.MachineId);
                    cmd.Parameters.AddWithValue("@workOrder", result.WorkOrder ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@station", result.Station ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@result", result.Result ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ebr", result.Ebr ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagePath", result.ImagePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@inspectionTime", result.InspectionTime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@tackTime", result.TackTime ?? (object)DBNull.Value);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0 && !string.IsNullOrEmpty(result.Pid) && !string.IsNullOrEmpty(result.Station))
                    {
                        var checkQuery = @"
                            SELECT COUNT(*) as Total, SUM(CASE WHEN result = 'NG' THEN 1 ELSE 0 END) as NgCount 
                            FROM tb_auto_vision_result 
                            WHERE pid = @pid AND station = @station";

                        int totalRecords = 0;
                        int ngRecords = 0;

                        using (var cmdCheck = new MySqlCommand(checkQuery, connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@pid", result.Pid);
                            cmdCheck.Parameters.AddWithValue("@station", result.Station);

                            using (var reader = await cmdCheck.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalRecords = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                                    ngRecords = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
                                }
                            }
                        }

                        if (totalRecords > 1 && ngRecords > 0)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var scanoutCheckQuery = "SELECT COUNT(*) FROM tb_auto_vision_scanout WHERE pid = @pid";
                                int scanoutExists = 0;

                                using (var cmdScanoutCheck = new MySqlCommand(scanoutCheckQuery, connection))
                                {
                                    cmdScanoutCheck.Parameters.AddWithValue("@pid", result.Pid);
                                    scanoutExists = Convert.ToInt32(await cmdScanoutCheck.ExecuteScalarAsync());
                                }

                                if (scanoutExists > 0)
                                {
                                    var updateQuery = @"
                                        UPDATE tb_auto_vision_scanout 
                                        SET inspection_count = IFNULL(inspection_count, 0) + 1 
                                        WHERE pid = @pid";
                                    using (var cmdUpdate = new MySqlCommand(updateQuery, connection))
                                    {
                                        cmdUpdate.Parameters.AddWithValue("@pid", result.Pid);
                                        await cmdUpdate.ExecuteNonQueryAsync();
                                        break;
                                    }
                                }
                                else
                                {
                                    if (i < 2) await Task.Delay(500);
                                }
                            }
                        }
                    }

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                // TODO: Log exception
                Console.WriteLine($"Error inserting vision result: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<bool> UpdateVisionScanoutAsync(TbAutoVisionScanout scanout)
        {
            MySqlConnection connection = null;
            try
            {
                connection = new MySqlConnection(_mmesConnectionString);
                await connection.OpenAsync();

                // Bước 1: Kiểm tra xem PID đã tồn tại chưa và lấy scanout_status hiện tại
                string selectQuery = "SELECT scanout_status FROM tb_auto_vision_scanout WHERE pid = @pid LIMIT 1";
                string existingStatus = null;
                bool rowExists = false;

                using (var cmdSelect = new MySqlCommand(selectQuery, connection))
                {
                    cmdSelect.Parameters.AddWithValue("@pid", scanout.Pid ?? (object)DBNull.Value);
                    using (var reader = await cmdSelect.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            rowExists = true;
                            existingStatus = reader.IsDBNull(0) ? null : reader.GetString(0);
                        }
                    }
                }

                if (!rowExists)
                {
                    // Bước 2: Chưa có record → INSERT mới
                    string insertQuery = @"
                        INSERT INTO tb_auto_vision_scanout 
                            (pid, scanout_status, error_message, scanout_time, ebr, wo)
                        VALUES 
                            (@pid, @scanoutStatus, @errorMessage, @scanoutTime, @ebr, @wo)";

                    using (var cmdInsert = new MySqlCommand(insertQuery, connection))
                    {
                        cmdInsert.Parameters.AddWithValue("@pid", scanout.Pid ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@scanoutStatus", scanout.ScanoutStatus ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@errorMessage", scanout.ErrorMessage ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@scanoutTime", scanout.ScanoutTime ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@ebr", scanout.ebr ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@wo", scanout.wo ?? (object)DBNull.Value);

                        int rowsAffected = await cmdInsert.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                else if (string.Equals(existingStatus, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    // Bước 3: Đã có record với scanout_status = 'OK' → bỏ qua, không update
                    Console.WriteLine($"[UpdateVisionScanout] PID={scanout.Pid} đã có status=OK, bỏ qua update.");
                    return true;
                }
                else
                {
                    // Bước 4: Đã có record với scanout_status = NG (hoặc NULL) → UPDATE và tăng inspection_count
                    string updateQuery = @"
                        UPDATE tb_auto_vision_scanout
                        SET scanout_time   = @scanoutTime,
                            scanout_status = @scanoutStatus,
                            error_message  = @errorMessage,
                            ebr            = @ebr,
                            wo             = @wo
                        WHERE pid = @pid";

                    using (var cmdUpdate = new MySqlCommand(updateQuery, connection))
                    {
                        cmdUpdate.Parameters.AddWithValue("@pid", scanout.Pid ?? (object)DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@scanoutStatus", scanout.ScanoutStatus ?? (object)DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@errorMessage", scanout.ErrorMessage ?? (object)DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@scanoutTime", scanout.ScanoutTime ?? (object)DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@ebr", scanout.ebr ?? (object)DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@wo", scanout.wo ?? (object)DBNull.Value);

                        int rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateVisionScanoutAsync: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                    await connection.CloseAsync();
                connection?.Dispose();
            }
        }


        public async Task<bool> UpdateVisionResultEbrWoAsync(string pid, string ebr, string wo)
        {
            MySqlConnection connection = null;
            try
            {
                connection = new MySqlConnection(_mmesConnectionString);
                await connection.OpenAsync();
                var query = @"
                    UPDATE tb_auto_vision_result
                    SET    ebr        = @ebr,
                           work_order = @work_order
                    WHERE  pid = @pid";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@pid", pid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ebr", ebr ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@work_order", wo ?? (object)DBNull.Value);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating vision result ebr/wo: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                    await connection.CloseAsync();
                connection?.Dispose();
            }
        }
        public async Task<bool> InsertVisionInputTimeAsync(string pid, int machineId)
        {
            if (string.IsNullOrWhiteSpace(pid)) return false;

            MySqlConnection connection = null;
            try
            {
                connection = new MySqlConnection(_mmesConnectionString);
                await connection.OpenAsync();
                var query = @"
                    INSERT IGNORE INTO tb_auto_vision_scanout (pid, machine_id, input_time) 
                    VALUES (@pid, @machineId, @inputTime)";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@pid", pid);
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    cmd.Parameters.AddWithValue("@inputTime", DateTime.Now);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting initial vision scanout: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                    await connection.CloseAsync();
                connection?.Dispose();
            }
        }

        public async Task<int?> GetActualMachineIdAsync(string machineName)
        {
            var query = "SELECT id FROM tb_auto_vision_machine_list WHERE machine_name = @machineName LIMIT 1";
            using (var connection = new MySqlConnection(_mmesConnectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@machineName", machineName);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && int.TryParse(result.ToString(), out int actualId))
                    {
                        return actualId;
                    }
                }
            }
            return null;
        }

        public async Task<bool> InitializeOperatingSessionAsync(int actualMachineId)
        {
            // .NET 4.8.1: Sử dụng using block truyền thống để đảm bảo đóng kết nối ngay cả khi crash
            using (MySqlConnection connection = new MySqlConnection(_mmesConnectionString))
            {
                await connection.OpenAsync();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;

                        // 1. Start Operating Session
                        string startSessionQuery = @"
                            INSERT INTO tb_auto_vision_operating (machine_id, start_time)
                            SELECT @machineId, @now
                            FROM (SELECT 1) AS dummy
                            WHERE NOT EXISTS (
                                SELECT 1 FROM tb_auto_vision_operating 
                                WHERE machine_id = @machineId AND end_time IS NULL
                                LIMIT 1
                            );";

                        using (MySqlCommand cmdStart = new MySqlCommand(startSessionQuery, connection, transaction))
                        {
                            cmdStart.Parameters.AddWithValue("@machineId", actualMachineId);
                            cmdStart.Parameters.AddWithValue("@now", now);
                            await cmdStart.ExecuteNonQueryAsync();
                        }

                        // 2. Close Active Errors
                        var activeErrorIds = new List<int>();
                        DateTime? earliestStartTime = null;

                        string selectActiveQuery = @"
                            SELECT e.id, e.error_start_time, d.error_type 
                            FROM tb_auto_vision_error_log e
                            LEFT JOIN tb_auto_vision_error_dict d ON e.error_id = d.id
                            WHERE e.machine_id = @machineId AND e.error_end_time IS NULL";

                        using (MySqlCommand cmdSelect = new MySqlCommand(selectActiveQuery, connection, transaction))
                        {
                            cmdSelect.Parameters.AddWithValue("@machineId", actualMachineId);
                            using (var reader = await cmdSelect.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    activeErrorIds.Add(reader.GetInt32(reader.GetOrdinal("id")));
                                    DateTime st = reader.GetDateTime(reader.GetOrdinal("error_start_time"));
                                    string type = reader.IsDBNull(reader.GetOrdinal("error_type")) ? "" : reader.GetString(reader.GetOrdinal("error_type"));

                                    if (!type.Equals("alarm", StringComparison.OrdinalIgnoreCase) && 
                                        !type.Equals("warning", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (earliestStartTime == null || st < earliestStartTime) earliestStartTime = st;
                                    }
                                }
                            }
                        }

                        if (activeErrorIds.Count > 0)
                        {
                            string updateErrorsQuery = @"
                                UPDATE tb_auto_vision_error_log 
                                SET error_end_time = @now, idle_time_minute = TIMESTAMPDIFF(SECOND, error_start_time, @now) / 60.0
                                WHERE machine_id = @machineId AND error_end_time IS NULL";

                            using (MySqlCommand cmdUpdate = new MySqlCommand(updateErrorsQuery, connection, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@machineId", actualMachineId);
                                cmdUpdate.Parameters.AddWithValue("@now", now);
                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            if (App.IsAutoMode && earliestStartTime.HasValue)
                            {
                                string insertDowntimeQuery = @"
                                    INSERT INTO tb_auto_vision_down_time (machine_id, start_time, end_time, down_time_minute)
                                    VALUES (@machineId, @startTime, @now, TIMESTAMPDIFF(SECOND, @startTime, @now) / 60.0);
                                    SELECT LAST_INSERT_ID();";
                                long dtId;
                                using (MySqlCommand cmdDown = new MySqlCommand(insertDowntimeQuery, connection, transaction))
                                {
                                    cmdDown.Parameters.AddWithValue("@machineId", actualMachineId);
                                    cmdDown.Parameters.AddWithValue("@startTime", earliestStartTime.Value);
                                    cmdDown.Parameters.AddWithValue("@now", now);
                                    dtId = Convert.ToInt64(await cmdDown.ExecuteScalarAsync());
                                }
                                foreach (int eid in activeErrorIds)
                                {
                                    string mapQuery = "INSERT INTO tb_auto_vision_error_down_time_mapping (down_time_id, error_log_id) VALUES (@dtId, @eid)";
                                    using (MySqlCommand cmdMap = new MySqlCommand(mapQuery, connection, transaction))
                                    {
                                        cmdMap.Parameters.AddWithValue("@dtId", dtId);
                                        cmdMap.Parameters.AddWithValue("@eid", eid);
                                        await cmdMap.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Error in InitializeOperatingSessionAsync: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> UpdateVisionOperatingEndAsync(int actualMachineId)
        {
            // Sử dụng using block truyền thống cho .NET 4.8.1
            using (MySqlConnection connection = new MySqlConnection(_mmesConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    /* TỐI ƯU: Cập nhật trực tiếp bản ghi đang mở mới nhất.
                       Logic: Tìm bản ghi có start_time lớn nhất mà end_time vẫn đang NULL của máy đó và đóng nó lại.
                       Việc dùng Subquery giúp xác định chính xác dòng cần Update mà không cần SELECT trước.
                    */
                    string updateQuery = @"
                        UPDATE tb_auto_vision_operating 
                        SET end_time = @now, 
                            duration_minute = TIMESTAMPDIFF(MINUTE, start_time, @now)
                        WHERE machine_id = @machineId 
                          AND end_time IS NULL
                        ORDER BY start_time DESC 
                        LIMIT 1;";

                    using (MySqlCommand cmdUpdate = new MySqlCommand(updateQuery, connection))
                    {
                        cmdUpdate.Parameters.AddWithValue("@machineId", actualMachineId);
                        cmdUpdate.Parameters.AddWithValue("@now", DateTime.Now);

                        int rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();

                        // rowsAffected > 0 nghĩa là đã đóng phiên thành công.
                        // Nếu bằng 0, nghĩa là không có phiên nào đang mở để đóng (Đúng ý đồ "bỏ qua" của bạn).
                        if (rowsAffected == 0)
                        {
                            Console.WriteLine(string.Format("No active session found to end for machine ID {0}.", actualMachineId));
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error logging vision operating end: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<Dictionary<string, int>> LoadErrorDictionaryAsync()
        {
            var dict = new Dictionary<string, int>();
            try
            {
                using (var connection = new MySqlConnection(_mmesConnectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT error_code, id FROM tb_auto_vision_error_dict";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                            {
                                dict[reader.GetString(0)] = reader.GetInt32(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading error dictionary: " + ex.Message);
            }
            return dict;
        }

        public async Task<bool> InsertStartVisionErrorAsync(int actualMachineId, ushort errorCode)
        {
            string errorCodeStr = errorCode.ToString();
            int? actualErrorId = null;

            // Chỉ dùng sẵn từ trong dictionary RAM rà soát id lỗi, không Query select DB nữa.
            if (App.ErrorDict != null && App.ErrorDict.ContainsKey(errorCodeStr))
            {
                actualErrorId = App.ErrorDict[errorCodeStr];
            }

            using (MySqlConnection connection = new MySqlConnection(_mmesConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Safe handling: Nếu dictionary không có, tự động insert mã lạ đó vào tb_auto_vision_error_log_dict
                    if (!actualErrorId.HasValue)
                    {
                        try
                        {
                            string insertDictQuery = "INSERT INTO tb_auto_vision_error_dict (error_code, error_description, error_type) VALUES (@errorCode, @error_description, @error_type)";
                            using (var insertCmd = new MySqlCommand(insertDictQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@errorCode", errorCodeStr);
                                insertCmd.Parameters.AddWithValue("@error_description", "Unknown Error");
                                insertCmd.Parameters.AddWithValue("@error_type", "error");
                                await insertCmd.ExecuteNonQueryAsync();

                                actualErrorId = (int)insertCmd.LastInsertedId;

                                // Bổ sung ID mới vào RAM để các lần sau không cần insert nữa
                                if (App.ErrorDict != null)
                                {
                                    App.ErrorDict[errorCodeStr] = actualErrorId.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to auto-insert unknown error code '{errorCodeStr}' into dict: {ex.Message}");
                            return false; // Bỏ qua ghi lỗi nguyên nhân là mã không xác định không insert được
                        }
                    }

                    if (!actualErrorId.HasValue) return false;

                    // Cuối cùng ghi thông tin lỗi log vào bảng tb_auto_vision_error_log
                    string insertQuery = @"
                        INSERT INTO tb_auto_vision_error_log (machine_id, error_id, error_start_time) 
                        VALUES (@machineId, @errorId, @now)";

                    using (MySqlCommand cmdInsert = new MySqlCommand(insertQuery, connection))
                    {
                        cmdInsert.Parameters.AddWithValue("@machineId", actualMachineId);
                        cmdInsert.Parameters.AddWithValue("@errorId", actualErrorId.Value);
                        cmdInsert.Parameters.AddWithValue("@now", DateTime.Now);

                        int rowsAffected = await cmdInsert.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error logging vision error: " + ex.Message);
                    return false;
                }
            }
        }


        public async Task<(bool isSameEbr, string CurrentEbr)> IsSameEbr(string pid, string ebr)
        {
            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(ebr)) return (false,"");

            using (MySqlConnection connection = new MySqlConnection(_mmesConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    string query = "SELECT EBR FROM tb_klas WHERE @pid >= start_sn AND @pid <= end_sn AND LENGTH(@pid) = LENGTH(start_sn) AND LENGTH(@pid) = LENGTH(end_sn) AND ebr = @ebr LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@pid", pid);
                        cmd.Parameters.AddWithValue("@ebr", ebr);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return (true, result.ToString()); ;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error checking IsSameEbr: " + ex.Message);
                }

                return (false, ""); ;
            }
        }

        /// <summary>
        /// Tra cứu EBR của PID từ bảng tb_klas (dùng khi CurrentEbr chưa được set)
        /// </summary>
        public async Task<string> GetEbrForPid(string pid)
        {
            if (string.IsNullOrEmpty(pid)) return null;

            using (MySqlConnection connection = new MySqlConnection(_mmesConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    string query = "SELECT EBR FROM tb_klas WHERE @pid >= start_sn AND @pid <= end_sn AND LENGTH(@pid) = LENGTH(start_sn) AND LENGTH(@pid) = LENGTH(end_sn) LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@pid", pid);
                        var result = await cmd.ExecuteScalarAsync();
                        return result != null && result != DBNull.Value ? result.ToString() : null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in GetEbrForPid: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<(bool isBlock, string reason)> IsBlock(string pid)
        {
            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(_hsmesConnectionString)) return (false, null);

            try
            {
                // LƯU Ý: Vui lòng Cài đặt package qua: Tools -> NuGet Package Manager -> Package Manager Console -> Gõ: Install-Package Oracle.ManagedDataAccess
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_hsmesConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT REASON FROM TBL_BLOCK WHERE PID = :pid AND STATUS = 0";

                    using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(query, connection))
                    {
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value && result.ToString().Contains("Scanout NG hoặc chưa chạy vision -> Thả lại vision"))
                        {
                            return (false, result.ToString());
                        }
                        else if (result != null && result != DBNull.Value)
                        {
                            return (true, result.ToString());
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking IsBlock in Oracle DB: " + ex.Message);
            }

            return (false, null);
        }

        public async Task<(bool isSuccess, string errorMessage)> TestHsmesConnectionAsync()
        {
            if (string.IsNullOrEmpty(_hsmesConnectionString))
                return (false, "Chuỗi kết nối HsmesConnection (Oracle) trống hoặc chưa được cấu hình.");

            try
            {
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_hsmesConnectionString))
                {
                    await connection.OpenAsync();
                    return (true, "Kết nối Oracle DB thành công!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error testing Oracle DB Connection: " + ex.Message);
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }


        public async Task<bool> UpdateBlock(string pid, string reason, string itemCode, string magazineNo, string username)
        {
            if (string.IsNullOrEmpty(_hsmesConnectionString)) return false;

            try
            {
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_hsmesConnectionString))
                {
                    await connection.OpenAsync();

                    // Kiểm tra tồn tại PID
                    string checkQuery = "SELECT 1 FROM TBL_BLOCK WHERE PID = :pid FETCH FIRST 1 ROWS ONLY";
                    bool isExists = false;
                    using (var checkCmd = new Oracle.ManagedDataAccess.Client.OracleCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                        var exists = await checkCmd.ExecuteScalarAsync();
                        if (exists != null && exists != DBNull.Value)
                        {
                            isExists = true;
                        }
                    }

                    if (isExists)
                    {
                        // Update
                        string updateQuery = @"
                            UPDATE TBL_BLOCK SET 
                            RECEIPT_DATE = SYSDATE,
                            RECEIPT_QTY = 1,
                            STATUS = 0,
                            REASON = :reason,
                            ITEM_CODE = :itemCode,
                            MAGAZINE_NO = :magazineNo,
                            USERNAME = :username
                            WHERE PID = :pid";

                        using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(updateQuery, connection))
                        {
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("reason", reason ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("itemCode", itemCode ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("magazineNo", magazineNo ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("username", username ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Insert
                        string insertQuery = @"
                            INSERT INTO TBL_BLOCK 
                            (RECEIPT_DATE, PID, RECEIPT_QTY, STATUS, REASON, ITEM_CODE, MAGAZINE_NO, USERNAME) 
                            VALUES 
                            (SYSDATE, :pid, 1, 0, :reason, :itemCode, :magazineNo, :username)";

                        using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(insertQuery, connection))
                        {
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("reason", reason ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("itemCode", itemCode ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("magazineNo", magazineNo ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("username", username ?? (object)DBNull.Value));
                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UpdateBlock: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> ReleaseBlock(string pid)
        {
            if (string.IsNullOrEmpty(_hsmesConnectionString) || string.IsNullOrEmpty(pid)) return false;

            try
            {
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_hsmesConnectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT REASON FROM TBL_BLOCK WHERE PID = :pid FETCH FIRST 1 ROWS ONLY";
                    string currentReason = "";
                    using (var checkCmd = new Oracle.ManagedDataAccess.Client.OracleCommand(selectQuery, connection))
                    {
                        checkCmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                        var reasonObj = await checkCmd.ExecuteScalarAsync();
                        if (reasonObj != null && reasonObj != DBNull.Value)
                        {
                            currentReason = reasonObj.ToString();
                        }
                    }

                    string newReason = string.IsNullOrEmpty(currentReason) 
                        ? "Auto Scan Out OK" 
                        : currentReason + " - Auto Scan Out OK";

                    string updateQuery = @"
                        UPDATE TBL_BLOCK SET 
                        STATUS = 1,
                        REASON = :reason
                        WHERE PID = :pid";

                    using (var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(updateQuery, connection))
                    {
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("reason", newReason));
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ReleaseBlock: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> IsScanOut(string pid)
        {
            if (string.IsNullOrEmpty(_hsmesConnectionString)) return false;

            try
            {
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_hsmesConnectionString))
                {
                    await connection.OpenAsync();

                    // Kiểm tra tồn tại PID
                    string checkQuery = "SELECT 1 FROM ICOM_PBA_ACTUAL_RESULT WHERE PID = :pid FETCH FIRST 1 ROWS ONLY";
                    bool isExists = false;
                    using (var checkCmd = new Oracle.ManagedDataAccess.Client.OracleCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("pid", pid));
                        var exists = await checkCmd.ExecuteScalarAsync();
                        if (exists != null && exists != DBNull.Value)
                        {
                            isExists = true;
                        }
                    }
                    return isExists;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in checking: " + ex.Message);
                return true;
            }
        }
    }
}
