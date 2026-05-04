using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VM.Core;
using VMControls.WPF.Release;
using VMControls.WPF.Release.Front;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Services.Vision
{
    public class VisionService : IVisionService, IDisposable
    {
        private readonly ILogger<VisionService> _logger;
        private readonly IErrorService _errorService;
        private readonly VisionSolutionManager _solutionManager;
        private string _currentSolutionPath;
        private readonly Dictionary<string, VmProcedure> _procedures = new Dictionary<string, VmProcedure>();

        public bool IsSolutionLoaded => !string.IsNullOrEmpty(_currentSolutionPath);
        public string CurrentSolutionPath => _currentSolutionPath;
        
        private VmFrontendControl _frontendControl;
        public object FrontendControl 
        { 
            get => _frontendControl;
            set => _frontendControl = value as VmFrontendControl;
        }

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
                        
                        _frontendControl?.LoadFrontendSource();
                        
                        SolutionLoaded?.Invoke(this, path);
                        _logger.LogInformation($"Vision solution loaded: {path}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Vision solution file not found: {path}");
                    _errorService.ReportError("Vision", $"Vision solution file not found: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading vision solution: {path}");
                _errorService.ReportError("Vision", $"Failed to load vision solution: {path}", ex);
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
                    if (name == "Align")
                    {
                        _logger.LogWarning("Required procedure 'Align' not found in solution");
                        _errorService.ReportError("Vision", "Procedure 'Align' không tồn tại trong cấu hình Vision Master.");
                    }
                    else
                    {
                        _logger.LogDebug($"Optional procedure '{name}' not found");
                    }
                }
            }
        }

        private void UnsubscribeFromAll()
        {
            // Note: In VMaster, when loading a new solution, previous subscriptions are generally cleared,
            // but we clear our local dictionary to stay in sync.
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
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to extract results for {name}: {ex.Message}");
                    }
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

        public bool GetVisionStatus()
        {
            return IsSolutionLoaded && _procedures.ContainsKey("Align");
        }

        public VmProcedure GetProcedure(string procedureName)
        {
            _procedures.TryGetValue(procedureName, out var proc);
            return proc;
        }

        public void SaveImage(string procedureName, string pid, bool isOK, string message)
        {
            try
            {
                if (_procedures.TryGetValue(procedureName, out var proc))
                {
                    // Using dynamic to call SaveImage as the exact namespace/type 
                    // might be in a separate assembly (SaveImageCs.dll) that's hard to reference directly here
                    ((dynamic)proc).SaveImage(pid, isOK, message);
                    _logger.LogDebug($"Image saved for {procedureName} (PID: {pid})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to save image for {procedureName}: {ex.Message}");
            }
        }
        public void Dispose()
        {
            UnsubscribeFromAll();
        }
    }
}



