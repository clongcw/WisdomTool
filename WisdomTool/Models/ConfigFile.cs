using Caliburn.Micro;
using System.IO;

namespace WisdomTool.Models;

public class ConfigFile : Screen
{
    public ConfigFile(FileInfo file)
    {
        File = file;
    }

    private FileInfo File { get; set; }

    /// <summary>
    /// 文件名称
    /// </summary>
    public string Name => File.Name.Replace(File.Extension, "");

    /// <summary>
    /// 文件全称
    /// </summary>
    public string FullName => File.FullName;
}