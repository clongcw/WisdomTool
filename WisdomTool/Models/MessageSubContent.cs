using Caliburn.Micro;
using WisdomTool.Enums;

namespace WisdomTool.Models;

/// <summary>
/// 子消息
/// </summary>
public class MessageSubContent : Screen
{
    public MessageSubContent(string content, ModbusRtuMessageType type)
    {
        Content = content;
        Type = type;
    }
    //public MessageSubContent(string content, Brush brush)
    //{
    //    Content = content;
    //    Brush = brush;
    //}

    /// <summary>
    /// 子消息内容
    /// </summary>
    public string Content { get => _Content; set => Set(ref _Content, value); }
    private string _Content = string.Empty;

    ///// <summary>
    ///// 消息展示的颜色
    ///// </summary>
    //public Brush Brush { get => _Brush; set => Set(ref _Brush, value); }
    //private Brush _Brush = Brushes.Black;

    /// <summary>
    /// 消息类型
    /// </summary>
    public ModbusRtuMessageType Type { get => _Type; set => Set(ref _Type, value); }
    private ModbusRtuMessageType _Type;
}