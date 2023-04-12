using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace WisdomTool.UI.Controls
{

    public class MaskedTextbox : TextBox
    {
        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(
            "Mask", typeof(string), typeof(MaskedTextbox), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMaskChanged));

        

        public string Mask
        {
            get { return (string)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextbox = d as MaskedTextbox;
            maskedTextbox?.FormatText();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            FormatText();
            // 将焦点设置到文本末尾
            SelectionStart = Text.Length;
        }


        private void FormatText()
        {
            Text = Regex.Replace(Text, Mask, "");
        }

        /// <summary>
        /// 有待商榷
        /// </summary>
        /// <param name ="e"></param>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            // 获取输入的字符
            string inputText = e.Text;

            // 计算插入字符后的文本
            string newText = Text.Substring(0, SelectionStart) + inputText + Text.Substring(SelectionStart + SelectionLength);

            // 如果插入字符后的文本不符合掩码，取消输入
            if (!string.IsNullOrEmpty(Mask) && !Regex.IsMatch(newText, Mask))
            {
                e.Handled = true;
                return;
            }

            base.OnTextInput(e);
        }
    }
}
