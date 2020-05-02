#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CSScript
{
	internal class DebugScript : ScriptRuntime
	{
		public override int RunScript(string arg)
		{
			var projectFolder = @"D:\Develop\VisualStudio\Projects";

			var directories = Directory.GetDirectories(projectFolder);
			foreach (var directory in directories)
			{
				var a = ToArchive(directory + "\\*", directory + ".7z");
			}




			return 0;
		}












		// --- СКРИПТОВЫЕ ФУНКЦИИ (версия 1.01) ---

		// Получение текущей рабочей папки
		private string WorkDirectory => Environment.CurrentDirectory;

		// Запуск программы
		private int Start(string program, string args = null, bool printOutput = true)
		{
			using (Process process = new Process())
			{
				process.StartInfo = new ProcessStartInfo()
				{
					FileName = program,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					CreateNoWindow = true,
				};
				process.OutputDataReceived += (sender, e) =>
				{
					if (printOutput)
					{
						ScriptRuntime.WriteLine(e.Data);
					}
				};
				process.Start();
				process.BeginOutputReadLine();
				process.WaitForExit();
				return process.ExitCode;
			}
		}

		// Упаковка в архив 7-Zip
		private int ToArchive(string input, string output, int compressLevel = 9, string password = null)
		{
			StringBuilder args = new StringBuilder();
			args.Append("a"); // Добавление файлов в архив. Если архивного файла не существует, создает его
			args.Append(" \"" + output + "\"");
			args.Append(" -ssw"); // Включить файл в архив, даже если он в данный момент используется. Для резервного копирования очень полезный ключ
			args.Append(" -mx" + compressLevel); // Уровень компрессии. 0 - без компрессии (быстро), 9 - самая большая компрессия (медленно)
			if (!string.IsNullOrEmpty(password))
			{
				args.Append(" -p" + password); // Пароль для архива
				args.Append(" -mhe"); // Шифровать имена файлов
			}
			args.Append(" \"" + input + "\"");

			string archiverPath = Path.GetFullPath("libs\\7z.exe");
			return Start(archiverPath, args.ToString());
		}

		// Создание папки
		private bool CreateDir(string path, bool isFilePath = false)
		{
			string dirPath = isFilePath ? Path.GetDirectoryName(path) : path;
			if (!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
				return true;
			}
			return false;
		}

		// Добавление префикса к имени файла
		private string AddNamePrefix(string path, string prefix)
		{
			string dirPath = Path.GetDirectoryName(path);
			string fileName = Path.GetFileNameWithoutExtension(path);
			string ext = Path.GetExtension(path);
			return dirPath + "\\" + prefix + fileName + ext;
		}

		// Добавление суффикса к имени файла
		private string AddNameSuffix(string path, string suffix)
		{
			string dirPath = Path.GetDirectoryName(path);
			string fileName = Path.GetFileNameWithoutExtension(path);
			string ext = Path.GetExtension(path);
			return dirPath + "\\" + fileName + suffix + ext;
		}

		// Переименование файла (без учёта расширения)
		private string RenameFile(string path, string newName)
		{
			string dirPath = Path.GetDirectoryName(path);
			string ext = Path.GetExtension(path);
			return dirPath + "\\" + newName + ext;
		}

		// Переименование файла (с учётом расширения)
		private string RenameFileAndExtension(string path, string newName)
		{
			string dirPath = Path.GetDirectoryName(path);
			return dirPath + "\\" + newName;
		}

		// Удаление старых файлов в папке (по дате изменения)
		private void DeleteOldFiles(string path, int remainCount, string searchPattern = null)
		{
			if (string.IsNullOrEmpty(searchPattern))
			{
				searchPattern = "*";
			}
			string[] files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
			string[] oldFiles = GetOldFiles(files, remainCount);
			foreach (string file in oldFiles)
			{
				try
				{
					File.Delete(file);
				}
				catch
				{
					string fileName = Path.GetFileName(file);
					ScriptRuntime.WriteLine("Не удалось удалить файл '" + fileName + "'", ScriptRuntime.Settings.ErrorColor);
				}
			}
		}

		// Получение списка старых файлов в папке (по дате изменения)
		private string[] GetOldFiles(string[] files, int remainCount)
		{
			List<KeyValuePair<string, DateTime>> fileList = new List<KeyValuePair<string, DateTime>>();
			foreach (string file in files)
			{
				DateTime fileDate = new FileInfo(file).LastWriteTime;
				KeyValuePair<string, DateTime> fileItem = new KeyValuePair<string, DateTime>(file, fileDate);
				fileList.Add(fileItem);
			}
			fileList.Sort((x, y) => y.Value.CompareTo(x.Value));
			fileList.RemoveRange(0, remainCount);
			string[] newFileList = new string[fileList.Count];
			for (int i = 0; i < fileList.Count; i++)
			{
				newFileList[i] = fileList[i].Key;
			}
			return newFileList;
		}




	}
}

#endif
