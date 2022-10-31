using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_Master_project
{
    public class ApplicationSettings
    {
        public bool Minimize_as_TaskbarIcon { get; set; }
        public bool Start_with_minimized { get; set; }
        public string Language { get; set; }

        public ApplicationSettings(bool minimize_as_TaskbarIcon, bool start_with_minimized, string language)
        {
            Minimize_as_TaskbarIcon = minimize_as_TaskbarIcon;
            Start_with_minimized = start_with_minimized;
            Language = language;
        }
    }
}
