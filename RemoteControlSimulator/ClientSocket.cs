using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using RemoteControlSimulator.UiMessage;
using Serilog;
using System.Timers;

namespace RemoteControlSimulator
{
    public class ClientSocket
    {
        public delegate void CommandUpdatedEventHandler(string command);

        public event CommandUpdatedEventHandler CommandUpdatedEvent;

        public delegate void ConnectionStateChangedEventHanlder(ConnectioinState connectioinState);

        public event ConnectionStateChangedEventHanlder ConnectionStateChangedEvent;

        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        private Socket _client;
        private Timer _timer;

        public bool IsConnected => _client != null && _client.Connected;

        private byte[] _inOptionValues;

        private void InitOptionValues()
        {
            uint dummy = 0;
            _inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(_inOptionValues, 0);
            BitConverter.GetBytes((uint)15000).CopyTo(_inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)15000).CopyTo(_inOptionValues, Marshal.SizeOf(dummy) * 2);
        }

        public ClientSocket(IPAddress address, int port)
        {
            InitOptionValues();

            Address = address;
            Port = port;

            InitCheckingTimer();
            ConnectToServer();
        }

        private void InitCheckingTimer()
        {
            _timer = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsConnected)
            {
                ConnectionStateChangedEvent?.Invoke(ConnectioinState.Connected);
            }
            else
            {
                ConnectionStateChangedEvent?.Invoke(ConnectioinState.Connecting);
                Console.WriteLine(@"trying to reconnect...");
                ConnectToServer();
            }
        }

        private void ConnectToServer(bool forceToReconnect = false)
        {
            try
            {
                if (forceToReconnect)
                {
                    CloseCurrentSocket();
                }

                if (_client == null)
                {
                    _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _client.IOControl(IOControlCode.KeepAliveValues, _inOptionValues, null);
                }

                _client.Connect(Address, Port);

                ReceiveCommand();
            }
            catch (SocketException exception)
            {
                Console.WriteLine(
                    $@"errorCode={exception.ErrorCode}, socketErrorCode={exception.SocketErrorCode}, msg={exception
                        .Message}");
                Log.Logger.Error(
                    $"ConnectToServer => forceToReconnect={forceToReconnect}, address={Address}, port={Port}");
            }
            catch (InvalidOperationException invalidOperation)
            {
                CloseCurrentSocket();

                Console.WriteLine($@"ConnectToServer => {invalidOperation.Message}");
            }
        }

        public void ReceiveCommand()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    byte[] dataBytes = new byte[4096];

                    string command;

                    try
                    {
                        var length = _client.Receive(dataBytes);
                        command = Encoding.UTF8.GetString(dataBytes, 0, length);

                        Console.WriteLine($@"ReceiveCommand => {command}");

                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            CommandUpdatedEvent?.Invoke(command);
                        }));
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.ConnectionReset ||
                            ex.SocketErrorCode == SocketError.ConnectionAborted)
                        {
                            CloseCurrentSocket();
                        }
                        Console.WriteLine($@"ReceiveCommand => {ex}");
                        Log.Logger.Error($"ReceiveCommand => {ex}");
                        break;
                    }
                }
            });
        }

        private void CloseCurrentSocket()
        {
            _client.Close();
            _client = null;
        }

        public void SendCommand(string directive)
        {
            try
            {
                byte[] dataBytes = Encoding.Default.GetBytes(directive);
                _client.Send(dataBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Logger.Error($"SendCommand => {ex}");
            }
        }

        public void ReConnectToServer(IPAddress newIpAddress, int newPort)
        {
            Address = newIpAddress;
            Port = newPort;

            ConnectToServer(true);
        }

    }
}
