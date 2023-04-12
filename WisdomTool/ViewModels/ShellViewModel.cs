using Caliburn.Micro;
using PropertyChanged;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WisdomTool.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ShellViewModel : Conductor<object>.Collection.OneActive
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

        public ShellViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public Task About()
        {
            //弹出对话框
            return _windowManager.ShowDialogAsync(IoC.Get<CategoryViewModel>());
        }

        protected async override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            //初始化界面
            await Navigate(IoC.Get<OverToSingleViewModel>("OverToSingle"));
        }

        public async void OnMainMenuSelectionChanged(string viewname)
        {
            viewname = Regex.Match(viewname, @"[\S^:]*$").Value;//使用正则表达式匹配冒号后的所有非空字符

            //viewname = viewname.Substring(viewname.IndexOf(':') + 1).Trim();
            switch (viewname)
            {
                case "ModbusdRTU1": await Navigate(IoC.Get<ModbusRtuViewModel>("A")); break;
                case "ModbusdRTU2": await Navigate(IoC.Get<ModbusRtuViewModel>("C")); break;
                case "StepMode": await Navigate(IoC.Get<StepModeViewModel>("StepMode1")); break;
                case "OverToSingle": await Navigate(IoC.Get<OverToSingleViewModel>("OverToSingle")); break;
            }
        }

        public async Task Navigate(Screen viewmodel)
        {
            await ActivateItemAsync(viewmodel, new CancellationToken());

        }

    }
}
