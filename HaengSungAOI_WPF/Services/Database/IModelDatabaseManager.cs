using System.Collections.Generic;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Database
{
    public interface IModelDatabaseManager
    {
        List<PCBModel> GetAllModels();
        PCBModel GetModelById(int id);
        PCBModel GetActiveModel();
        void SaveModel(PCBModel model);
        void DeleteModel(int id);
        void SetActiveModel(int id);
    }
}
