using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using WisdomTool.Enums;

namespace WisdomTool.Models
{
    [AddINotifyPropertyChangedInterface]
    public class StepModeModel
    {
        public string Id { get; set; }
        public string Voltage { get; set; }
        public string Current { get; set; }
        public string Power  { get; set; }
        public string Name { get; set; }
    }
}
