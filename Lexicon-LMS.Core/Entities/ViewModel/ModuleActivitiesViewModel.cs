using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexicon_LMS.Core.Entities.ViewModel
{
    public class ModuleActivitiesViewModel
    {
        public int ModuleId { get; set; }
        public IEnumerable<ActivityListViewModel> ActivityList { get; set; }
    }
}
