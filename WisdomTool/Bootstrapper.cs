using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WisdomTool.ViewModels;

namespace WisdomTool
{
    public class Bootstrapper : BootstrapperBase
    {

        private readonly SimpleContainer _container = new SimpleContainer();
        public Bootstrapper()
        {

            Initialize();

            //LogManager.GetLog = type => new DebugLog(type);
        }

        protected override void Configure()
        {
            _container.Instance(_container);
            _container
              .Singleton<IWindowManager, WindowManager>()
              .Singleton<IEventAggregator, EventAggregator>();

            foreach (var assembly in SelectAssemblies())
            {
                assembly.GetTypes()
               .Where(type => type.IsClass)
               .Where(type => type.Name.EndsWith("ViewModel"))
               .ToList()
               .ForEach(viewModelType => _container.RegisterPerRequest(
                   viewModelType, viewModelType.ToString(), viewModelType));
            }

            _container.RegisterInstance(typeof(ModbusRtuViewModel), "A", new ModbusRtuViewModel());
            _container.RegisterInstance(typeof(ModbusRtuViewModel), "B", new ModbusRtuViewModel());
            _container.RegisterInstance(typeof(ModbusRtuViewModel), "C", new ModbusRtuViewModel());
            _container.RegisterInstance(typeof(A1ViewModel), "A1", new A1ViewModel());
            _container.RegisterInstance(typeof(OverToSingleViewModel), "OverToSingle", new OverToSingleViewModel());
            _container.RegisterInstance(typeof(StepModeViewModel), "StepMode1", new StepModeViewModel());
            //_container.Instance(new StepModeViewModel());
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            await DisplayRootViewForAsync(typeof(ShellViewModel));
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            //关闭WPF之后即关闭所有进程
            Environment.Exit(0);
        }
    }
}
