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

        private readonly ModbusRtuViewModel _modbusRtuViewModel;
        private readonly ModbusRtuViewModel _modbusRtuViewModel2;
        private readonly StepModeViewModel _stepModeViewModel;

        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

        public ShellViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _modbusRtuViewModel = IoC.Get<ModbusRtuViewModel>("A");
            _modbusRtuViewModel2 = IoC.Get<ModbusRtuViewModel>("C");
            _stepModeViewModel = IoC.Get<StepModeViewModel>("StepMode1");
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
            //await Navigate(_modbusRtuViewModel);
            await Navigate(IoC.Get<OverToSingleViewModel>("OverToSingle"));
        }

        public async void OnMainMenuSelectionChanged(string viewname)
        {
            viewname = Regex.Match(viewname, @"[\S^:]*$").Value;//使用正则表达式匹配冒号后的所有非空字符

            //viewname = viewname.Substring(viewname.IndexOf(':') + 1).Trim();
            switch (viewname)
            {
                case "ModbusdRTU1": await Navigate(_modbusRtuViewModel); break;
                case "ModbusdRTU2": await Navigate(_modbusRtuViewModel2); break;
                case "StepMode": await Navigate(_stepModeViewModel); break;
                case "OverToSingle": await Navigate(IoC.Get<OverToSingleViewModel>("OverToSingle")); break;
            }
        }

        public async Task Navigate(Screen viewmodel)
        {
            await ActivateItemAsync(viewmodel, new CancellationToken());

        }

    }
}
