using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using WisdomTool.Converters;

namespace WisdomTool.Enums
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Options : int
    {
        [Description("横向数据源")]
        横向数据源 = 0,
        [Description("纵向数据源")]
        纵向数据源 = 1
    }

}
