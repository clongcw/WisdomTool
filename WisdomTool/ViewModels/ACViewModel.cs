using Caliburn.Micro;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WisdomTool.ViewModels
{
    public class ACViewModel : Conductor<object>.Collection.OneActive
    {

        private readonly ACViewModel _modbusRtuViewModel;

        public ACViewModel()
        {
            // _modbusRtuViewModel = IoC.Get<ACViewModel>("D");
        }

        public async void OnMainMenuSelectionChanged(string viewname)
        {
            viewname = Regex.Match(viewname, @"[\S^:]*$").Value;//使用正则表达式匹配冒号后的所有非空字符

            //viewname = viewname.Substring(viewname.IndexOf(':') + 1).Trim();
            switch (viewname)
            {
                case "ModbusdRTU1": await Navigate(_modbusRtuViewModel); break;
            }
        }

        public async Task Navigate(Screen viewmodel)
        {
            await ActivateItemAsync(viewmodel, new CancellationToken());
        }
    }
}
