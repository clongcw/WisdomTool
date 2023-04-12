using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wisdom.Shared.Common
{
    public static class ExcelHelper
    {
        /// <summary>
        /// 根据列数获取列名
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <returns></returns>
        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }


        /// <summary>
        /// 根据列名获取列数
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static int GetExcelColumn(string columnName)
        {
            int columnNumber = 0;
            foreach (char c in columnName)
            {
                columnNumber = columnNumber * 26 + (c - 'A' + 1);
            }
            return columnNumber;
        }

        /// <summary>
        /// 例如获取从"A"到"JG"的excel数组
        /// </summary>
        /// <param name="startcolumnname">"A"</param>
        /// <param name="endcolumnname">"JG"</param>
        /// <returns></returns>
        public static string[] GetColmunNames(string startcolumnname, string endcolumnname)
        {
            return Enumerable.Range(GetExcelColumn(startcolumnname), GetExcelColumn(endcolumnname) - GetExcelColumn(startcolumnname) + 1).Select(i => GetExcelColumnName(i)).ToArray();
        }


    }
}
