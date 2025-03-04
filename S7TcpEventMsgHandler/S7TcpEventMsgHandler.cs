using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace S7TcpEventMsgHandler
{
	public class S7TcpEventMsgHandler
	{
		#region variables

		Config config;
		private bool connectcmd = true;
		public MsgListener Listener { get; set; }
		private readonly SortedDictionary<string, TreeItem> datatree = new SortedDictionary<string, TreeItem>();
		int EventMin;
		int EventHour;
		int EventYear;
		int EventMonth;
		int EventDay;

		public Action<string> AddLogMessage { get; set; }
		public Action ConnCheck { get; set; }
		public int Port { get; set; }
		public SortedDictionary<string, TreeItem> Datatree { get { return datatree; } }

		#endregion

		public S7TcpEventMsgHandler()
		{
			DateTime now = DateTime.Now;
			EventMin = now.Minute;
			EventHour = now.Hour;
			EventYear = now.Year;
			EventMonth = now.Month;
			EventDay = now.Day;
			try
			{
				config = Config.ReadJson(@"C:\Lavaus\Asetukset\EventConfig.json");
			}
			catch (Exception)
			{
				// todo: exception handling
			}
		}

		public void SetValues2(int year, int mon, int day, int h, int m)
		{
			EventYear = year;
			EventMonth = mon;
			EventDay = day;
			EventHour = h;
			EventMin = m;
		}

		public void SetValues1(int h, int m)
		{
			EventHour = h;
			EventMin = m;
		}

		private void NewData(int bytesread)
		{
			List<IData> SplitMsg(byte[] bytes_, int length)
			{
				int msgcount = bytes_[0];
				int handled_msgcount = 0;

				List<IData> data_ = new List<IData>();
				int startpos = 1;
				// bytes_[startpos] == '<'
				// bytes_[startpos+1] == 'sec'
				// bytes_[startpos+2] == 'ms.1'
				// bytes_[startpos+3] == 'ms.2'
				do
				{
					byte _type = bytes_[startpos + 4];

					switch (_type)
					{
						case 0: // hour/minute
							data_.Add(EHourData.DeSerialize(bytes_, ref startpos));
							handled_msgcount++;
							break;

						case 1: // time
							data_.Add(ETimeData.DeSerialize(bytes_, ref startpos));
							handled_msgcount++;
							break;

						case 2: // time
							data_.Add(ETimeData.DeSerialize(bytes_, ref startpos));
							handled_msgcount++;
							break;

						default:
							data_.Add(EMachineData.DeSerialize(bytes_, length, ref startpos));
							handled_msgcount++;
							break;
					}
				} while ((handled_msgcount < msgcount && startpos < length));

				return data_;
			}

			void SaveToRootFile(string s_, DateTime d_)
			{
				try
				{
					Config c = new Config();
					string path = c.RootDirectory;
					if (!path.EndsWith("\\")) path += "\\";
					File.AppendAllText(path + c.RootFile, Environment.NewLine + d_.ToString("dd.MM.yyyy HH:mm:ss.fff") + " | " + s_, Encoding.UTF8);
				}
				catch (Exception x)
				{
					AddLogMessage?.Invoke("SaveMeta : " + x.Message + Environment.NewLine + x.InnerException);
				}
			}

			DateTime _start_ = DateTime.Now;

			List<IData> data = SplitMsg(Listener.ReceiveData, bytesread);
			if (data.Count == 0) return;

			AddLogMessage?.Invoke($"read {bytesread} | {Listener.ReceiveData:X}");
			int _eventSec = ((IData)data[0]).S;
			int _eventMs = ((IData)data[0]).Ms;
			AddLogMessage?.Invoke($"SS:{_eventSec}, MS:{_eventMs}");
			string lastDataStr = "";
			DateTime d = new DateTime(EventYear, EventMonth, EventDay, EventHour, EventMin, _eventSec, _eventMs);

			foreach (IData item in data)
			{
				switch (item.Ident)
				{
					case 0:
						try
						{
							EHourData e = (EHourData)item;
							lastDataStr = e.ToString();
							AddLogMessage?.Invoke(lastDataStr);
							SetValues1(e.Hour, e.Min);
							SaveToRootFile(lastDataStr, d);
						}
						catch (System.Exception x)
						{
							AddLogMessage?.Invoke("Minute Event Exception [{item}] : " + x.Message + Environment.NewLine + x.InnerException);
						}
						break;

					case 1:
						try
						{
							ETimeData e = (ETimeData)item;
							lastDataStr = e.ToString();
							AddLogMessage?.Invoke(lastDataStr);
							SetValues2(e.Year, e.Mon, e.Day, e.Hour, e.Min);
							SaveToRootFile(lastDataStr, d);
						}
						catch (System.Exception x)
						{
							AddLogMessage?.Invoke($"Daily Event Exception [{item}] : " + x.Message + Environment.NewLine + x.InnerException);
						}
						break;

					case 2:
						try
						{
							ETimeData e = (ETimeData)item;
							lastDataStr = e.ToString();
							AddLogMessage?.Invoke(lastDataStr);
							SetValues2(e.Year, e.Mon, e.Day, e.Hour, e.Min);
							SaveToRootFile(lastDataStr, d);
						}
						catch (System.Exception x)
						{
							AddLogMessage?.Invoke($"Master Event Exception [{item}] : " + x.Message + Environment.NewLine + x.InnerException);
						}
						break;

					case 3:
						try
						{
							EMachineData e = (EMachineData)item;
							lastDataStr = e.ToString();
							AddLogMessage?.Invoke(e.ToString());
							e.SaveEventFile(datatree, d, config);
						}
						catch (System.Exception x)
						{
							AddLogMessage?.Invoke($"Machine Event Exception [{item}] : " + x.Message + Environment.NewLine + x.InnerException);
						}
						break;

					case 10:
						break;

					default:
						break;
				}
			}

			AddLogMessage?.Invoke(string.Format("kesto : {0}ms", (DateTime.Now - _start_).TotalMilliseconds));
		}

		public void OnOff(int port)
		{
			Port = port;
			if (connectcmd)
			{
				if (Listener != null)
				{
					Listener.Abort();
					System.Threading.Thread.Sleep(50);
					Listener = null;
				}
				AddLogMessage?.Invoke("EventHandler : Connect, Port : " + Port.ToString());
				Listener = new MsgListener(Port) { OnLogMessage = AddLogMessage, OnConnectionCheck = ConnCheck, OnNewData = NewData };
				connectcmd = false;
			}
			else
			{
				if (Listener != null)
				{
					Listener.Abort();
					System.Threading.Thread.Sleep(50);
					Listener = null;
				}
				AddLogMessage?.Invoke("EventHandler : OnOff, Port : " + Port.ToString());
				connectcmd = false;
			}
		}
	}
}
