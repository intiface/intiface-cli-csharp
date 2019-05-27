using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using Buttplug.Server;
using Buttplug.Server.Connectors;
using Buttplug.Server.Connectors.WebsocketServer;
using Google.Protobuf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IntifaceCLI
{
    internal class ServerCLI
    {
        private bool _useProtobufOutput;
        private DeviceManager _deviceManager;
        private readonly Stream _stdout = Console.OpenStandardOutput();
        private TaskCompletionSource<bool> _disconnectWait = new TaskCompletionSource<bool>();
        private readonly CancellationTokenSource _stdinTokenSource = new CancellationTokenSource();
        private Task _stdioTask;
        private bool _shouldExit;

        // Simple server that exposes device manager, since we'll need to chain device managers
        // through it for this. This is required because Windows 10 has problems disconnecting from
        // BLE devices without completely stopping and restarting processes. :(
        private class CLIServer : ButtplugServer
        {
            public IButtplugLogManager LogManager => BpLogManager;

            public CLIServer(string aServerName, uint aMaxPingTime, DeviceManager aDevMgr)
            : base(aServerName, aMaxPingTime, aDevMgr)
            {
            }
        }

        public ServerCLI()
        {
            Console.CancelKeyPress += (aObj, aEvent) =>
            {
                PrintProcessLog("Console exit called.");
                _shouldExit = true;
                _disconnectWait.SetResult(true);
            };
        }

        private void ReadStdio()
        {
            using (var stdin = Console.OpenStandardInput())
            {
                // Largest message we can receive is 1mb, so just allocate that now.
                var buf = new byte[1024768];

                while (!_disconnectWait.Task.IsCompleted && !_shouldExit)
                {
                    try
                    {
                        var msg = ServerControlMessage.Parser.ParseDelimitedFrom(stdin);

                        if (msg?.Stop == null && !_shouldExit)
                        {
                            continue;
                        }

                        _stdinTokenSource.Cancel();
                        _disconnectWait?.SetResult(true);
                        _shouldExit = true;
                        break;
                    }
                    catch (InvalidProtocolBufferException)
                    {
                        break;
                    }
                }
            }
        }

        private void SendProcessMessage(ServerProcessMessage aMsg)
        {
            if (!_useProtobufOutput)
            {
                return;
            }
            var arr = aMsg.ToByteArray();
            aMsg.WriteDelimitedTo(_stdout);
        }

        private void PrintProcessLog(string aLogMsg)
        {
            if (_useProtobufOutput)
            {
                var msg = new ServerProcessMessage { ProcessLog = new ServerProcessMessage.Types.ProcessLog { Message = aLogMsg } };
                SendProcessMessage(msg);
            }
            else
            {
                Console.WriteLine(aLogMsg);
            }
        }

        public void RunServer(Options aOptions)
        {
            if (aOptions.Version)
            {
                Console.WriteLine("1");
                return;
            }

            _useProtobufOutput = aOptions.FrontendPipe;
            if (_useProtobufOutput)
            {
                _stdioTask = new Task(ReadStdio);
                _stdioTask.Start();
            }

            if (aOptions.GenerateCertificate)
            {
                // CertUtils.GenerateSelfSignedCert(aOptions.CertFile, aOptions.PrivFile);
                Console.WriteLine("Cannot currently generate certificates.");
                return;
            }

            if (aOptions.DeviceConfigFile != null)
            {
                DeviceConfigurationManager.LoadBaseConfigurationFile(aOptions.DeviceConfigFile);
            }
            else
            {
                DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            }

            if (aOptions.UserDeviceConfigFile != null)
            {
                DeviceConfigurationManager.Manager.LoadUserConfigurationFile(aOptions.UserDeviceConfigFile);
            }

            if (aOptions.WebsocketServerInsecurePort == 0 && aOptions.WebsocketServerSecurePort == 0 && !aOptions.UseIpcServer)
            {
                PrintProcessLog("ERROR: Must specify either IPC server or Websocket server!");
                return;
            }

            var logLevel = ButtplugLogLevel.Off;
            if (aOptions.Log != null)
            {
                if (!Enum.TryParse(aOptions.Log, out logLevel))
                {
                    PrintProcessLog("ERROR: Invalid log level!");
                    return;
                }
            }

            ButtplugServer ServerFactory()
            {
                var server = new CLIServer(aOptions.ServerName, (uint)aOptions.PingTime, _deviceManager);

                // Pull out the device manager for reuse later.
                if (_deviceManager == null)
                {
                    _deviceManager = server.DeviceManager;
                }

                if (logLevel != ButtplugLogLevel.Off)
                {
                    server.LogManager.AddLogListener(logLevel, (aLogMsg) =>
                    {
                        PrintProcessLog(aLogMsg.LogMessage);
                    });
                }

                server.ClientConnected += (aObj, aEvent) =>
                {
                    if (_useProtobufOutput)
                    {
                        SendProcessMessage(new ServerProcessMessage
                        {
                            ClientConnected = new ServerProcessMessage.Types.ClientConnected
                            {
                                ClientName = server.ClientName
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Client connected: {server.ClientName}");
                    }
                };

                return server;
            }

            ButtplugIPCServer ipcServer = null;
            ButtplugWebsocketServer insecureWebsocketServer = null;
            ButtplugWebsocketServer secureWebsocketServer = null;

            if (aOptions.WebsocketServerInsecurePort != 0)
            {
                insecureWebsocketServer = new ButtplugWebsocketServer();
                insecureWebsocketServer.StartServerAsync(ServerFactory, 1, aOptions.WebsocketServerInsecurePort,
                    !aOptions.WebsocketServerAllInterfaces).Wait();
                insecureWebsocketServer.ConnectionClosed += (aSender, aArgs) => { _disconnectWait.SetResult(true); };
                PrintProcessLog("Insecure websocket Server now running...");
            }

            if (aOptions.WebsocketServerSecurePort != 0 && aOptions.CertFile != null &&
                aOptions.PrivFile != null)
            {
                secureWebsocketServer = new ButtplugWebsocketServer();
                secureWebsocketServer.StartServerAsync(ServerFactory, 1, aOptions.WebsocketServerSecurePort,
                    !aOptions.WebsocketServerAllInterfaces, aOptions.CertFile, aOptions.PrivFile).Wait();
                secureWebsocketServer.ConnectionClosed += (aSender, aArgs) => { _disconnectWait.SetResult(true); };
                PrintProcessLog("Secure websocket Server now running...");
            }

            if (aOptions.UseIpcServer)
            {
                ipcServer = new ButtplugIPCServer();
                ipcServer.StartServer(ServerFactory, aOptions.IpcPipe);
                ipcServer.ConnectionClosed += (aSender, aArgs) => { _disconnectWait.SetResult(true); };
                PrintProcessLog("IPC Server now running...");
            }

            // Now that all server possibilities are up and running, if we have a pipe, let the
            // parent program know we've started.
            if (_useProtobufOutput)
            {
                var msg = new ServerProcessMessage
                { ProcessStarted = new ServerProcessMessage.Types.ProcessStarted() };
                SendProcessMessage(msg);
            }
            else
            {
                Console.WriteLine("Server started, waiting for client connection.");
            }

            do
            {
                _disconnectWait.Task.Wait();

                if (ipcServer != null && ipcServer.Connected)
                {
                    ipcServer?.Disconnect();
                }

                if (insecureWebsocketServer != null && insecureWebsocketServer.Connected)
                {
                    insecureWebsocketServer?.DisconnectAsync().Wait();
                }

                if (secureWebsocketServer != null && secureWebsocketServer.Connected)
                {
                    secureWebsocketServer?.DisconnectAsync().Wait();
                }

                if (_useProtobufOutput)
                {
                    var msg = new ServerProcessMessage
                    { ClientDisconnected = new ServerProcessMessage.Types.ClientDisconnected() };
                    SendProcessMessage(msg);
                }
                else
                {
                    Console.WriteLine("Client disconnected.");
                }
                _disconnectWait = new TaskCompletionSource<bool>();
            } while (aOptions.StayOpen && !_shouldExit);

            if (!_useProtobufOutput)
            {
                return;
            }

            PrintProcessLog("Exiting");

            if (_useProtobufOutput)
            {
                var exitMsg = new ServerProcessMessage
                {
                    ProcessEnded = new ServerProcessMessage.Types.ProcessEnded()
                };
                SendProcessMessage(exitMsg);
            }

            _stdinTokenSource.Cancel();
        }
    }
}
