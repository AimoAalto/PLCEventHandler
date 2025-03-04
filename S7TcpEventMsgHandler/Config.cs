using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace S7TcpEventMsgHandler
{
	public class ConfigEventItem
	{
		public string FileName { get; set; }
		public string FileDirectory { get; set; }
		public SortedDictionary<string, ConfigEventItem> Children { get; set; } = new SortedDictionary<string, ConfigEventItem>();
	}

	public class Config
	{
		public string RootFile { get; set; }
		public string RootDirectory { get; set; }
		public SortedDictionary<string, ConfigEventItem> Children { get; set; } = new SortedDictionary<string, ConfigEventItem>();

		public Config()
		{
			RootDirectory = @"C:\Orfer\Events\";
			RootFile = "Root.txt";
		}

		public static Config ReadJson(string filename)
		{
			if (File.Exists(filename))
			{
				string s = File.ReadAllText(filename);
				return JsonConvert.DeserializeObject<Config>(s);
			}
			else
				return new Config();
		}

		public static string Path(string path)
		{
			if (!path.EndsWith("\\")) path += "\\";
			Directory.CreateDirectory(path);
			return path;
		}
	}
}
