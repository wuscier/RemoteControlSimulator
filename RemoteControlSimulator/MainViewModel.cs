using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using Caliburn.Micro;
using RemoteControlSimulator.UiMessage;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Serilog;

namespace RemoteControlSimulator
{
    public class MainViewModel : ViewAware
    {

        public MainViewModel()
        {
            RemoteCommands = new ObservableCollection<RemoteCommand>();
            Status = EnumHelper.GetDescription(typeof(ConnectioinState), ConnectioinState.Disconnected);
        }

        private ClientSocket _client;
        private readonly object _syncRoot = new object();

        public void Interactive()
        {
            _client.SendCommand("1");
        }

        public void Discussion()
        {
            _client.SendCommand("2");
        }

        public void InteractiveWithoutLive()
        {
            _client.SendCommand("3");
        }

        public ObservableCollection<RemoteCommand> RemoteCommands { get; set; }

        public void Connect()
        {
            IPAddress serverIpAddress;
            int serverPort;

            if (string.IsNullOrEmpty(ServerIpAddress) || !IPAddress.TryParse(ServerIpAddress, out serverIpAddress))
            {
                MessageQueueManager.Instance.AddInfo("请填写有效的服务器IP地址！");
                return;
            }

            if (string.IsNullOrEmpty(Port) || !int.TryParse(Port, out serverPort))
            {
                MessageQueueManager.Instance.AddInfo("请填写有效的服务器监听端口！");
                return;
            }

            try
            {
                if (_client == null)
                {
                    _client = new ClientSocket(serverIpAddress, serverPort);
                    _client.ConnectionStateChangedEvent += _client_ConnectionStateChangedEvent;
                }
                else
                {
                    _client.ReConnectToServer(serverIpAddress, serverPort);
                }

                _client.CommandUpdatedEvent += Client_CommandUpdatedEvent;
                _client.SendCommand("0");

                if (_client.IsConnected)
                {
                    Status = EnumHelper.GetDescription(typeof(ConnectioinState), ConnectioinState.Connected);
                }
                //MessageQueueManager.Instance.AddInfo("连接成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Connect => {ex}");
                MessageQueueManager.Instance.AddInfo("连接失败！");
            }
        }

        private void _client_ConnectionStateChangedEvent(ConnectioinState connectioinState)
        {
            Status = EnumHelper.GetDescription(typeof(ConnectioinState), connectioinState);
        }

        private void Client_CommandUpdatedEvent(string command)
        {
            try
            {
                List<RemoteCommand> latestRemoteCommands = JsonConvert.DeserializeObject<List<RemoteCommand>>(command);

                lock (_syncRoot)
                {
                    if (RemoteCommands.Count == 0)
                    {
                        latestRemoteCommands.ForEach(rc =>
                        {
                            rc.ExecuteCommandAction = () => { _client.SendCommand(rc.Directive); };
                            RemoteCommands.Add(rc);
                        });
                    }
                    else
                    {
                        var joinedCmds = from remoteCmd in RemoteCommands
                            join latestCmd in latestRemoteCommands on remoteCmd.Directive equals latestCmd.Directive
                            select new {remoteCmd, latestCmd};

                        foreach (var joinedCmd in joinedCmds)
                        {
                            joinedCmd.remoteCmd.Enabled = joinedCmd.latestCmd.Enabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Client_CommandUpdatedEvent => {ex}");
                Log.Logger.Error($"Client_CommandUpdatedEvent => {ex}");
            }
        }

        private string _serverIpAddress;

        public string ServerIpAddress
        {
            get { return _serverIpAddress; }
            set
            {
                _serverIpAddress = value;
                NotifyOfPropertyChange(() => ServerIpAddress);
            }
        }

        private string _port;
        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                NotifyOfPropertyChange(() => Port);
            }
        }

        private string _status;

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyOfPropertyChange(() => Status);
            }
        }
    }
}
