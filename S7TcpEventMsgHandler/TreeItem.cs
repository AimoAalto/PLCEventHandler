using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace S7TcpEventMsgHandler
{
	public class TreeItem
	{
		public string Value { get; set; }
		public DateTime Timestamp { get; set; }
		public SortedDictionary<string, TreeItem> Children { get; set; }

		public void Add(int index, string[] data, ConfigEventItem config, string path, DateTime dt)
		{
			bool last = true;
			int lastdata_index = index + 1;
			string key = data[index];
			Value = data[index];
			Timestamp = dt;
			if (data.Length > lastdata_index)
			{
				if (!string.IsNullOrEmpty(data[index + 1]))
				{
					if (!Children.ContainsKey(key)) Children.Add(key, new TreeItem() { Value = "", Children = new SortedDictionary<string, TreeItem>() });
					if (config.Children != null && config.Children.ContainsKey(key)) config = config.Children[key]; else config = new ConfigEventItem() { Children = new SortedDictionary<string, ConfigEventItem>(), FileDirectory = key, FileName = key + ".txt" };
					if (!string.IsNullOrEmpty(config.FileDirectory))
					{
						if (!path.EndsWith("\\")) path += "\\";
						path += config.FileDirectory;
					}
					Children[key].Add(index + 1, data, config, path, dt);
					last = false;
				}
			}
			if (last)
			{
				path = Config.Path(path);
				string filename = path + dt.ToString("yyyyMMdd") + "_" + (string.IsNullOrEmpty(config.FileName) ? key + ".txt" : config.FileName);
				File.AppendAllText(filename, dt.ToString("dd.MM.yyyy HH:mm:ss.fff") + " | " + Value + Environment.NewLine, Encoding.UTF8);
			}
		}

		public void Print(StringBuilder sb, string tabs)
		{
			if (Children.Count > 0)
			{
				foreach (string key in Children.Keys)
				{
					if (Children[key].Children.Count > 0)
					{
						sb.AppendLine($"{tabs}{Children[key].Value}");
						Children[key].Print(sb, $"{tabs}\t");
					}
					else
						sb.AppendLine($"{tabs}{key} = {Children[key].Value}");
				}
			}
			else
				sb.AppendLine(Value);
		}
	}
}
