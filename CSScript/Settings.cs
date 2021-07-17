using System;
using System.IO;
using System.Text;

namespace CSScript
{
    public class Settings
    {
        public static Settings Default { get; set; } = new Settings();

        public string ScriptLibDirectory { get; set; } = "scriptlib";
        public string[] Usings { get; set; } = new string[0];
        public string[] Imports { get; set; } = new string[0];

        public static Settings FromFile(string path) {
            Settings config = new Settings();
            foreach (string srcLine in File.ReadAllLines(path, Encoding.UTF8)) {
                string line = srcLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) {
                    continue;
                }
                string[] split = DivideString(line, ":");
                string name = split[0].Trim();
                string value = split[1].Trim();

                switch (name) {
                    case "ScriptLibDirectory":
                        config.ScriptLibDirectory = value;
                        break;

                    case "Usings":
                        config.Usings = ParseArray(value);
                        break;

                    case "Imports":
                        config.Imports = ParseArray(value);
                        break;

                    default:
                        throw new Exception($"Неизвестный параметр \"{name}\"");
                }
            }
            return config;
        }


        private static string[] DivideString(string value, string separator) {
            int index = value.IndexOf(separator);
            return index == -1 ?
                (new string[] { value }) :
                (new string[] { value.Remove(index), value.Substring(index + separator.Length) });
        }

        private static string[] ParseArray(string value) {
            string[] array = value.Split(';');
            for (int i = 0; i < array.Length; i++) {
                array[i] = array[i].Trim();
            }
            return array;
        }
    }
}
