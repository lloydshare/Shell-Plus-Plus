using Commands;
using Core;
using Granados;
using Granados.IO;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace CLINETCommands
{
    public class ssh : ITerminalCommand
    {
        public string Name => "ssh";
        private string _currentLocation;
        private string _helpMessage = @"Usage of picview command:
    picview <file> :  Displays in console the <file> data.
";

        public void Execute(string arg)
        {
            // 1. UPDATE THESE
            var p = new SSHConnectionParameter("192.168.0.16", 22, SSHProtocol.SSH2, AuthenticationType.Password, "lloyd", "test");

            // 2. SOCKET
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(new IPEndPoint(IPAddress.Parse(p.HostName), p.PortNumber));

            // 3. CONNECT
            ISSHConnection conn = SSHConnection.Connect(s, p, c => new Reader());

            // 4. Create handler and capture the channel operator exposed to the handler
            var channelHandler = conn.OpenShell<SimpleChannelHandler>(c => new SimpleChannelHandler(c));

            Console.WriteLine("--- CONNECTED ---");

            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // handle Ctrl+C: forward as SSH break and then request shutdown
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true; // prevent process termination; we will shut down cleanly
                try
                {
                    channelHandler?.Channel?.SendBreak(0);
                }
                catch { }
                cts.Cancel();
            };

            // start background task to monitor console resize and notify remote
            Task.Run(() => {
                try
                {
                    int lastWidth = Console.WindowWidth;
                    int lastHeight = Console.WindowHeight;
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            int w = Console.WindowWidth;
                            int h = Console.WindowHeight;
                            if (w != lastWidth || h != lastHeight)
                            {
                                lastWidth = w;
                                lastHeight = h;
                                try
                                {
                                    channelHandler?.Channel?.ResizeTerminal((uint)w, (uint)h, 0u, 0u);
                                }
                                catch { }
                            }
                        }
                        catch { }
                        Thread.Sleep(250);
                    }
                }
                catch { }
            }, token);

            try
            {
                // 5. INPUT LOOP (non-blocking so we can exit on cancel)
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        byte[] data = Encoding.ASCII.GetBytes(new string(key.KeyChar, 1));

                        // Send expects a DataFragment on ISSHChannel
                        try
                        {
                            channelHandler?.Channel?.Send(new DataFragment(data, 0, data.Length));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("\n[Error] " + ex.Message);
                            break;
                        }
                    }
                    else
                    {
                        Thread.Sleep(25);
                    }
                }
            }
            finally
            {
                // clean shutdown
                try { channelHandler?.Channel?.SendEOF(); } catch { }
                try { conn.Close(); } catch { }
                try { s.Shutdown(SocketShutdown.Both); } catch { }
                try { s.Close(); } catch { }
            }
        }

        class Reader : ISSHConnectionEventHandler
        {
            public void OnData(DataFragment data) => Console.Write(Encoding.ASCII.GetString(data.Data, data.Offset, data.Length));
            public void OnError(Exception e) => Console.WriteLine("\n[Error] " + e.Message);
            public void OnConnectionClosed() => Environment.Exit(0);
            public void OnDebugMessage(bool alwaysDisplay, string message) { }
            public void OnIgnoreMessage(byte[] data) { }
            public void OnUnhandledMessage(byte type, byte[] data) { }
        }

        class SimpleChannelHandler : ISSHChannelEventHandler
        {
            // store the channel operator so the application code can send data
            public ISSHChannel Channel { get; }

            public SimpleChannelHandler(ISSHChannel channel)
            {
                Channel = channel;
            }

            public void OnData(DataFragment data) => Console.Write(Encoding.ASCII.GetString(data.Data, data.Offset, data.Length));
            public void OnError(Exception e) => Console.WriteLine("\n[Error] " + e.Message);
            public void OnReady() { }
            public void OnEstablished(DataFragment data) { }
            public void OnExtendedData(uint type, DataFragment data) { }
            public void OnClosing(bool b) { }
            public void OnClosed(bool b) { }
            public void OnEOF() { }
            public void OnRequestFailed() { }
            public void OnUnhandledPacket(byte type, DataFragment data) { }
            public void OnConnectionLost() { }
            public void Dispose() { }
        }
    }
}
