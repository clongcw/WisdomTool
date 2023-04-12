using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.Generic;

namespace WisdomTool.Models;

[AddINotifyPropertyChangedInterface]
public class ComConfig
{
    /// <summary>
    /// COM口
    /// </summary>
    public KeyValuePair<string, string> Port { get; set; } = new KeyValuePair<string, string>();

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
    /// 串口是否已打开
    /// </summary>
    public bool IsOpened { get; set; } = false;

    /// <summary>
    /// 是否处于接收数据状态
    /// </summary>
    [JsonIgnore]
    public bool IsReceiving { get; set; } = false;

    /// <summary>
    /// 是否处于发送数据状态
    /// </summary>
    [JsonIgnore]
    public bool IsSending { get; set; } = false;

    /// <summary>
    /// 最大分包时间
    /// </summary>
    public int TimeOut { get; set; } = 30;

    /// <summary>
    /// 最大分包字节
    /// </summary>
    public int MaxLength { get; set; } = 10240;

    /// <summary>
    /// 自动搜索设备的间隔 单位:ms
    /// </summary>
    public int SearchInterval { get; set; } = 50;
}