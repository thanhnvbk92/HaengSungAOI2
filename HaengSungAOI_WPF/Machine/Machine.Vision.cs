using FrontendUI.WPF;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using VM.Core;
using VMControls.Interface;
using VMControls.RenderInterface;
using VMControls.WPF.Release;
using VMControls.WPF.Release.Front;

namespace HaengSungAOI_WPF.Machine
{
    /// <summary>
    /// Machine partial class - Vision solution and camera procedure handling
    /// </summary>
    public partial class Machine
    {
        /// <summary>
        /// Load vision solution based on the current PCB model
        /// </summary>
        public void LoadVisionSolution(PCBModel model)
        {
            try
            {
                if (model == null) return;

                string visionSolutionPath = _visionManager.GetModelVisionSolutionPath(model);

                if (string.IsNullOrEmpty(visionSolutionPath) || !File.Exists(visionSolutionPath))
                {
                    visionSolutionPath = _visionManager.CreateDefaultVisionSolution(model);
                }

                if (File.Exists(visionSolutionPath))
                {
                    if (_currentVisionSolutionPath != visionSolutionPath)
                    {
                        VmSolution.Load(visionSolutionPath);
                        _currentVisionSolutionPath = visionSolutionPath;
                        InitializeVisionProcedures();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error loading vision solution: {ex.Message}");
                LoadFallbackVisionSolution();
            }
        }

        private void LoadFallbackVisionSolution()
        {
            try
            {
                string fallbackPath = Path.Combine(@"E:\VMSolution", "Default.SOL");
                if (File.Exists(fallbackPath))
                {
                    VmSolution.Load(fallbackPath);
                    _currentVisionSolutionPath = fallbackPath;
                    InitializeVisionProcedures();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Failed to load fallback vision solution: {ex.Message}");
            }
        }

        private void InitializeVisionProcedures()
        {
            try
            {
                Console.WriteLine("Initializing vision procedures...");
                Camera_align = VmSolution.Instance["Align"] as VmProcedure;
                if (Camera_align == null)
                {
                    Console.WriteLine("Warning: 'Align' procedure not found in vision solution");
                    ProcessInfoList procedures = VmSolution.Instance.GetAllProcedureList();
                    Console.WriteLine($"Available procedures: {procedures.ToString()}");
                }
                else
                {
                    Console.WriteLine("Camera_align procedure found and initialized");
                }

                Camera_inspect1 = VmSolution.Instance["Inspect1"] as VmProcedure ?? new VmProcedure("Inspect1");
                Camera_inspect2 = VmSolution.Instance["Inspect2"] as VmProcedure ?? new VmProcedure("Inspect2");
                Camera_inspect3 = VmSolution.Instance["Inspect3"] as VmProcedure ?? new VmProcedure("Inspect3");
                Camera_inspect4 = VmSolution.Instance["Inspect4"] as VmProcedure ?? new VmProcedure("Inspect4");
                Camera_inspect5 = VmSolution.Instance["Inspect5"] as VmProcedure ?? new VmProcedure("Inspect5");
                Camera_inspect6 = VmSolution.Instance["Inspect6"] as VmProcedure ?? new VmProcedure("Inspect6");

                frontendControl.LoadFrontendSource();
                FrontendRootControl rootControl = frontendControl.Content as FrontendRootControl;
                FrontendUI.WPF.Controls.Root rootControl2 = rootControl.Content as FrontendUI.WPF.Controls.Root;
                UIElementCollection rootControl2Children = rootControl2.Children;


                Console.WriteLine("Vision procedures initialization completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing vision procedures: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        public bool GetVisionStatus()
        {
            try
            {
                return Camera_align != null && !string.IsNullOrEmpty(_currentVisionSolutionPath);
            }
            catch { return false; }
        }

        public void TriggerVision()
        {
            try
            {
                Camera_align?.Run();
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error triggering vision: {ex.Message}");
            }
        }


        private void SaveImageWithShapes(string folderPath, string name, FrontendUI.WPF.Controls.ImageControl imageControl)
        {
            if (imageControl == null) return;

            try
            {
                // Get the image data
                var imageData = imageControl.ImageDataSource as IImageData;
                if (imageData == null) return;

                // Get shapes source for rendering
                List<object> shapesSource = (imageControl.OShapeList as IEnumerable<object>)?
                  .Where((object p) => !(p is IIsVisible) || (p is IIsVisible isVisible && isVisible.IsVisible))
                 .ToList();

                // Use VMControls export helper - namespace may vary by VMaster version
                var exportType = Type.GetType("VMControls.Winform.Release.ExportControl.ImageSaveOpenCvHelper, VMControls.Winform.Release");
                if (exportType != null)
                {
                    var saveMethod = exportType.GetMethod("SaveRoiImageSyn", BindingFlags.Public | BindingFlags.Static);
                    if (saveMethod != null)
                    {
                        // Save without shapes
                        saveMethod.Invoke(null, new object[] { folderPath + $@"\{name}.jpg", imageData, null, null });

                        // Save with shapes
                        saveMethod.Invoke(null, new object[] { folderPath + $@"\{name}_r.jpg", imageData, shapesSource, null });
                    }
                    else
                    {
                        Logger.Warning("Machine", "SaveRoiImageSyn method not found in ImageSaveOpenCvHelper");
                    }
                }
                else
                {
                    Logger.Warning("Machine", "ImageSaveOpenCvHelper type not found - image saving disabled");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error in SaveImageWithShapes: {ex.Message}");
            }
        }

    }
}
