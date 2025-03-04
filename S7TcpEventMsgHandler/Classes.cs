using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace S7TcpEventMsgHandler
{
	public interface IData
	{
		byte S { get; set; }
		short Ms { get; set; }
		byte Ident { get; set; }
		string ToString();
	}

	public class EHourData : IData
	{
		public byte S { get; set; }
		public short Ms { get; set; }
		public byte Ident { get; set; }
		public byte Hour { get; set; }
		public byte Min { get; set; }
		public override string ToString() { return $"<s:{S} ms:{Ms} i:{Ident} hour:{Hour} min:{Min}>"; }

		public byte[] GetBytes()
		{
			byte[] buffer = new byte[9];
			buffer[0] = 1;
			buffer[1] = (byte)'<';
			buffer[2] = S;
			buffer[3] = (byte)(Ms >> 8);
			buffer[4] = (byte)(Ms & 0x00FF);
			buffer[5] = 0;
			buffer[6] = Hour;
			buffer[7] = Min;
			buffer[8] = (byte)'>';
			return buffer;
		}

		public static EHourData DeSerialize(byte[] buffer, ref int startpos)
		{
			EHourData me = new EHourData()
			{
				// buffer[startpos+0] == '<'
				S = buffer[startpos + 1],
				Ms = (short)((buffer[startpos + 2] << 8) | buffer[startpos + 3]),
				Ident = buffer[startpos + 4],
				Hour = buffer[startpos + 5],
				Min = buffer[startpos + 6]
				// buffer[startpos+7] == '<'
			};
			if (me.S > 59) me.S = 59;
			if (me.Ms > 000) me.Ms = 999;
			if (me.Hour > 23) me.Hour = 23;
			if (me.Min > 59) me.Min = 59;
			startpos += 8;

			return me;
		}
	}

	public class ETimeData : IData
	{
		public byte S { get; set; }
		public short Ms { get; set; }
		public byte Ident { get; set; }
		public short Year { get; set; }
		public byte Mon { get; set; }
		public byte Day { get; set; }
		public byte Hour { get; set; }
		public byte Min { get; set; }
		public override string ToString() { return $"<s:{S} ms:{Ms} i:{Ident} year:{Year} mon:{Mon} day:{Day} hour:{Hour} min:{Min}>"; }

		public byte[] GetBytes()
		{
			byte[] buffer = new byte[13];
			buffer[0] = 1;
			buffer[1] = (byte)'<';
			buffer[2] = S;
			buffer[3] = (byte)(Ms >> 8);
			buffer[4] = (byte)(Ms & 0x00FF);
			buffer[5] = Ident;
			buffer[6] = (byte)(Year >> 8);
			buffer[7] = (byte)(Year & 0x00FF);
			buffer[8] = Mon;
			buffer[9] = Day;
			buffer[10] = Hour;
			buffer[11] = Min;
			buffer[12] = (byte)'>';
			return buffer;
		}

		public static ETimeData DeSerialize(byte[] buffer, ref int startpos)
		{
			ETimeData me = new ETimeData()
			{
				// buffer[startpos0] == '<'
				S = buffer[startpos + 1],
				Ms = (short)((buffer[startpos + 2] << 8) | buffer[startpos + 3]),
				Ident = buffer[startpos + 4],
				Year = (short)((buffer[startpos + 5] << 8) | buffer[startpos + 6]),
				Mon = buffer[startpos + 7],
				Day = buffer[startpos + 8],
				Hour = buffer[startpos + 9],
				Min = buffer[startpos + 10]
				// buffer[startpos+11] == '<'
			};
			if (me.S > 59) me.S = 59;
			if (me.Ms > 000) me.Ms = 999;
			if (me.Hour > 23) me.Hour = 23;
			if (me.Min > 59) me.Min = 59;
			if (me.Mon > 12) me.Mon = 12;
			if (me.Day > 31) me.Day = 31;

			if (me.Mon < 1) me.Mon = 1;
			if (me.Day < 1) me.Day = 1;
			startpos += 12;

			return me;
		}
	}

	public class EMachineData : IData
	{
		public byte S { get; set; }
		public short Ms { get; set; }
		public byte Ident { get; set; }
		public byte Machine { get; set; }
		public byte Area { get; set; }
		public byte Module { get; set; }
		public byte Function { get; set; }
		public byte Device { get; set; }
		public byte Identification { get; set; }
		public string Payload { get; set; }

		public override string ToString() { return $"<s:{S} ms:{Ms} i:{Ident} machine:{Machine} area:{Area} module:{Module} function:{Function} device:{Device} identification:{Identification} payload:{Payload}>"; }

		public byte[] GetBytes()
		{
			byte[] buffer = new byte[14 + Payload.Length];
			buffer[0] = 1;
			buffer[1] = (byte)'<';
			buffer[2] = S;
			buffer[3] = (byte)(Ms >> 8);
			buffer[4] = (byte)(Ms & 0x00FF);
			buffer[5] = Ident;
			buffer[6] = Machine;
			buffer[7] = Area;
			buffer[8] = Module;
			buffer[9] = Function;
			buffer[10] = Device;
			buffer[11] = Identification;
			int i = 12;
			for (int j = 0; j < Payload.Length; j++) buffer[i++] = (byte)Payload[j];
			buffer[i] = (byte)'>';
			return buffer;
		}

		public static EMachineData DeSerialize(byte[] buffer, int readbytes, ref int startpos)
		{
			EMachineData me = new EMachineData()
			{
				// buffer[0] == '<'
				S = buffer[startpos + 1],
				Ms = (short)((buffer[startpos + 2] << 8) | buffer[startpos + 3]),
				Ident = buffer[startpos + 4],
				Machine = buffer[startpos + 5],
				Area = buffer[startpos + 6],
				Module = buffer[startpos + 7],
				Function = buffer[startpos + 8],
				Device = buffer[startpos + 9],
				Identification = buffer[startpos + 10],
				Payload = ""
				// buffer[+] == '<'
			};
			if (me.S > 59) me.S = 59;
			if (me.Ms > 999) me.Ms = 999;
			if (me.S < 0) me.S = 1;
			int j = startpos + 11;
			for (; buffer[j] != '>' && j < readbytes; j++) me.Payload += (char)buffer[j];
			startpos = j + 1; // j points to '>'

			return me;
		}

		public void SaveEventFile(SortedDictionary<string, TreeItem> datatree, DateTime d, Config config)
		{
			string area = "";
			string module = "";
			string fun = "";
			string dev = "";

			string mac = (Machine.ToString());
			if (Area > 0)
			{
				area = "|" + (Area.ToString());
				if (Module > 0)
				{
					module = "|" + (Module.ToString());
					if (Function > 0)
					{
						fun = "|" + Function.ToString();
						if (Device > 0)
						{
							dev = "|" + Device.ToString();
						}
					}
				}
			}

			string[] metadata = (mac + area + module + fun + dev + "|" + Identification.ToString() + ":ä" + Payload).Split('|');
			string key = metadata[0];
			if (!datatree.ContainsKey(key)) datatree.Add(key, new TreeItem() { Value = "", Children = new SortedDictionary<string, TreeItem>() });
			ConfigEventItem ci;
			if (config.Children.ContainsKey(key)) ci = config.Children[key]; else ci = new ConfigEventItem() { Children = new SortedDictionary<string, ConfigEventItem>(), FileDirectory = key, FileName = key + ".txt" };
			string rootpath = Config.Path(config.RootDirectory);
			string path = rootpath + ci.FileDirectory;
			datatree[key].Add(1, metadata, ci, path, d);

			File.AppendAllText(rootpath + config.RootFile, Environment.NewLine + d.ToString("dd.MM.yyyy HH:mm:ss.fff") + " | " + ToString(), Encoding.UTF8);
		}
	}
}
