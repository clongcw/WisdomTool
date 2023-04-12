﻿using Caliburn.Micro;
using Microsoft.Win32;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using Wisdom.Shared.Common;
using WisdomTool.Enums;
using WisdomTool.Extensions;
using WisdomTool.Models;

namespace WisdomTool.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ModbusRtuViewModel : Screen
    {
        #region **********字段**********

        public readonly SerialPort SerialPort = new SerialPort(); //串口
        protected System.Timers.Timer timer = new System.Timers.Timer(); //定时器 定时读取数据
        public Queue<(string, int)> PublishFrameQueue = new Queue<(string, int)>(); //数据帧发送队列
        public readonly Queue<string> ReceiveFrameQueue = new Queue<string>(); //数据帧处理队列

        readonly Task publishHandleTask; //发布消息处理线程
        readonly Task receiveHandleTask; //接收处理线程

        public readonly string ModbusRtuConfigDict = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuConfig"); //ModbusRtu配置文件路径
        public readonly string ModbusRtuAutoResponseConfigDict = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuAutoResponseConfig"); //ModbusRtu自动应答配置文件路径


        public EventWaitHandle WaitPublishFrameEnqueue = new AutoResetEvent(true); //等待发布消息入队
        public EventWaitHandle WaitUartReceived = new AutoResetEvent(true); //接收到串口数据完成标志
        public static string viewName = "ModbusRtuView";

        #endregion

        #region **********构造函数**********

        public ModbusRtuViewModel()
        {
            SerialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiveMessage); //串口接收事件

            //更新串口列表
            GetComPorts();

            //默认选中9600无校验
            SelectedBaudRates.Add(BaudRate._9600);
            SelectedParitys.Add(Models.Parity.None);

            //数据帧处理子线程
            publishHandleTask = new Task(PublishFrame);
            receiveHandleTask = new Task(ReceiveFrame);
            publishHandleTask.Start();
            receiveHandleTask.Start();

        }

        public string AAProperty { get; set; }


        #endregion

        #region **********搜索设备 属性**********

        /// <summary>
        /// 当前搜索的ModbusRtu设备
        /// </summary>
        public ModbusRtuDevice CurrentDevice { get; set; } = new ModbusRtuDevice();

        /// <summary>
        /// 搜索设备的状态 0=未开始搜索 1=搜索中 2=搜索结束/搜索中止
        /// </summary>
        public int SearchDeviceState { get; set; } = 0;

        public IList<WisdomTool.Models.Parity> SelectedParitys { get; set; } = new List<WisdomTool.Models.Parity>();

        /// <summary>
        /// 搜索到的ModbusRtu设备
        /// </summary>
        public ObservableCollection<ModbusRtuDevice> ModbusRtuDevices { get; set; } = new ObservableCollection<ModbusRtuDevice>();

        /// <summary>
        /// 选中的波特率
        /// </summary>
        public IList<BaudRate> SelectedBaudRates { get; set; } = new List<BaudRate>();

        #endregion

        #region **********属性**********

        public string Visibility { get; set; } = "Visible";

        /// <summary>
        /// Com口配置
        /// </summary>
        public ComConfig ComConfig { get; set; } = new ComConfig();

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> ComPorts { get; set; } = new ObservableCollection<KeyValuePair<string, string>>();

        /// <summary>
        /// 页面消息
        /// </summary>
        public ObservableCollection<MessageData> Messages { get; set; } = new ObservableCollection<MessageData>();

        /// <summary>
        /// 发送的消息
        /// </summary>
        public string SendMessage { get; set; } = "01 03 0000 0031";

        /// <summary>
        /// Crc校验模式
        /// </summary>
        public CrcMode CrcMode { get; set; } = CrcMode.Modbus;

        /// <summary>
        /// 接收的数据总数
        /// </summary>
        public int ReceiveBytesCount { get; set; } = 0;

        /// <summary>
        /// 发送的数据总数
        /// </summary>
        public int SendBytesCount { get; set; } = 0;

        /// <summary>
        /// 暂停界面更新接收的数据
        /// </summary>
        public bool IsPause { get; set; } = false;

        /// <summary>
        /// 数据监控配置
        /// </summary>
        public DataMonitorConfig DataMonitorConfig { get; set; } = new DataMonitorConfig();

        /// <summary>
        /// ModbusRtuDataDataView
        /// </summary>
        public ListCollectionView ModbusRtuDataDataView { get; set; }

        /// <summary>
        /// 配置文件列表
        /// </summary>
        public ObservableCollection<ConfigFile> ConfigFiles { get; set; } = new ObservableCollection<ConfigFile>();

        /// <summary>
        /// 是否开启自动应答
        /// </summary>
        public bool IsAutoResponse { get; set; } = false;

        /// <summary>
        /// ModbusRtu自动应答
        /// </summary>
        public ObservableCollection<ModbusRtuAutoResponseData> MosbusRtuAutoResponseDatas { get; set; } = new ObservableCollection<ModbusRtuAutoResponseData>();

        #endregion

        #region **************************************** 方法 ****************************************



        /// <summary>
        /// 发送自定义帧
        /// </summary>
        public void SendCustomFrame()
        {
            //若串口未打开则打开串口
            if (!ComConfig.IsOpened)
            {
                ShowMessage("串口未打开, 尝试打开串口...");
                OpenCom();
                if (!ComConfig.IsOpened)
                {
                    return;
                }
            }

            try
            {
                //var msg = SendMessage.Replace("-", string.Empty).GetBytes();
                //List<byte> crc = new();
                ////根据选择进行CRC校验
                //switch (CrcMode)
                //{
                //    //无校验
                //    case CrcMode.None:
                //        break;

                //    //Modebus校验
                //    case CrcMode.Modbus:
                //        var code = Crc.Crc16Modbus(msg);
                //        Array.Reverse(code);
                //        crc.AddRange(code);
                //        break;
                //    default:
                //        break;
                //}
                ////合并数组
                //List<byte> list = new List<byte>();
                //list.AddRange(msg);
                //list.AddRange(crc);
                //var data = BitConverter.ToString(list.ToArray()).Replace("-", "");

                PublishFrameEnqueue(GetCrcedStrWithSelect(SendMessage));                  //将待发送的消息添加进队列
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }


        /// <summary>
        /// 根据选择 对字符串进行crc校验
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string GetCrcedStrWithSelect(string msg)
        {
            string reMsg = msg.Replace("-", string.Empty).Replace(" ", string.Empty);
            if (reMsg.Length % 2 == 1)
            {
                ShowErrorMessage("发送字符数量不符, 应为2的整数倍");
                return null;
            }
            var msg2 = msg.Replace("-", string.Empty).GetBytes();
            List<byte> crc = new();
            //根据选择进行CRC校验
            switch (CrcMode)
            {
                //无校验
                case CrcMode.None:
                    break;

                //Modebus校验
                case CrcMode.Modbus:
                    var code = Crc.Crc16Modbus(msg2);
                    Array.Reverse(code);
                    crc.AddRange(code);
                    break;
                default:
                    break;
            }
            //合并数组
            List<byte> list = new List<byte>();
            list.AddRange(msg2);
            list.AddRange(crc);
            var data = BitConverter.ToString(list.ToArray()).Replace("-", "");
            return data;
        }

        /// <summary>
        /// 根据选择 对字符串进行crc校验
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string GetModbusCrcedStr(string msg)
        {
            var msg2 = msg.Replace("-", string.Empty).GetBytes();
            List<byte> crc = new();
            //根据选择ModbusCRC校验
            var code = Crc.Crc16Modbus(msg2);
            Array.Reverse(code);
            crc.AddRange(code);
            //合并数组
            List<byte> list = new List<byte>();
            list.AddRange(msg2);
            list.AddRange(crc);
            var data = BitConverter.ToString(list.ToArray()).Replace("-", "");
            return data;
        }



        /// <summary>
        /// 关闭自动应答
        /// </summary>
        public void AutoResponseOff()
        {
            try
            {
                IsAutoResponse = false;
                ShowMessage("关闭自动应答...");
                //HcGrowlExtensions.Warning("关闭自动应答...", viewName);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 启用自动应答
        /// </summary>
        public void AutoResponseOn()
        {
            try
            {
                IsAutoResponse = true;
                ShowMessage("启用自动应答...");
                //HcGrowlExtensions.Success("启用自动应答...", viewName);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 添加新的响应
        /// </summary>
        public void AddMosbusRtuAutoResponseData()
        {
            try
            {
                MosbusRtuAutoResponseDatas.Add(new());
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 测试
        /// </summary>
        public void Test()
        {
            //
        }

        /// <summary>
        /// 开关数据过滤器
        /// </summary>
        public void OperateFilter()
        {
            try
            {
                //验证过滤后是否有值 没有值则提示无法过滤
                if (!DataMonitorConfig.IsFilter)
                {
                    var y = DataMonitorConfig.ModbusRtuDatas.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name));
                    if (y == null)
                    {
                        //HcGrowlExtensions.Warning("请配置数据名称再开启过滤器...");
                        return;
                    }
                }

                DataMonitorConfig.IsFilter = !DataMonitorConfig.IsFilter;
                RefreshModbusRtuDataDataView();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 更新数据监控视图
        /// </summary>
        public void RefreshModbusRtuDataDataView()
        {
            ModbusRtuDataDataView = (ListCollectionView)CollectionViewSource.GetDefaultView(DataMonitorConfig.ModbusRtuDatas);
            //数据监控过滤器
            if (DataMonitorConfig.IsFilter)
            {
                ModbusRtuDataDataView.Filter = new Predicate<object>(x => !string.IsNullOrWhiteSpace(((ModbusRtuData)x).Name));
            }
            else
            {
                ModbusRtuDataDataView.Filter = new Predicate<object>(x => true);
            }
            //TestDataView = (DataView)CollectionViewSource.GetDefaultView(DataMonitorConfig.ModbusRtuDatas);
            //var cview = CollectionViewSource.GetDefaultView(DataMonitorConfig.ModbusRtuDatas);
            //cview.Filter = new Predicate<object>(x => true);
            //TestDataView = (DataView)cview;
        }

        /// <summary>
        /// 关闭自动读取数据
        /// </summary>
        public void CloseAutoRead()
        {
            try
            {
                timer.Stop();
                DataMonitorConfig.IsOpened = false;
                CloseCom();
                ShowMessage("关闭自动读取数据...");
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 打开数据监控
        /// </summary>
        public async void OpenAutoRead()
        {
            try
            {
                //若搜索设备则先停止搜索
                if (SearchDeviceState == 1)
                {
                    SearchDeviceState = 2;
                }

                //若串口未打开 则开启串口
                if (!ComConfig.IsOpened)
                    OpenCom();
                //若串口开启失败则返回
                if (!ComConfig.IsOpened)
                    return;

                timer = new()
                {
                    Interval = DataMonitorConfig.Period,   //这里设置的间隔时间单位ms
                    AutoReset = true                    //设置一直执行
                };
                timer.Elapsed += TimerElapsed;
                timer.Start();
                DataMonitorConfig.IsOpened = true;
                ShowMessage("开启数据监控...");
                //IsDrawersOpen.IsRightDrawerOpen = false;

                //生成列表
                if (DataMonitorConfig.ModbusRtuDatas.Count != DataMonitorConfig.Quantity)
                {
                    //关闭过滤器
                    DataMonitorConfig.IsFilter = false;
                    RefreshModbusRtuDataDataView();

                    //生成列表
                    DataMonitorConfig.ModbusRtuDatas.Clear();
                    for (int i = DataMonitorConfig.StartAddr; i < DataMonitorConfig.Quantity + DataMonitorConfig.StartAddr; i++)
                    {
                        DataMonitorConfig.ModbusRtuDatas.Add(new ModbusRtuData()
                        {
                            Addr = i
                        }); ;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 数据监控 定时器事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                string msg = DataMonitorConfig.DataFrame[..^4];
                PublishMessage(GetModbusCrcedStr(msg));
                ////PublishMessage(DataMonitorConfig.DataFrame);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
            finally
            {
                timer.Start();
            }
        }

        /// <summary>
        /// 周期读取区域数据
        /// </summary>
        public void AreaData()
        {
            DataMonitorConfig.ModbusRtuDatas.Clear();
            for (int i = 0; i < 100; i++)
            {
                DataMonitorConfig.ModbusRtuDatas.Add(new ModbusRtuData() { Addr = i });
            }
        }

        /// <summary>
        /// 暂停更新接收的数据
        /// </summary>
        public void Pause()
        {
            IsPause = !IsPause;
            if (IsPause)
            {
                ShowMessage("暂停更新接收的数据");
            }
            else
            {
                ShowMessage("恢复更新接收的数据");
            }
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        public void OpenCom()
        {
            try
            {
                //判断串口是否已打开
                if (ComConfig.IsOpened)
                {
                    ShowMessage("当前串口已打开, 无法重复开启");
                    return;
                }

                //配置串口
                SerialPort.PortName = ComConfig.Port.Key;                              //串口
                SerialPort.BaudRate = (int)ComConfig.BaudRate;                         //波特率
                SerialPort.Parity = (System.IO.Ports.Parity)ComConfig.Parity;          //校验
                SerialPort.DataBits = ComConfig.DataBits;                              //数据位
                SerialPort.StopBits = (System.IO.Ports.StopBits)ComConfig.StopBits;    //停止位
                try
                {
                    SerialPort.Open();               //打开串口
                    ComConfig.IsOpened = true;      //标记串口已打开
                    ShowMessage($"打开串口 {SerialPort.PortName} : {ComConfig.Port.Value}  波特率: {SerialPort.BaudRate} 校验: {SerialPort.Parity}");
                }
                catch (Exception ex)
                {
                    ShowMessage($"打开串口失败, 该串口设备不存在或已被占用。{ex.Message}", MessageType.Error);
                    return;
                }
                //IsDrawersOpen.IsLeftDrawerOpen = false;        //关闭左侧抽屉
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// 清空消息
        /// </summary>
        public void Clear()
        {
            ReceiveBytesCount = 0;
            SendBytesCount = 0;
            Messages.Clear();
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        public bool PublishMessage(string message)
        {
            try
            {
                //串口未打开 打开串口
                if (ComConfig.IsOpened == false)
                {
                    ShowErrorMessage("串口未打开");
                    ShowMessage("尝试打开串口...");
                    OpenCom();
                }

                //发送数据不能为空
                if (message is null || message.Length.Equals(0))
                {
                    ShowErrorMessage("发送的数据不能为空");
                    return false;
                }

                //验证数据字符必须符合16进制
                Regex regex = new(@"^[0-9 a-f A-F -]*$");
                if (!regex.IsMatch(message))
                {
                    ShowErrorMessage("数据字符仅限 0123456789 ABCDEF");
                    return false;
                }

                byte[] data;
                try
                {
                    data = message.Replace("-", string.Empty).GetBytes();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"数据转换16进制失败，发送数据位数量必须为偶数(16进制一个字节2位数)。");
                    return false;
                }

                if (SerialPort.IsOpen)
                {
                    try
                    {
                        #region MyRegion
                        //List<byte> crc = new();
                        ////根据选择进行CRC校验
                        //switch (CrcMode)
                        //{
                        //    //无校验
                        //    case CrcMode.None:
                        //        break;

                        //    //Modebus校验
                        //    case CrcMode.Modbus:
                        //        var code = Crc.Crc16Modbus(msg);
                        //        Array.Reverse(code);
                        //        crc.AddRange(code);
                        //        break;
                        //    default:
                        //        break;
                        //}

                        ////合并数组
                        //List<byte> list = new List<byte>();
                        //list.AddRange(msg);
                        //list.AddRange(crc);
                        //var data = list.ToArray(); 
                        #endregion


                        #region 测试发送数据所用的时间
                        //System.Diagnostics.Stopwatch oTime = new();   //定义一个计时对象  
                        //oTime.Start();                         //开始计时 
                        //SerialPort.Write(data, 0, data.Length);     //发送数据
                        //oTime.Stop();
                        //ShowMessage($"发送数据用时{oTime.Elapsed.TotalMilliseconds} ms");
                        #endregion


                        SerialPort.Write(data, 0, data.Length);     //发送数据
                        SendBytesCount += data.Length;                    //统计发送数据总数

                        if (!IsPause)
                            ShowSendMessage("", new ModbusRtuFrame(data).GetmessageWithErrMsg());
                        //ShowSendMessage(new ModbusRtuFrame(data).ToString(), mFrame.GetmessageWithErrMsg());
                        //ShowMessage(BitConverter.ToString(data).Replace("-", "").Replace(" ", "").InsertFormat(4, " "), MessageType.Send);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex.Message);
                    }
                }
                else
                {
                    ShowErrorMessage("串口未打开");
                }
                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        public async void ReceiveMessage(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //若串口未开启则返回
                if (!SerialPort.IsOpen)
                {
                    SerialPort?.DiscardInBuffer();//丢弃接收缓冲区的数据
                    return;
                }

                ComConfig.IsReceiving = true;

                //System.Diagnostics.Stopwatch oTime = new();   //定义一个计时对象  
                //oTime.Start();                         //开始计时 

                #region 接收数据
                //接收的数据缓存
                List<byte> list = new();
                if (ComConfig.IsOpened == false)
                    return;
                string msg = string.Empty;
                int times = 0;//计算次数 连续数ms无数据判断为一帧结束
                do
                {
                    if (ComConfig.IsOpened && SerialPort.BytesToRead > 0)
                    {
                        times = 0;
                        int dataCount = SerialPort.BytesToRead;          //获取数据量
                        byte[] tempBuffer = new byte[dataCount];         //声明数组
                        SerialPort.Read(tempBuffer, 0, dataCount); //从第0个读取n个字节, 写入tempBuffer 
                        list.AddRange(tempBuffer);                       //添加进接收的数据列表
                        msg += BitConverter.ToString(tempBuffer);
                        //限制一次接收的最大数量 避免多设备连接时 导致数据收发无法判断帧结束
                        if (list.Count > ComConfig.MaxLength)
                            break;
                    }
                    else
                    {
                        times++;
                        Thread.Sleep(1);
                    }
                } while (times < ComConfig.TimeOut);
                #endregion


                msg = msg.Replace('-', ' ');
                ReceiveFrameQueue.Enqueue(msg);//接收到的消息入队

                //搜索时将验证通过的添加至搜索到的设备列表
                if (SearchDeviceState == 1)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        CurrentDevice.ReceiveMessage = msg;
                        CurrentDevice.Address = int.Parse(msg[..2], System.Globalization.NumberStyles.HexNumber);
                        ModbusRtuDevices.Add(CurrentDevice);
                    }));
                    //HcGrowlExtensions.Success($"搜索到设备 {CurrentDevice.Address}...", viewName);
                }

                ReceiveBytesCount += list.Count;         //计算总接收数据量
                //若暂停更新接收数据 则不显示
                if (IsPause)
                    return;
                WaitUartReceived.Set();//置位数据接收完成标志
                //oTime.Stop();
                //ShowMessage($"接收数据用时{oTime.Elapsed.TotalMilliseconds} ms");
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, MessageType.Receive);
            }
            finally
            {
                ComConfig.IsReceiving = false;
            }
        }

        /// <summary>
        /// 解析接收的数据
        /// </summary>
        public void Analyse(List<byte> list)
        {
            //TODO 解析数据
            //目前仅支持03功能码

            //判断字节数为奇数还是偶数
            //偶数为主站请求
            if (list.Count % 2 == 0)
                return;
            //奇数为响应

            //验证数据是否为请求的数据 根据 从站地址 功能码 数据字节数量
            if (list[0] != DataMonitorConfig.SlaveId || list[1] != DataMonitorConfig.Function || list[2] != DataMonitorConfig.Quantity * 2)
                return;//非请求的数据

            var byteArr = list.ToArray();
            //将读取的数据写入
            for (int i = 0; i < DataMonitorConfig.Quantity; i++)
            {
                DataMonitorConfig.ModbusRtuDatas[i].Location = i * 2 + 3;         //在源字节数组中的起始位置 源字节数组为完整的数据帧,帧头部分3字节 每个值为1个word2字节
                DataMonitorConfig.ModbusRtuDatas[i].OriginValue = ConvertUtil.GetUInt16FromBigEndianBytes(byteArr, 3 + 2 * i);
                DataMonitorConfig.ModbusRtuDatas[i].OriginBytes = byteArr;        //源字节数组
                DataMonitorConfig.ModbusRtuDatas[i].ModbusByteOrder = DataMonitorConfig.ByteOrder; //字节序
                DataMonitorConfig.ModbusRtuDatas[i].UpdateTime = DateTime.Now;    //更新时间
            }
        }

        /// <summary>
        /// 打开串口 若串口未打开则打开串口 若串口已打开则关闭
        /// </summary>
        public void OperatePort()
        {
            try
            {
                if (SerialPort.IsOpen == false)
                {
                    //配置串口
                    SerialPort.PortName = ComConfig.Port.Key;                              //串口
                    SerialPort.BaudRate = (int)ComConfig.BaudRate;                         //波特率
                    SerialPort.Parity = (System.IO.Ports.Parity)ComConfig.Parity;          //校验
                    SerialPort.DataBits = ComConfig.DataBits;                              //数据位
                    SerialPort.StopBits = (System.IO.Ports.StopBits)ComConfig.StopBits;    //停止位
                    try
                    {
                        SerialPort.Open();               //打开串口
                        ComConfig.IsOpened = true;      //标记串口已打开
                        ShowMessage($"打开串口 {SerialPort.PortName} : {ComConfig.Port.Value}");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage($"打开串口失败, 该串口设备不存在或已被占用。{ex.Message}", MessageType.Error);
                        return;
                    }
                    //IsDrawersOpen.IsLeftDrawerOpen = false;
                }
                else
                {
                    try
                    {
                        SerialPort.Close();                   //关闭串口
                        ComConfig.IsOpened = false;          //标记串口已关闭
                        ShowMessage($"关闭串口{SerialPort.PortName}");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, MessageType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, MessageType.Error);
            }
            finally
            {

            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void CloseCom()
        {
            try
            {
                //若串口未开启则返回
                if (!ComConfig.IsOpened)
                {
                    return;
                }
                //停止自动读取
                if (DataMonitorConfig.IsOpened)
                {
                    CloseAutoRead();
                }

                ComConfig.IsOpened = false;          //标记串口已关闭
                                                     //SerialPort.DataReceived -= ReceiveMessage;
                SerialPort.Close();                   //关闭串口 
                ShowMessage($"关闭串口{SerialPort.PortName}");

                PublishFrameQueue.Clear();      //清空发送帧队列
                ReceiveFrameQueue.Clear();      //清空接收帧队列
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// 获取串口完整名字（包括驱动名字）
        /// </summary>
        public void GetComPorts()
        {
            //清空列表
            ComPorts.Clear();
            //查找Com口
            using (ManagementObjectSearcher searcher = new("select * from Win32_PnPEntity where Name like '%(COM[0-999]%'"))
            {
                var hardInfos = searcher.Get();
                foreach (var hardInfo in hardInfos)
                {
                    if (hardInfo.Properties["Name"].Value != null)
                    {
                        //获取名称
                        string deviceName = hardInfo.Properties["Name"].Value.ToString()!;
                        //从名称中截取串口
                        List<string> dList = new();
                        foreach (Match mch in Regex.Matches(deviceName, @"COM\d{1,3}").Cast<Match>())
                        {
                            string x = mch.Value.Trim();
                            dList.Add(x);
                        }

                        int startIndex = deviceName.IndexOf("(");
                        //int endIndex = deviceName.IndexOf(")");
                        //string key = deviceName.Substring(startIndex + 1, deviceName.Length - startIndex - 2);
                        string key = dList[0];
                        string name = deviceName[..(startIndex - 1)];
                        //添加进列表
                        ComPorts.Add(new KeyValuePair<string, string>(key, name));

                    }
                }
                if (ComPorts.Count != 0)
                {
                    //查找第一个USB设备
                    var usbDevice = ComPorts.FirstOrDefault(x => x.Value.ToLower().Contains("usb"));
                    //搜索结果不为空
                    if (usbDevice.Key != null)
                    {
                        //默认选中项 若含USB设备则指定第一个USB, 若不含USB则指定第一个
                        ComConfig.Port = usbDevice;
                    }
                    else
                    {
                        //没有usb设备则选中第一个
                        ComConfig.Port = ComPorts[0];
                    }
                }
                string str = $"获取串口成功, 共{ComPorts.Count}个。";
                foreach (var item in ComPorts)
                {
                    str += $"   {item.Key}: {item.Value};";
                }
                ShowMessage(str);
            }
        }

        protected void ShowErrorMessage(string message) => ShowMessage(message, MessageType.Error);
        protected void ShowReceiveMessage(string message, List<MessageSubContent> messageSubContents)
        {
            try
            {
                void action()
                {
                    var msg = new ModbusRtuMessageData("", DateTime.Now, MessageType.Receive);
                    foreach (var item in messageSubContents)
                    {
                        msg.MessageSubContents.Add(item);
                    }
                    Messages.Add(msg);
                    while (Messages.Count > 500)
                    {
                        Messages.RemoveAt(0);
                    }
                }
                Utils.ExecuteFunBeginInvoke(action);
            }
            catch (Exception) { }
        }

        protected void ShowSendMessage(string message, List<MessageSubContent> messageSubContents)
        {
            //ShowMessage(message, MessageType.Send);
            try
            {
                void action()
                {
                    var msg = new ModbusRtuMessageData("", DateTime.Now, MessageType.Send);
                    foreach (var item in messageSubContents)
                    {
                        msg.MessageSubContents.Add(item);
                    }
                    Messages.Add(msg);
                    while (Messages.Count > 260)
                    {
                        Messages.RemoveAt(0);
                    }
                }
                Utils.ExecuteFunBeginInvoke(action);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 界面显示数据
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        protected void ShowMessage(string message, MessageType type = MessageType.Info)
        {
            try
            {
                void action()
                {
                    Messages.Add(new ModbusRtuMessageData($"{message}", DateTime.Now, type));
                    while (Messages.Count > 260)
                    {
                        Messages.RemoveAt(0);
                    }
                }
                Utils.ExecuteFunBeginInvoke(action);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 界面显示数据
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        protected void ShowMessage(ModbusRtuMessageData msg)
        {
            try
            {
                void action()
                {
                    Messages.Add(msg);
                    while (Messages.Count > 260)
                    {
                        Messages.RemoveAt(0);
                    }
                }
                Utils.ExecuteFunBeginInvoke(action);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 导出配置文件
        /// </summary>
        public void ExportAutoResponseConfig()
        {
            try
            {
                //配置文件目录
                string dict = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuAutoResponseConfig");
                IOUtil.Exists(dict);
                Microsoft.Win32.SaveFileDialog sfd = new()
                {
                    Title = "请选择导出配置文件...",                                              //对话框标题
                    Filter = "json files(*.jsonARC)|*.jsonARC",    //文件格式过滤器
                    FilterIndex = 1,                                                         //默认选中的过滤器
                    FileName = "Default",                                           //默认文件名
                    DefaultExt = "jsonARC",                                     //默认扩展名
                    InitialDirectory = dict,                //指定初始的目录
                    OverwritePrompt = true,                                                  //文件已存在警告
                    AddExtension = true,                                                     //若用户省略扩展名将自动添加扩展名
                };
                if (sfd.ShowDialog() != true)
                    return;
                //将当前的配置序列化为json字符串
                var content = JsonConvert.SerializeObject(MosbusRtuAutoResponseDatas);
                //保存文件
                Common.Utils.WriteJsonFile(sfd.FileName, content);
                //HcGrowlExtensions.Success($"自动应答配置\"{Path.GetFileNameWithoutExtension(sfd.FileName)}\"导出成功", viewName);
                RefreshQuickImportList();//更新列表
            }
            catch (Exception ex)
            {
                //HcGrowlExtensions.Warning($"自动应答配置导出失败", viewName);
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 导入配置文件
        /// </summary>
        public void ImportAutoResponseConfig()
        {
            try
            {
                //配置文件目录
                string dict = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuAutoResponseConfig");
                IOUtil.Exists(dict);
                //选中配置文件
                OpenFileDialog dlg = new()
                {
                    Title = "请选择导入自动应答配置文件...",                                              //对话框标题
                    Filter = "json files(*.jsonARC)|*.jsonARC",    //文件格式过滤器
                    FilterIndex = 1,                                                         //默认选中的过滤器
                    InitialDirectory = dict
                };

                if (dlg.ShowDialog() != true)
                    return;
                var xx = Common.Utils.ReadJsonFile(dlg.FileName);
                MosbusRtuAutoResponseDatas = JsonConvert.DeserializeObject<ObservableCollection<ModbusRtuAutoResponseData>>(xx)!;
                RefreshModbusRtuDataDataView();//更新数据视图
                //HcGrowlExtensions.Success($"自动应答配置\"{Path.GetFileNameWithoutExtension(dlg.FileName)}\"导出成功", viewName);
            }
            catch (Exception ex)
            {
                //HcGrowlExtensions.Warning($"自动应答配置导入成功", viewName);
                ShowErrorMessage(ex.Message);
            }
        }



        /// <summary>
        /// 导出配置文件
        /// </summary>
        public void ExportConfig()
        {
            try
            {
                //配置文件目录
                string dict = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuConfig");
                IOUtil.Exists(dict);
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "请选择导出配置文件...",                                              //对话框标题
                    Filter = "json files(*.jsonDMC)|*.jsonDMC",    //文件格式过滤器
                    FilterIndex = 1,                                                         //默认选中的过滤器
                    FileName = "Default",                                           //默认文件名
                    DefaultExt = "jsonDMC",                                     //默认扩展名
                    InitialDirectory = dict,                //指定初始的目录
                    OverwritePrompt = true,                                                  //文件已存在警告
                    AddExtension = true,                                                     //若用户省略扩展名将自动添加扩展名
                };
                if (sfd.ShowDialog() != true)
                    return;
                //将当前的配置序列化为json字符串
                var content = JsonConvert.SerializeObject(DataMonitorConfig);
                //保存文件
                Common.Utils.WriteJsonFile(sfd.FileName, content);
                //ShowMessage("导出自动应答配置完成");
                //HcGrowlExtensions.Success("导出自动应答配置完成", viewName);
                RefreshQuickImportList();//更新列表
            }
            catch (Exception ex)
            {
                //HcGrowlExtensions.Warning("导出自动应答配置失败", viewName);
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 导入配置文件
        /// </summary>
        public void ImportConfig()
        {
            try
            {
                //配置文件目录
                string dict = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configs\ModbusRtuConfig");
                IOUtil.Exists(dict);
                //选中配置文件
                OpenFileDialog dlg = new()
                {
                    Title = "请选择导入配置文件...",                                              //对话框标题
                    Filter = "json files(*.jsonDMC)|*.jsonDMC",    //文件格式过滤器
                    FilterIndex = 1,                                                         //默认选中的过滤器
                    InitialDirectory = dict
                };

                if (dlg.ShowDialog() != true)
                    return;
                var xx = Common.Utils.ReadJsonFile(dlg.FileName);
                DataMonitorConfig = JsonConvert.DeserializeObject<DataMonitorConfig>(xx)!;
                RefreshModbusRtuDataDataView();//更新数据视图
                //ShowMessage("导入配置完成");
                //HcGrowlExtensions.Success($"配置\"{Path.GetFileNameWithoutExtension(dlg.FileName)}\"导入完成", viewName);
            }
            catch (Exception ex)
            {
                //HcGrowlExtensions.Warning("自动应答配置导入失败", viewName);
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 导入配置文件
        /// </summary>
        /// <param name="obj"></param>
        public void ImportConfig(ConfigFile obj)
        {
            try
            {
                var xx = Common.Utils.ReadJsonFile(obj.FullName);
                DataMonitorConfig = JsonConvert.DeserializeObject<DataMonitorConfig>(xx)!;
                if (DataMonitorConfig == null)
                {
                    ShowErrorMessage("读取配置文件失败");
                    return;
                }
                RefreshModbusRtuDataDataView();//更新数据视图
                //HcGrowlExtensions.Success($"配置\"{Path.GetFileNameWithoutExtension(obj.FullName)}\"导入完成", viewName);
            }
            catch (Exception ex)
            {
                //HcGrowlExtensions.Warning("自动应答配置导入失败", viewName);
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 更新快速导入配置列表
        /// </summary>
        public void RefreshQuickImportList()
        {
            try
            {
                DirectoryInfo Folder = new DirectoryInfo(ModbusRtuConfigDict);
                //var a = Folder.GetFiles().Where(x => x.Extension.ToLower().Equals(".jsondmc"));
                var a = Folder.GetFiles().Select(item => new ConfigFile(item));
                ConfigFiles.Clear();
                foreach (var item in a)
                {
                    ConfigFiles.Add(item);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("读取配置文件夹异常: " + ex.Message);
            }
        }



        /// <summary>
        /// 波特率选框修改时
        /// </summary>
        /// <param name="obj"></param>
        public void BaudRateSelectionChanged(object obj)
        {
            //获取选中项列表
            System.Collections.IList items = (System.Collections.IList)obj;
            var collection = items.Cast<BaudRate>();
            //获取所有选中项
            SelectedBaudRates = collection.ToList();
        }

        /// <summary>
        /// 停止搜索设备
        /// </summary>
        public void StopSearchDevices()
        {
            try
            {
                //若串口已打开则关闭
                if (ComConfig.IsOpened == true)
                {
                    OperatePort();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 自动搜索ModbusRtu设备
        /// </summary>
        public async void SearchDevices()
        {
            try
            {
                //若数据监控功能开启中则关闭
                if (DataMonitorConfig.IsOpened)
                {
                    //HcGrowlExtensions.Warning("数据监控功能关闭...", viewName);
                    CloseAutoRead();
                }

                //设置串口
                //修改串口设置
                if (SelectedBaudRates.Count.Equals(0))
                {
                    ShowErrorMessage("未选择要搜索的波特率");
                    return;
                }
                if (SelectedParitys.Count.Equals(0))
                {
                    ShowErrorMessage("未选择要搜索的校验位");
                    return;
                }

                //打开串口
                if (ComConfig.IsOpened == false)
                {
                    OpenCom();
                }

                //HcGrowlExtensions.Info("开始搜索...", viewName);
                //Growl.Info(new HandyControl.Data.GrowlInfo()
                //{
                //    WaitTime = 1,
                //    Message = "开始搜索...",
                //    //Token = "ModbusRtu"
                //});

                ComConfig.TimeOut = 20;//搜索时设置帧超时时间为20

                SearchDeviceState = 1;//标记状态为搜索设备中
                //清空搜索到的设备列表
                ModbusRtuDevices.Clear();

                //遍历选项

                //Flag:
                foreach (var baud in SelectedBaudRates)
                {
                    foreach (var parity in SelectedParitys)
                    {
                        //搜索
                        ShowMessage($"搜索: {ComConfig.Port.Key}:{ComConfig.Port.Value} 波特率:{(int)baud} 校验方式:{parity} 数据位:{ComConfig.DataBits} 停止位:{ComConfig.StopBits}");

                        for (int i = 0; i <= 255; i++)
                        {
                            //当前搜索的设备
                            CurrentDevice = new()
                            {
                                Address = i,
                                BaudRate = baud,
                                Parity = parity,
                                DataBits = ComConfig.DataBits,
                                StopBits = ComConfig.StopBits
                            };

                            //修改设置
                            SerialPort.BaudRate = (int)baud;
                            SerialPort.Parity = (System.IO.Ports.Parity)parity;
                            string unCrcMsg = $"{i:X2}0300000001";//读取第一个字
                            //串口关闭时或不处于搜索状态
                            if (ComConfig.IsOpened == false || SearchDeviceState != 1)
                                break;
                            PublishFrameEnqueue(GetModbusCrcedStr(unCrcMsg), ComConfig.SearchInterval);//发送消息入队
                            await Task.Delay(ComConfig.SearchInterval);           //间隔80ms后再请求下一个
                        }
                        if (ComConfig.IsOpened == false)
                            break;
                    }
                    if (ComConfig.IsOpened == false)
                        break;
                }
                if (ComConfig.IsOpened)
                {
                    await Task.Delay(1000);
                    ShowMessage("搜索完成");
                    CloseCom();
                }
                else
                {
                    ShowMessage("停止搜索");
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                SearchDeviceState = 2;//标记状态为搜索结束
            }
        }



        /// <summary>
        /// 发送消息帧入队
        /// </summary>
        /// <param name="msg">发送的消息</param>
        /// <param name="delay">发送完成后等待的时间,期间不会发送消息</param>
        public void PublishFrameEnqueue(string msg, int delay = 10)
        {
            if (msg == null)
            {
                return;
            }
            PublishFrameQueue.Enqueue((msg, delay));       //发布消息入队
            WaitPublishFrameEnqueue.Set();                 //置位发布消息入队标志
        }

        /// <summary>
        /// 复制Modbus数据帧
        /// </summary>
        /// <param name="obj"></param>
        public void CopyModbusRtuFrame(ModbusRtuMessageData obj)
        {
            try
            {
                string xx = string.Empty;
                foreach (var item in obj.MessageSubContents)
                {
                    xx += $"{item.Content} ";
                }
                Clipboard.SetDataObject(xx);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion


        #region **************************************** 数据帧处理 ****************************************
        /// <summary>
        /// 发送数据帧处理线程
        /// </summary>
        public async void PublishFrame()
        {
            WaitPublishFrameEnqueue.Reset();
            while (true)
            {
                //System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();   //定义一个计时对象  
                //oTime.Start();                         //开始计时 
                try
                {
                    //判断队列是否不空 若为空则等待
                    if (PublishFrameQueue.Count == 0)
                    {
                        WaitPublishFrameEnqueue.WaitOne();
                        continue;//需要再次验证队列是否为空
                    }
                    ComConfig.IsSending = true;
                    var frame = PublishFrameQueue.Dequeue();  //出队 数据帧
                    PublishMessage(frame.Item1);              //请求发送数据帧
                    await Task.Delay(frame.Item2);            //等待一段时间
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex.Message);
                }
                finally
                {
                    ComConfig.IsSending = false;
                }
                //oTime.Stop();                          //结束计时
                //ShowMessage($"{oTime.ElapsedMilliseconds} ms");
            }
        }

        /// <summary>
        /// 接收数据帧处理线程
        /// </summary>
        public void ReceiveFrame()
        {
            WaitUartReceived.Reset();
            while (true)
            {
                try
                {
                    //若无消息需要处理则进入等待
                    if (ReceiveFrameQueue.Count == 0)
                    {
                        WaitUartReceived.WaitOne(); //等待接收消息
                    }

                    //从接收消息队列中取出一条消息
                    var frame = ReceiveFrameQueue.Dequeue();
                    if (string.IsNullOrWhiteSpace(frame))
                    {
                        continue;
                    }
                    //实例化ModbusRtu帧
                    var mFrame = new ModbusRtuFrame(frame.GetBytes());

                    //对接收的消息直接进行crc校验
                    var crc = Crc.Crc16Modbus(frame.GetBytes());   //校验码 校验通过的为0000

                    #region 界面输出接收的消息 若校验成功则根据接收到内容输出不同的格式
                    if (IsPause)
                    {
                        //若暂停更新显示则不输出
                    }
                    else if (mFrame.Type.Equals(ModbusRtuFrameType.校验失败))
                    {
                        ShowReceiveMessage(mFrame.ToString(), mFrame.GetmessageWithErrMsg());
                        continue;
                    }
                    //校验成功
                    else
                    {
                        ShowReceiveMessage(mFrame.ToString(), mFrame.GetmessageWithErrMsg());
                    }
                    #endregion


                    #region 自动应答
                    if (IsAutoResponse)
                    {
                        //验证匹配哪一条规则
                        var xx = MosbusRtuAutoResponseDatas.FirstOrDefault(x => x.MateTemplate.ToLower().Replace(" ", "").Equals(frame.ToLower().Replace(" ", "")));
                        if (xx != null)
                        {
                            ShowMessage($"自动应答匹配: {xx.Name}");
                            PublishFrameEnqueue(xx.ResponseTemplate);      //自动应答
                        }
                    }
                    #endregion

                    List<byte> frameList = frame.GetBytes().ToList();//将字符串类型的数据帧转换为字节列表
                    int slaveId = frameList[0];                 //从站地址
                    int func = frameList[1];                    //功能码

                    #region 对接收的数据分功能码展示

                    //03功能码
                    if (mFrame.Type.Equals(ModbusRtuFrameType._0x03响应帧))
                    {
                        //若自动读取开启则解析接收的数据
                        if (DataMonitorConfig.IsOpened)
                        {
                            //验证数据是否为请求的数据 根据 从站地址 功能码 数据字节数量
                            if (frameList[0] == DataMonitorConfig.SlaveId && frameList[2] == DataMonitorConfig.Quantity * 2)
                            {
                                Analyse(frameList);
                            }
                        }
                    }

                    //0x10功能码
                    else if (mFrame.Type.Equals(ModbusRtuFrameType._0x10响应帧) && DataMonitorConfig.IsOpened)
                    {
                        ShowMessage("数据写入成功");
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex.Message);
                }
            }
        }

        /// <summary>
        /// 判断ModbusCrc校验是否通过
        /// </summary>
        /// <returns></returns>
        public bool IsModbusCrcVerifyPass(byte[] frame)
        {
            //对接收的消息直接进行crc校验
            var crc = Crc.Crc16Modbus(frame);   //校验码 校验通过的为0000
            //若校验结果不为0000则校验失败
            if (crc == null || !crc[0].Equals(0) || !crc[1].Equals(0))
                return false;
            //校验成功
            else
                return true;
        }

        /// <summary>
        /// ModbusRtu数据写入
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        public async void ModburRtuDataWrite(ModbusRtuData obj)
        {
            try
            {
                //todo 使用正则验证值为数值
                if (obj.WriteValue == null)
                {
                    ShowErrorMessage("写入值不能为空...");
                    return;
                }
                string addr = DataMonitorConfig.SlaveId.ToString("X2");         //从站地址
                string fun = "10";                                                    //0x10 写入多个寄存器
                string startAddr = obj.Addr.ToString("X4");                     //起始地址
                string jcqSl = (obj.DataTypeByteLength / 2).ToString("X4");     //寄存器数量
                string quantity = (obj.DataTypeByteLength).ToString("X2");      //字节数量
                double wValue = double.Parse(obj.WriteValue) / obj.Rate;              //对值的倍率做处理
                string dataStr = "";
                dynamic data = "";
                //根据设定的类型转换值
                switch (obj.Type)
                {
                    case DataType.uShort:
                        data = (ushort)wValue;
                        break;
                    case DataType.Short:
                        data = (short)wValue;
                        break;
                    case DataType.uInt:
                        data = (uint)wValue;
                        break;
                    case DataType.Int:
                        data = (int)wValue;
                        break;
                    case DataType.uLong:
                        data = (ulong)wValue;
                        break;
                    case DataType.Long:
                        data = (long)wValue;
                        break;
                    case DataType.Float:
                        data = (float)wValue;
                        break;
                    case DataType.Double:
                        data = (double)wValue;
                        break;
                    default:
                        break;
                }

                dataStr = BitConverter.ToString(ModbusRtuData.ByteOrder(BitConverter.GetBytes(data), obj.ModbusByteOrder)).Replace("-", "");

                //string unCrcFrame = addr + fun + startAddr + quantity;       //未校验的数据帧
                //var crc = Wu.Utils.Crc.Crc16Modbus(unCrcFrame.GetBytes());   //校验码
                //string frame = $"{addr} {fun} {startAddr} {jcqSl} {quantity} {dataStr} {crc[1]:X2}{crc[0]:X2}";
                string unCrcFrame = $"{addr} {fun} {startAddr} {jcqSl} {quantity} {dataStr}";

                ShowMessage("数据写入...");

                //请求发送数据帧 由于会失败, 请求多次
                PublishFrameEnqueue(GetModbusCrcedStr(unCrcFrame), 1000);
                //PublishFrameEnqueue(GetCrcedStr(unCrcFrame), 1000);
                //PublishFrameQueue.Enqueue(unCrcFrame);
                ////PublishFrameQueue.Enqueue(unCrcFrame);
                ////await Task.Delay(1000);
                //TaskDelayTime = 100;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        //TODO 数据写入处理  数据写入时 在列表内保存帧, 写入失败需要重新触发写入,至多3次  
        #endregion
    }
}