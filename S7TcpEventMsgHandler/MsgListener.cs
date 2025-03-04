using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media;

namespace Common
{
    public class MsgListener
	{
		public Action OnConnectionCheck { get; set; }
		public Action<string> OnLogMessage { get; set; }
		public Action<int> OnNewData { get; set; }

		#region variables

		protected string _ip = "";
		protected int _port;
		protected string job;
		private TcpListener tcplistener;
		private Thread serverthread;
		private readonly byte[] readbuffer = new byte[10420];

		public bool WaitForConnection { get; set; }
		public int ErrCount { get; internal set; }
		public int Loopcounter { get; internal set; }
		public bool Stop { get; set; }
		public bool DoInit { get; set; }
		public bool Connected { get; internal set; }
		public byte[] ReceiveData { get { return readbuffer; } }

		public Color StatusColor
		{
			get
			{
				if (WaitForConnection)
					return Colors.Yellow;
				else if (Connected)
					return Colors.Green;
				else
					return Colors.LightGray;
			}
		}

		#endregion

		public MsgListener(int port = 50200)
		{
			_port = port;
			Connected = false;

			Stop = false;
			DoInit = true;
			ReStart("Init");
		}

		public void ReStart(string mode)
		{
			OnLogMessage?.Invoke($"Listener [{mode}]");

			Connected = false;
			WaitForConnection = true;

			if (DoInit)
			{
				if (serverthread == null)
				{
					serverthread = new Thread(DoWork) { IsBackground = true };
				}

				if (serverthread.ThreadState == ThreadState.Running)
				{
					Thread.Sleep(50);
					serverthread.Abort();
					serverthread.Join(50);
				}

				if (serverthread.ThreadState != ThreadState.Background)
					serverthread.Start();
			}
			else
			{
				if (tcplistener != null)
				{
					try
					{
						tcplistener.Server.Close();
						tcplistener.Stop();
					}
					catch (Exception ex)
					{
						OnLogMessage?.Invoke("Listener " + mode + " Error: " + ex.Message);
					}
				}

				tcplistener = new TcpListener(IPAddress.Any, _port);
				tcplistener.Server.ReceiveTimeout = 5000;
				try
				{
					tcplistener.Start();
					OnLogMessage?.Invoke($"TcpListener started [{_port}]");
				}
				catch (Exception ex)
				{
					OnLogMessage?.Invoke($"Listener {mode} Error: {ex.Message}");
				}
			}

			DoInit = false;
		}

		public void Abort()
		{
			if (tcplistener != null)
			{
				try
				{
					tcplistener.Server.Close();
					tcplistener.Stop();
				}
				catch (Exception ex)
				{
					OnLogMessage?.Invoke("Listener Abort Error: " + ex.Message);
				}
			}

			Stop = true;
			Connected = false;
		}

		protected void CheckConnection(TcpClient client)
		{
			IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections()
				.Where(x => x.LocalEndPoint.Equals(client.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
				.ToArray();

			if (tcpConnections != null && tcpConnections.Length > 0)
			{
				TcpState stateOfConnection = tcpConnections.First().State;
				Connected = (stateOfConnection == TcpState.Established);
				/*Connected = (stateOfConnection != TcpState.Unknown
					&& stateOfConnection != TcpState.Closed
					&& stateOfConnection != TcpState.DeleteTcb);*/
			}
			else
				Connected = false;
			OnConnectionCheck?.Invoke();
		}

		private readonly int maxreceivebuffersize = 8096;
		private int ReadAllFromStream(NetworkStream stream)
		{
			byte[] dummy = new byte[maxreceivebuffersize];
			int byteoffset = 0;
			try
			{
				int bytesread = -1;
				while (bytesread != 0)
				{
					if (stream.DataAvailable)
					{
						if (byteoffset + maxreceivebuffersize > readbuffer.Length)
						{
							bytesread = stream.Read(dummy, 0, maxreceivebuffersize);
							//System.Diagnostics.Trace.WriteLine($"read over read buffer size, bytes read {bytesread}");
						}
						else
						{
							bytesread = stream.Read(readbuffer, byteoffset, maxreceivebuffersize);
							byteoffset += bytesread;
						}
					}
					else
						bytesread = 0;
				}
				//if (sendbuffer != null) stream.Write(sendbuffer, 0, sendbuffer.Length);
			}
			catch (Exception ex)
			{
				//if (byteoffset > 0)
				OnLogMessage?.Invoke(string.Format("Listener : ReadAllFromStream Exception {0}: {1}", job, ex.StackTrace));
			}

			return byteoffset;
		}

		public void DoWork()
		{
			WaitForConnection = true;

			OnLogMessage?.Invoke("Listener thread started");

			ReStart("Start");

			try
			{
				while (!Stop)
				{
					OnLogMessage?.Invoke("Listener wait for connection");

					Connected = false;
					WaitForConnection = true;

					// wait for connection
					try
					{
						if (tcplistener == null) ReStart("Start");
						TcpClient client = tcplistener.AcceptTcpClient();

						// AddLog("Connection to: " + client.Client.RemoteEndPoint.AddressFamily);
						OnLogMessage?.Invoke("Listener Connection to: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);

						Connected = true;
						while (Connected)
						{
							WaitForConnection = false;
							Loopcounter++;
							try
							{
								job = "read";
								if (client.GetStream().DataAvailable)
								{
									int bytesread = ReadAllFromStream(client.GetStream());
									//System.Diagnostics.Trace.WriteLine($"bytes:{bytesread}");
									if (bytesread > 0)
										OnNewData?.Invoke(bytesread);
								}
								//job = "write";
								//if (sendBuffer.Count > 0) WriteFirst(client);
							}
							catch (Exception ex)
							{
								OnLogMessage?.Invoke(string.Format("S7 mockup WorkThread {0}: {1}{3}{2}", job, ex.Message, ex.StackTrace, Environment.NewLine));
								ErrCount++;
								ReStart("ReStart");
							}
							Thread.Sleep(10);
							CheckConnection(client);
						}
					}
					catch (Exception ex)
					{
						OnLogMessage?.Invoke(string.Format("Listener WorkThread {0}: {1}{3}{2}", job, ex.Message, ex.StackTrace, Environment.NewLine));
						ErrCount++;
						ReStart("ReStart");
					}
				}
			}
			finally
			{
				OnLogMessage?.Invoke("Listener Communication Thread closed");
				tcplistener.Server.Close();
				tcplistener.Stop();
				Connected = false;
				WaitForConnection = false;
			}
		}
	}
}
