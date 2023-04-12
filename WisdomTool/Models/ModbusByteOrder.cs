namespace WisdomTool.Models;

/// ModbusRtu字节序
/// </summary>
public enum ModbusByteOrder : int
{
    ABCD,
    BADC,
    CDAB,
    DCBA
}