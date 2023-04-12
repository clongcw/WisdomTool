using System.ComponentModel;
using WisdomTool.Converters;

namespace WisdomTool.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum StopBits
    {
        [Description("1")]
        One = 1,
        [Description("2")]
        Two,
        [Description("1.5")]
        OnePointFive
    }
}