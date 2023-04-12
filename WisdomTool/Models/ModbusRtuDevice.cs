using PropertyChanged;

namespace WisdomTool.Models;

[AddINotifyPropertyChangedInterface]
public class ModbusRtuDevice
{
    /// <summary>
    /// 从站地址
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// 波特率
    /// </summary>
    public BaudRate BaudRate { get; set; } = BaudRate._9600;

    /// <summary>
    /// 校验位
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 接收的消息
    /// </summary>
    public string ReceiveMessage { get; set; } = string.Empty;
}