using System;
using System.IO;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Vision
{
    public class VisionSolutionManager
    {
        private const string BaseVisionPath = @"E:\VMSolution";

        public VisionSolutionManager()
        {
            EnsureBaseDirectoryExists();
        }

        private void EnsureBaseDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(BaseVisionPath))
                {
                    Directory.CreateDirectory(BaseVisionPath);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating base vision directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a model-specific directory for vision solutions
        /// </summary>
        /// <param name="model">PCB Model</param>
        /// <returns>Path to the model directory</returns>
        public string CreateModelDirectory(PCBModel model)
        {
            try
            {
                string modelDirPath = model.GetModelVisionDirectory();
                if (!Directory.Exists(modelDirPath))
                {
                    Directory.CreateDirectory(modelDirPath);
                    //Console.WriteLine($"Created vision directory for model: {modelDirPath}");
                }
                return modelDirPath;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating model directory: {ex.Message}");
                return BaseVisionPath;
            }
        }

        /// <summary>
        /// Copy a vision solution file to the model-specific directory
        /// </summary>
        /// <param name="sourcePath">Source .SOL file path</param>
        /// <param name="model">Target PCB Model</param>
        /// <param name="newFileName">New file name (optional)</param>
        /// <returns>Path to the copied file</returns>
        public string CopyVisionSolutionToModel(string sourcePath, PCBModel model, string newFileName = null)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Source vision solution file not found: {sourcePath}");
                }

                string modelDir = CreateModelDirectory(model);
                string fileName = newFileName ?? Path.GetFileName(sourcePath);
                string targetPath = Path.Combine(modelDir, fileName);

                File.Copy(sourcePath, targetPath, true);
                
                // Update model vision solution paths
                model.VisionSolutionName = fileName;
                model.VisionSolutionPath = targetPath;

                //Console.WriteLine($"Copied vision solution to: {targetPath}");
                return targetPath;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error copying vision solution: {ex.Message}");
                return sourcePath;
            }
        }

        /// <summary>
        /// Get the full path to a model's vision solution file
        /// </summary>
        /// <param name="model">PCB Model</param>
        /// <returns>Full path to the .SOL file</returns>
        public string GetModelVisionSolutionPath(PCBModel model)
        {
            // First try the model's specific path
            if (!string.IsNullOrEmpty(model.VisionSolutionPath) && File.Exists(model.VisionSolutionPath))
            {
                return model.VisionSolutionPath;
            }

            // Try model-specific directory
            string modelDir = model.GetModelVisionDirectory();
            if (Directory.Exists(modelDir))
            {
                string modelSpecificPath = Path.Combine(modelDir, model.VisionSolutionName ?? "Default.SOL");
                if (File.Exists(modelSpecificPath))
                {
                    model.VisionSolutionPath = modelSpecificPath;
                    return modelSpecificPath;
                }
            }

            // Fallback to base directory
            string basePath = Path.Combine(BaseVisionPath, model.VisionSolutionName ?? "Default.SOL");
            if (File.Exists(basePath))
            {
                return basePath;
            }

            // Create default if none exists
            return CreateDefaultVisionSolution(model);
        }

        /// <summary>
        /// Create a default vision solution for a model
        /// </summary>
        /// <param name="model">PCB Model</param>
        /// <returns>Path to the created default solution</returns>
        public string CreateDefaultVisionSolution(PCBModel model)
        {
            try
            {
                string modelDir = CreateModelDirectory(model);
                string defaultPath = Path.Combine(modelDir, "Default.SOL");

                // Check if there's a template default solution in base directory
                string templatePath = Path.Combine(BaseVisionPath, "Template.SOL");
                if (!File.Exists(templatePath))
                {
                    templatePath = Path.Combine(BaseVisionPath, "Default.SOL");
                }

                if (File.Exists(templatePath))
                {
                    File.Copy(templatePath, defaultPath, true);
                }
                else
                {
                    // Create a minimal solution file if no template exists
                    CreateMinimalSolutionFile(defaultPath);
                }

                model.VisionSolutionName = "Default.SOL";
                model.VisionSolutionPath = defaultPath;

                //Console.WriteLine($"Created default vision solution: {defaultPath}");
                return defaultPath;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating default vision solution: {ex.Message}");
                return Path.Combine(BaseVisionPath, "Default.SOL");
            }
        }

        private void CreateMinimalSolutionFile(string filePath)
        {
            try
            {
                // Create a minimal .SOL file structure
                // This would depend on the specific format required by VM.Core
                File.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Solution>\r\n</Solution>");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating minimal solution file: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a model's vision solution directory and files
        /// </summary>
        /// <param name="model">PCB Model</param>
        public void DeleteModelVisionSolution(PCBModel model)
        {
            try
            {
                string modelDir = model.GetModelVisionDirectory();
                if (Directory.Exists(modelDir))
                {
                    Directory.Delete(modelDir, true);
                    //Console.WriteLine($"Deleted vision solution directory: {modelDir}");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error deleting model vision solution: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all available vision solution files in the base directory
        /// </summary>
        /// <returns>Array of .SOL file names</returns>
        public string[] GetAvailableVisionSolutions()
        {
            try
            {
                if (Directory.Exists(BaseVisionPath))
                {
                    string[] solFiles = Directory.GetFiles(BaseVisionPath, "*.SOL");
                    string[] fileNames = new string[solFiles.Length];
                    for (int i = 0; i < solFiles.Length; i++)
                    {
                        fileNames[i] = Path.GetFileName(solFiles[i]);
                    }
                    return fileNames;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error getting available vision solutions: {ex.Message}");
            }
            return new string[0];
        }

        /// <summary>
        /// Validate that a vision solution file exists and is accessible
        /// </summary>
        /// <param name="filePath">Path to the .SOL file</param>
        /// <returns>True if file exists and is accessible</returns>
        public bool ValidateVisionSolution(string filePath)
        {
            try
            {
                return File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".sol";
            }
            catch
            {
                return false;
            }
        }
    }
}


