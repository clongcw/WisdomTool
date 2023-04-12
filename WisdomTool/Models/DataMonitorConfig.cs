﻿using Caliburn.Micro;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using Wisdom.Shared.Common;
using WisdomTool.Extensions;

namespace WisdomTool.Models;

[AddINotifyPropertyChangedInterface]
[JsonObject]
/// <summary>
/// ModbusRtu数据监控配置
/// </summary>
public class DataMonitorConfig : Screen
{
    /// <summary>
    /// 从站地址
    /// </summary>
    public int SlaveId
    {
        get => _SlaveId;
        set
        {
            Set(ref _SlaveId, value);
            NotifyOfPropertyChange(nameof(DataFrame));
        }
    }
    private int _SlaveId = 1;

    /// <summary>
    /// 功能码
    /// </summary>
    public int Function
    {
        get => _Function;
        set
        {
            Set(ref _Function, value);
            NotifyOfPropertyChange(nameof(DataFrame));
        }
    }
    private int _Function = 03;

    /// <summary>
    /// 起始地址 十进制
    /// </summary>
    public int StartAddr
    {
        get => _StartAddr;
        set
        {
            Set(ref _StartAddr, value);

            _StartAddrHex = value.ToString("X4");
            NotifyOfPropertyChange(nameof(StartAddrHex));

            NotifyOfPropertyChange(nameof(DataFrame));
        }
    }
    private int _StartAddr = 0;


    /// <summary>
    /// 起始地址Hex
    /// </summary>
    public string StartAddrHex
    {
        get => _StartAddrHex;
        set
        {
            Set(ref _StartAddrHex, value);

            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;
                _StartAddr = Convert.ToInt32(value, 16);
                NotifyOfPropertyChange(nameof(StartAddr));

                NotifyOfPropertyChange(nameof(DataFrame));
            }
            catch (Exception) { }

        }
    }
    private string _StartAddrHex = "0";

    /// <summary>
    /// 读取数量
    /// </summary>
    public int Quantity
    {
        get => _Quantity;
        set
        {
            Set(ref _Quantity, value);
            NotifyOfPropertyChange(nameof(DataFrame));
        }
    }
    private int _Quantity = 70;

    /// <summary>
    /// 周期
    /// </summary>
    public int Period { get => _Period; set => Set(ref _Period, value); }
    private int _Period = 1000;

    /// <summary>
    /// 字节序设置
    /// </summary>
    public ModbusByteOrder ByteOrder { get => _ByteOrder; set => Set(ref _ByteOrder, value); }
    private ModbusByteOrder _ByteOrder = ModbusByteOrder.DCBA;

    /// <summary>
    /// 发送数据帧
    /// </summary>
    public string DataFrame { get => _DataFrame; }
    private string _DataFrame => GetDataFrame();

    /// <summary>
    /// 获取发送数据帧
    /// </summary>
    /// <returns></returns>
    private string GetDataFrame()
    {
        string addr = SlaveId.ToString("X2");             //从站地址
        string fun = Function.ToString("X2");             //功能码
        string startAddr = StartAddr.ToString("X4");      //起始地址
        string quantity = Quantity.ToString("X4");        //数量
        string unCrcFrame = addr + fun + startAddr + quantity;       //未校验的帧
        var crc = Crc.Crc16Modbus(unCrcFrame.GetBytes());   //校验码
        return $"{addr} {fun} {startAddr} {quantity} {crc[1]:X2}{crc[0]:X2}";
    }

    /// <summary>
    /// 过滤数据, 没有设置Name的不显示
    /// </summary>
    public bool IsFilter { get => _IsFilter; set => Set(ref _IsFilter, value); }
    private bool _IsFilter = false;

    /// <summary>
    /// 打开状态
    /// </summary>
    [JsonIgnore]
    public bool IsOpened { get => _IsOpened; set => Set(ref _IsOpened, value); }
    private bool _IsOpened = false;

    /// <summary>
    /// 数据值
    /// </summary>
    public ObservableCollection<ModbusRtuData> ModbusRtuDatas { get => _ModbusRtuDatas; set => Set(ref _ModbusRtuDatas, value); }
    private ObservableCollection<ModbusRtuData> _ModbusRtuDatas = new();
}