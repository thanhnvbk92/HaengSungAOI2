using System;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Database
{
    /// <summary>
    /// Handles recording of inspection results with unique ID management
    /// </summary>
    public class InspectionResultRecorder
    {
        private readonly InspectionHistoryManager _historyManager;
        private static int _nextRecordId = 1;
        private static readonly object _lockObject = new object();

        public InspectionResultRecorder()
        {
            _historyManager = new InspectionHistoryManager();
            InitializeNextRecordId();
        }

        /// <summary>
        /// Initialize the next record ID based on existing records
        /// </summary>
        private void InitializeNextRecordId()
        {
            try
            {
                var lastRecord = _historyManager.GetLastInspectionResult();
                if (lastRecord != null && lastRecord.STT > 0)
                {
                    _nextRecordId = lastRecord.STT + 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing record ID: {ex.Message}");
                _nextRecordId = 1; // Fallback to 1
            }
        }

        /// <summary>
        /// Generate the next unique record ID
        /// </summary>
        /// <returns>Unique integer ID</returns>
        public int GetNextRecordId()
        {
            lock (_lockObject)
            {
                return _nextRecordId++;
            }
        }

        /// <summary>
        /// Create and save an initial inspection record
        /// </summary>
        /// <param name="modelName">Name of the PCB model being inspected</param>
        /// <param name="operatorName">Name of the operator</param>
        /// <returns>The unique record ID</returns>
        public int CreateInitialRecord(string modelName, string operatorName = "admin")
        {
            try
            {
                int recordId = GetNextRecordId();
                var initialResult = new InspectionResult
                {
                    STT = recordId,
                    PCBCode = $"PCB{DateTime.Now:yyyyMMddHHmmss}_{recordId:D6}",
                    ModelName = modelName ?? "Unknown",
                    InspectionDateTime = DateTime.Now,
                    Result = "IN_PROGRESS",
                    OperatorName = operatorName,
                    InspectionTime = 0,
                    TotalDefects = 0,
                    TotalOK = 0,
                    TotalNG = 0,
                    Defects = new System.Collections.Generic.List<DefectResult>()
                };

                _historyManager.SaveInspectionResult(initialResult);
                return recordId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating initial inspection record: {ex.Message}");
                return 0;
            }
        }
    }
}


