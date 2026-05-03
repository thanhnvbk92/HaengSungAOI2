using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.ViewModels
{
    public static class AppConfig
    {
        public static string SaveDir
        {
            get
            {
                string dir = ConfigurationManager.AppSettings["SaveDir"];

                if (string.IsNullOrWhiteSpace(dir))
                    throw new Exception("SaveDir is not configured in App.config");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir;
            }
        }
    }

}
