using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using Prism.Mvvm;
using Action = System.Action;

namespace RemoteControlSimulator
{
    public class RemoteCommand : BindableBase
    {
        public Action ExecuteCommandAction;

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string Directive { get; set; }

        private bool _enabled;

        public bool Enabled
        {
            get { return _enabled; }
            set { SetProperty(ref _enabled, value); }
        }

        public void SendCommand()
        {
            ExecuteCommandAction();
        }
    }
}
