using AjaxControlToolkit.HtmlEditor.ToolbarButtons;
using Caliburn.Micro;
using MiniExcelLibs;
using OfficeOpenXml;
using Panuon.WPF.UI;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Wisdom.Shared.Common;
using WisdomTool.Enums;
using File = System.IO.File;
using MessageBoxIcon = Panuon.WPF.UI.MessageBoxIcon;
using Path = System.IO.Path;

namespace WisdomTool.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class OverToSingleViewModel : Caliburn.Micro.Screen
    {
        public string ExcelPath { get; set; } //= @"C:\Users\clongc\Desktop\资金测算_公式(宝应)01(1) 的副本.xlsx";
        public string TemplateExcelPath { get; set; } //= @"C:\Users\clongc\Desktop\HaiLing\附件12模板.xlsx";
        //public string TmpPath { get; set; } = @"C:\Users\clongc\Desktop\HaiLing\Tmp\";
        public string TmpPath { get; set; }
        public string MergedName { get; set; } //= "请点击左侧按钮，注意输出文件夹不能选择Tmp";
        public string SheetName { get; set; } = "Sheet2";

        //纵向选择显示
        public bool IsControl1Visible { get; set; } = true;
        //横向选择显示
        public bool IsControl2Visible { get; set; } = false;

        private Options _options = Options.纵向数据源;
        public Options Options
        {
            get { return _options; }
            set
            {
                _options = value;
                if (_options == Options.纵向数据源)
                {
                    IsControl1Visible = true;
                    IsControl2Visible = false;
                }
                else if (_options == Options.横向数据源)
                {
                    IsControl1Visible = false;
                    IsControl2Visible = true;
                }
                else
                {
                    IsControl1Visible = false;
                    IsControl2Visible = false;
                }
            }
        }
        public TemplateOptions TemplateOptions { get; set; } = TemplateOptions.附件13;

        /// <summary>
        /// 开始列
        /// </summary>
        public string StartColumn { get; set; } = "A";

        /// <summary>
        /// 结束列
        /// </summary>
        public string EndColumn { get; set; } = "Z";

        /// <summary>
        /// 开始行
        /// </summary>
        public int StartRow { get; set; } = 3;

        /// <summary>
        /// 结束行
        /// </summary>
        public int EndRow { get; set; } = 10;

        //生成文件进度
        private double schedule;
        public double Schedule
        {
            get => schedule;
            set => schedule = double.Parse(string.Format("{0:f2}", value));
        }

        public bool? IsEnabled { get; set; } = true;
        public OverToSingleViewModel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            TmpPath = Environment.CurrentDirectory + "\\Tmp\\";
        }

        public void OpenExcel()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ExcelPath = openFileDialog.FileName;
            }
        }

        public void OpenTemplateExcel()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TemplateExcelPath = openFileDialog.FileName;
            }
        }

        public void OpenOutputDic()
        {
            // 创建 FolderBrowserDialog 对象
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            // 显示选择文件夹对话框并获取结果
            DialogResult result = folderBrowserDialog.ShowDialog();

            // 处理结果
            if (result == DialogResult.OK)
            {
                // 用户选择了文件夹
                TmpPath = $"{folderBrowserDialog.SelectedPath}\\";
                // 执行处理逻辑
            }
        }


        public void Export()
        {
            //配置文件目录
            string dict = null;
            //IOUtil.Exists(dict);
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "请选择导出配置文件...",            //对话框标题
                Filter = "Excel Files(*.xlsx)|*.xlsx",    //文件格式过滤器
                FilterIndex = 1,                          //默认选中的过滤器
                FileName = "merged",                      //默认文件名
                DefaultExt = "xlsx",                      //默认扩展名
                InitialDirectory = dict,                  //指定初始的目录
                OverwritePrompt = true,                   //文件已存在警告
                AddExtension = true,                      //若用户省略扩展名将自动添加扩展名
            };
            if (sfd.ShowDialog() == true)
            {
                MergedName = sfd.FileName;
            }
        }

        public void OutputExcels()
        {
            List<string> sheetNames = MiniExcel.GetSheetNames(ExcelPath);
            // 读取数据源文件
            var templateRows = MiniExcel.Query(TemplateExcelPath).ToList();

            foreach (string sheetName in sheetNames)
            {
                var rows = MiniExcel.Query(ExcelPath, sheetName: sheetName).ToArray();
                // 打开新的Excel工作簿
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelPackage newWorkbook = new ExcelPackage();
                List<string> sourceFiles = new List<string>();
                for (int i = 1; i < rows.Length; i++)
                {
                    List<string> newdatas = new List<string>();

                    newdatas.Add(rows[i].B.ToString());
                    newdatas.Add(rows[i].C.ToString());
                    newdatas.Add(rows[i].D.ToString());
                    newdatas.Add(rows[i].E.ToString());
                    newdatas.Add(rows[i].F.ToString());
                    newdatas.Add(rows[i].G.ToString());
                    newdatas.Add(rows[i].H.ToString());

                    Dictionary<string, object> value = new Dictionary<string, object>();
                    for (int k = 0; k < newdatas.Count; k++)
                    {
                        string key = "A" + k.ToString();
                        value.Add(key, newdatas[k]); // null代表暂时没有值
                    }
                    MiniExcel.SaveAsByTemplate(TmpPath + i + ".xlsx", TemplateExcelPath, value);

                }
            }
        }

        public async Task OutputExcelsWithHorizontal()
        {
            List<string> sheetNames = MiniExcel.GetSheetNames(ExcelPath);

            /*foreach (string sheetName in sheetNames)
            {*/
            var rows = MiniExcel.Query(ExcelPath, sheetName: SheetName).Cast<IDictionary<string, object>>().ToList();
            rows = rows.Skip(StartRow - 1).Take(EndRow - StartRow + 1).ToList();

            List<string> newdatas = new List<string>();
            Dictionary<string, object> value = new Dictionary<string, object>();
            int count = 0;
            foreach (var row in rows)
            {
                foreach (var item in row)
                {
                    if (item.Value != null)
                    {
                        newdatas.Add(item.Value.ToString());
                    }
                }

                for (int k = 0; k < newdatas.Count; k++)
                {
                    string key = "A" + k.ToString();
                    value.Add(key, newdatas[k]); // null代表暂时没有值
                }
                MiniExcel.SaveAsByTemplate(TmpPath + newdatas[0] + ".xlsx", TemplateExcelPath, value);
                await Task.Delay(10);
                value.Clear();
                newdatas.Clear();
                count++;
                Schedule = 50d / rows.Count * count;
            }

            /*}*/
        }

       


        public async Task OutputExcelsWithVertical()
        {
            List<string> sheetNames = MiniExcel.GetSheetNames(ExcelPath);
            

            // 读取数据源文件
            var templateRows = MiniExcel.Query(TemplateExcelPath).ToList();
            List<string> newdatas = new List<string>();
            Dictionary<string, object> value = new Dictionary<string, object>();
            /*foreach (string sheetName in sheetNames)
            {*/
            //var rows = MiniExcel.Query(ExcelPath, sheetName: sheetName).ToList();
            //var rows = MiniExcel.Query(ExcelPath, sheetName: sheetName).Cast<IDictionary<string, object>>().ToList();
            //暂时只需要第一张表
            var rows = MiniExcel.Query(ExcelPath, sheetName: SheetName).Cast<IDictionary<string, object>>().ToList();
            /*string[] columns = new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                                 "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                                 "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ"};*/

            string[] columns = ExcelHelper.GetColmunNames(StartColumn, EndColumn);

            for (int i = 0; i < columns.Length; i++)
            {
                for (int j = 0; j < rows.Count; j++)
                {
                    var row = rows[j];
                    if (row[columns[i]] != null)
                    {
                        var result = row[columns[i]].ToString();
                        newdatas.Add(result);
                    }
                    else
                    {
                        newdatas.Add("");
                    }
                }

                if (TemplateOptions == TemplateOptions.附件13)
                {
                    value.Add("name", newdatas[2]);
                    value.Add("id", newdatas[1]);

                    if (newdatas[0].Contains("2023"))
                    {
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "A" + (k - 1).ToString();
                            value.Add(key, newdatas[k]); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "B" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "C" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                    }

                    if (newdatas[0].Contains("2024"))
                    {
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "A" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "B" + (k - 1).ToString();
                            value.Add(key, newdatas[k]); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "C" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                    }

                    if (newdatas[0].Contains("2025"))
                    {
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "A" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "B" + (k - 1).ToString();
                            value.Add(key, null); // null代表暂时没有值
                        }
                        for (int k = 3; k < newdatas.Count; k++)
                        {
                            string key = "C" + (k - 1).ToString();
                            value.Add(key, newdatas[k]); // null代表暂时没有值
                        }
                    }

                    MiniExcel.SaveAsByTemplate(TmpPath + newdatas[2] + ".xlsx", TemplateExcelPath, value);
                }
                else
                {
                    value.Add("Name", newdatas[0]);
                    value.Add("Id", newdatas[1]);
                    for (int k = 3; k < newdatas.Count; k++)
                    {
                        string key = "A" + (k - 3).ToString();
                        value.Add(key, newdatas[k]); // null代表暂时没有值
                    }


                    MiniExcel.SaveAsByTemplate(TmpPath + newdatas[0] + ".xlsx", TemplateExcelPath, value);
                }



                value.Clear();
                newdatas.Clear();
                await Task.Delay(50);

                Schedule = 50d / (columns.Length - 1) * i;
            }
            /*}*/
        }

        public async Task MergeExcelFiles()
        {
            // 创建一个新的 Excel 文件
            var newFile = new FileInfo(MergedName);
            if (File.Exists(MergedName))
            {
                // 如果文件存在，删除文件
                File.Delete(MergedName);
            }

            // 创建一个 ExcelPackage 对象，用于写入数据到新文件中
            using (var package = new ExcelPackage(newFile))
            {
                // 获取目录下所有 Excel 文件信息，并按照创建时间排序
                var files = new DirectoryInfo(TmpPath).GetFiles("*.xlsx")
                                                         .OrderBy(f => f.CreationTime)
                                                         .ToList();
                //文件数
                int count = 0;
                // 遍历排序后的 Excel 文件
                foreach (var file in files)
                {
                    // 创建一个 ExcelPackage 对象，用于读取数据
                    using (var source = new ExcelPackage(file))
                    {
                        // 获取当前 Excel 文件的第一个工作表
                        var sourceWorksheet = source.Workbook.Worksheets[0];

                        // 在新文件中创建一个新的工作表，名称为当前 Excel 文件的文件名

                        // 检查文件是否存在
                        if (File.Exists(Path.GetFileNameWithoutExtension(file.FullName)))
                        {
                            // 如果文件存在，删除文件
                            File.Delete(Path.GetFileNameWithoutExtension(file.FullName));
                        }
                        var destWorksheet = package.Workbook.Worksheets.Add(Path.GetFileNameWithoutExtension(file.FullName), sourceWorksheet);

                        // 重新计算工作表中的公式
                        destWorksheet.Calculate();
                        await Task.Delay(1);
                        count++;
                        //实时进度
                        Schedule = 50 + 50d / files.Count * count;
                    }
                }

                // 保存新文件
                package.Save();
            }
        }

      

        public async Task GenerateNewExcelFile()
        {
            

            Schedule = 0;
            if (!Regex.IsMatch(MergedName, "[a-gA-G]"))
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    MessageBoxX.Show(null, "请先点击左侧按钮！", "消息提示", MessageBoxButton.OK, MessageBoxIcon.Warning);
                }));
                return;
            }

            if (string.IsNullOrEmpty(StartColumn) || string.IsNullOrEmpty(EndColumn))
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    MessageBoxX.Show(null, "请选择生成列数！", "消息提示", MessageBoxButton.OK, MessageBoxIcon.Warning);
                }));
                return;
            }


            IsEnabled = false;
            //删除Tmp文件夹下所有文件
            IOUtil.Exists(TmpPath);
            DirectoryInfo directory = new DirectoryInfo(TmpPath);
            if (directory != null)
            {
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
            }

            if (Options == Options.横向数据源)
            {
                await OutputExcelsWithHorizontal();
            }
            else if (Options == Options.纵向数据源)
            {
                await OutputExcelsWithVertical();
            }
            else
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    MessageBoxX.Show(null, "当先数据源未定义！", "消息提示", MessageBoxButton.OK, MessageBoxIcon.Warning);
                }));
                IsEnabled = true;
                return;
            }

            await MergeExcelFiles();

            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                MessageBoxX.Show(null, "新文件已生成！", "消息提示", MessageBoxButton.OK, MessageBoxIcon.Success);
            }));
            IsEnabled = true;
        }
    }


}
