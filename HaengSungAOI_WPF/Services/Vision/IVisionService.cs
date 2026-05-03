using System;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Models;
using VM.Core;

namespace HaengSungAOI_WPF.Services.Vision
{
    public class VisionProcedureCompletedEventArgs : EventArgs
    {
        public string ProcedureName { get; }
        public VmProcedure Procedure { get; }
        public bool IsOK { get; }
        public float AlignX { get; }
        public float AlignY { get; }
        public float AlignAngle { get; }
        public DateTime Timestamp { get; }

        public VisionProcedureCompletedEventArgs(string procedureName, VmProcedure procedure, bool isOK, 
            float alignX = 0, float alignY = 0, float alignAngle = 0)
        {
            ProcedureName = procedureName;
            Procedure = procedure;
            IsOK = isOK;
            AlignX = alignX;
            AlignY = alignY;
            AlignAngle = alignAngle;
            Timestamp = DateTime.Now;
        }
    }

    public interface IVisionService : IDisposable
    {
        bool IsSolutionLoaded { get; }
        string CurrentSolutionPath { get; }
        object FrontendControl { get; set; }
        
        void LoadSolution(string path);
        void LoadSolutionForModel(PCBModel model);
        
        Task RunProcedureAsync(string procedureName);
        VmProcedure GetProcedure(string procedureName);
        
        event EventHandler<VisionProcedureCompletedEventArgs> ProcedureCompleted;
        event EventHandler<string> SolutionLoaded;
    }
}
