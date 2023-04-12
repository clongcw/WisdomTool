using Caliburn.Micro;
using PropertyChanged;
using System;

namespace WisdomTool.Models;

[AddINotifyPropertyChangedInterface]
public class MessageData : Screen
{
    public MessageData(string Content, DateTime dateTime, MessageType Type = MessageType.Info, string Title = "")
    {
        this.Type = Type;
        this.Content = Content;
        this.Time = dateTime;
        this.Title = Title;
    }

    /// <summary>
    /// 时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }
}