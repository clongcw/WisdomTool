using System.Windows;
using System.Windows.Controls;

namespace WisdomTool.Views
{
    /// <summary>
    /// ModbusRtuView.xaml 的交互逻辑
    /// </summary>
    public partial class ModbusRtuView : UserControl
    {
        public ModbusRtuView()
        {
            InitializeComponent();
        }

        private void HiddenPUDrawer_Click(object sender, RoutedEventArgs e)
        {
            DrwMenu.IsOpen = false;
        }
    }
}
