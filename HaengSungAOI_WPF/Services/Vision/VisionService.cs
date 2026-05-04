using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VM.Core;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Utils;
using HaengSungAOI_WPF.Machine;

namespace HaengSungAOI_WPF.Services.Vision
{
    public class VisionService : IVisionService
    {
        private readonly ILogger<VisionService> _logger;
        private readonly IErrorService _errorService;
        private readonly VisionSolutionManager _solutionManager;
        private string _currentSolutionPath;
        private readonly Dictionary<string, VmProcedure> _procedures = new Dictionary<string, VmProcedure>();

        public bool IsSolutionLoaded => !string.IsNullOrEmpty(_currentSolutionPath);
        public string CurrentSolutionPath => _currentSolutionPath;
        public object FrontendControl { get; set; }

        public event EventHandler<VisionProcedureCompletedEventArgs> ProcedureCompleted;
        public event EventHandler<string> SolutionLoaded;

        public VisionService(ILogger<VisionService> logger, IErrorService errorService)
        {
            _logger = logger;
            _errorService = errorService;
            _solutionManager = new VisionSolutionManager();
        }

        public void LoadSolution(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    if (_currentSolutionPath != path)
                    {
                        UnsubscribeFromAll();
                        VmSolution.Load(path);
                        _currentSolutionPath = path;
                        InitializeProcedures();
                        SolutionLoaded?.Invoke(this, path);
                        _logger.LogInformation($"Vision solution loaded: {path}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Vision solution file not found: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading vision solution: {path}. (Vision SDK environment might be missing)");
                _errorService.ReportError("Vision", $"Failed to load vision solution: {path}", ex);
                // Do not re-throw to allow the application to continue running without vision hardware
            }
        }

        public void LoadSolutionForModel(PCBModel model)
        {
            if (model == null) return;
            string path = _solutionManager.GetModelVisionSolutionPath(model);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = _solutionManager.CreateDefaultVisionSolution(model);
            }
            LoadSolution(path);
        }

        private void InitializeProcedures()
        {
            _procedures.Clear();
            string[] procNames = { "Align", "Inspect1", "Inspect2", "Inspect3", "Inspect4", "Inspect5", "Inspect6" };
            
            foreach (var name in procNames)
            {
                var proc = VmSolution.Instance[name] as VmProcedure;
                if (proc != null)
                {
                    _procedures[name] = proc;
                    proc.OnWorkEndStatusCallBack += (status, ctx) => OnProcedureEnd(name, proc, (int)status);
                    _logger.LogDebug($"Procedure initialized: {name}");
                }
                else
                {
                    _logger.LogWarning($"Procedure not found in solution: {name}");
                }
            }
        }

        private void UnsubscribeFromAll()
        {
            foreach (var proc in _procedures.Values)
            {
                // Note: We can't easily unsubscribe if we use anonymous lambdas, 
                // but since we are loading a NEW solution, the old VmSolution instance is gone anyway.
            }
            _procedures.Clear();
        }

        private void OnProcedureEnd(string name, VmProcedure proc, int status)
        {
            try
            {
                bool isOk = false;
                float x = 0, y = 0, angle = 0;

                if (proc.ModuResult != null)
                {
                    try
                    {
                        int okVal = proc.ModuResult.GetOutputInt("OK").pIntVal[0];
                        isOk = (okVal == 1);

                        if (name == "Align")
                        {
                            x = proc.ModuResult.GetOutputFloat("X").pFloatVal[0];
                            y = proc.ModuResult.GetOutputFloat("Y").pFloatVal[0];
                            angle = proc.ModuResult.GetOutputFloat("R").pFloatVal[0];
                        }
                    }
                    catch { /* Ignore if output pins missing */ }
                }

                _logger.LogInformation($"Procedure completed: {name}, Status: {status}, Result: {(isOk ? "OK" : "NG")}");
                
                ProcedureCompleted?.Invoke(this, new VisionProcedureCompletedEventArgs(name, proc, isOk, x, y, angle));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling completion for {name}");
            }
        }

        public async Task RunProcedureAsync(string procedureName)
        {
            if (_procedures.TryGetValue(procedureName, out var proc))
            {
                await Task.Run(() => proc.Run());
            }
            else
            {
                _logger.LogWarning($"Cannot run procedure: {procedureName} - not found");
            }
        }

        public VmProcedure GetProcedure(string procedureName)
        {
            _procedures.TryGetValue(procedureName, out var proc);
            return proc;
        }

        public void Dispose()
        {
            UnsubscribeFromAll();
        }
    }
}
