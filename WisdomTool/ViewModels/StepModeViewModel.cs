using Caliburn.Micro;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiniExcelLibs;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using WisdomTool.Models;

namespace WisdomTool.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class StepModeViewModel : Screen
    {
        private Thread thread;
        private bool pauseFlag = false;
        private bool stopFlag = false;

        public StepModeModel Model { get; set; } = new StepModeModel();
        public ObservableCollection<StepModeModel>? Steps { get; set; } = new();
        public ObservableCollection<string> Processes { get; set; } = new();
        public string ExcelPath { get; set; } = Environment.CurrentDirectory + @"\BatteryProcessLib\BatteryProcessLib.xlsx";

        public StepModeViewModel()
        {

            string asd = ReadExcel();
            Steps = JsonConvert.DeserializeObject<ObservableCollection<StepModeModel>>(asd);
            thread = new Thread(new ThreadStart(this.Run));
            thread.Start();
        }
        
        static string ReadExcel()
        {
            DataTable dtTable = new DataTable();
            List<string> rowList = new List<string>();
            ISheet sheet;
            using (var stream = new FileStream(Environment.CurrentDirectory + @"\BatteryProcessLib\BatteryProcessLib.xlsx", FileMode.Open))
            {
                stream.Position = 0;
                XSSFWorkbook xssWorkbook = new XSSFWorkbook(stream);
                sheet = xssWorkbook.GetSheetAt(0);
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                for (int j = 0; j < cellCount; j++)
                {
                    ICell cell = headerRow.GetCell(j);
                    if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) continue;
                    {
                        dtTable.Columns.Add(cell.ToString());
                    } 
                }
                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;
                    if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        if (row.GetCell(j) != null)
                        {
                            if (!string.IsNullOrEmpty(row.GetCell(j).ToString()) && !string.IsNullOrWhiteSpace(row.GetCell(j).ToString()))
                            {
                                rowList.Add(row.GetCell(j).ToString());
                            }
                        }
                    }
                    if(rowList.Count>0)
                        dtTable.Rows.Add(rowList.ToArray());
                    rowList.Clear(); 
                }
            }
            return JsonConvert.SerializeObject(dtTable);
        }

        public void Resume()
        {
            pauseFlag = false;
        }

        public void Cancel()
        {
            stopFlag = true;
        }

        private void Run()
        {
            while (!stopFlag)
            {
                if (!pauseFlag)
                {
                    // 在这里执行线程的任务

                    // 例如，这里输出当前时间并等待1秒
                    Console.WriteLine(DateTime.Now);
                    Thread.Sleep(1000);
                }
                else
                {
                    // 如果暂停了，则等待100毫秒后再继续
                    Thread.Sleep(100);
                }
            }
        }
    }
}