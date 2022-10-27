using System;
using System.Text;
using Topshelf;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Servicio061022
{
    internal class Program
    {

        static void Main(string[] args)
        {

            var exitcode = HostFactory.Run(x =>
            {
                x.Service<Updator>(s =>
                {
                    s.ConstructUsing(updator => new Updator());
                    s.WhenStarted(updator => updator.Start(args));
                    s.WhenStopped(updator => updator.Stop());
                });

                x.RunAsLocalSystem();

                x.SetServiceName("CuevAppsUpdatorService");
                x.SetDisplayName("CuevApps Updator Service");
                x.SetDescription("This service will update your CuevApps");

                
                 StartServer();
                 //return 0;
               



            });
        }

        public static void StartServer()
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            try
            {

                // Create a Socket that will use Tcp protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                Console.WriteLine("Waiting for a connection...");
                Socket handler = listener.Accept();

                // Incoming data from the client.
                string data = null;
                byte[] bytes = null;

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    //_installerProcess = (Process) BinarySerialization.Deserializate(bytes);
                    //Console.WriteLine("Proceso recibido");
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                    break;
                }

                Console.WriteLine("Text received : {0}", data);
                
                //LogWriter.PrintMessage("Starting the installer process at {0}", batchFilePath);
                //_installerProcess.Start();
                installBatch(data);
                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            //Console.WriteLine("\n Press any key to continue...");
            //Console.ReadKey();
        }

        private static void installBatch(string batchFilePath)
        {
            if (isWindows)
            {
                //Revisar
                Process _installerProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = batchFilePath,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                // start the installer process. the batch file will wait for the host app to close before starting.
                Console.WriteLine("Starting the installer process at {0}", batchFilePath);
                _installerProcess.Start();

            }
            else
            {
                // on macOS need to use bash to execute the shell script
                Console.WriteLine("Starting the installer script process at {0} via shell exec", batchFilePath);
                Exec(batchFilePath, false);
            }
        }

        private static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        protected static void Exec(string cmd, bool waitForExit = true)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var shell = "";
            try
            {
                // leave nothing up to chance :)
                shell = System.Environment.GetEnvironmentVariable("SHELL");
            }
            catch { }
            if (string.IsNullOrWhiteSpace(shell))
            {
                shell = "/bin/sh";
            }
            Console.WriteLine("Shell is {0}", shell);

            Process _installerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = shell,
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            Console.WriteLine("Starting the process via {1} -c \"{0}\"", escapedArgs, shell);
            _installerProcess.Start();
            if (waitForExit)
            {
                Console.WriteLine("Waiting for exit...");
                _installerProcess.WaitForExit();
            }
        }



    }
}
