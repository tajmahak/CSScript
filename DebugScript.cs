#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CSScript
{
    internal class DebugScript : ScriptRuntime
    {
        public override void StartScript(string arg)
        {
            string projectFolder = Environment.CurrentDirectory;
            string backupFolder = @"D:\Хранилище\Разработка\Проекты";
            BackupProjectDirectory(projectFolder, backupFolder);
        }

        private void BackupProjectDirectory(string projectDirectory, string backupDirectory)
        {
            CreateDirectory(backupDirectory);

            string[] directories = Directory.GetDirectories(projectDirectory);
            foreach (string directory in directories)
            {
                string archivePath = directory + ".7z";
                string archiveName = Path.GetFileName(archivePath);
                string backupArchivePath = Path.Combine(backupDirectory, archiveName);

                File.Delete(archivePath);

                WriteLog("Упаковка '" + archiveName + "'...");

                int archiveError = StartManaged(@"C:\Program Files\7-Zip\7z.exe",
                    new SevenZipArgs(directory + "\\*", archivePath).ToString(), false);

                if (archiveError == 0)
                {
                    WriteLog(" - успешно.", Settings.InfoColor);
                    string archiveHash = CalculateMD5Hash(archivePath);

                    string backupArchiveHash = null;
                    if (File.Exists(backupArchivePath))
                    {
                        backupArchiveHash = CalculateMD5Hash(backupArchivePath);
                    }

                    if (string.Equals(archiveHash, backupArchiveHash))
                    {
                        File.Delete(archivePath);
                        WriteLog(" Без изменений.", Settings.InfoColor);
                    }
                    else
                    {
                        File.Delete(backupArchivePath);
                        File.Move(archivePath, backupArchivePath);
                        WriteLog(" Выполнена замена.", Color.Green);
                    }
                }
                else
                {
                    WriteLog(" - с ошибками (код " + archiveError + ").", Settings.ErrorColor);
                    File.Delete(archivePath);
                }
                WriteLineLog();
            }
        }










        // --- СКРИПТОВЫЕ ФУНКЦИИ (версия 1.09) ---

        // Запуск неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
        private int Start(string program, string args = null, bool printOutput = true, Color? outputColor = null, int padCount = 0)
        {
            Process process = new Process();
            return __StartProcess(process, program, args, printOutput, outputColor, padCount);
        }

        // Запуск контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится) 
        private int StartManaged(string program, string args = null, bool printOutput = true, Color? outputColor = null, int padCount = 0)
        {
            Process process = CreateManagedProcess();
            return __StartProcess(process, program, args, printOutput, outputColor, padCount);
        }

        // Создание папки
        private bool CreateDirectory(string path, bool isFilePath = false)
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

        // Удаление списка файлов
        private int DeleteFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, searchPattern, searchOption);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
                return files.Length;
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                return 1;
            }
            return 0;
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
                    WriteLineLog("Не удалось удалить файл '" + fileName + "'", ScriptRuntime.Settings.ErrorColor);
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

        // Вычисление MD5 хэша для файла
        private string CalculateMD5Hash(string filePath)
        {
            __CheckFileExists(filePath);
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    hash = md5.ComputeHash(stream);
                }
            }
            StringBuilder hashString = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                hashString.Append(hash[i].ToString("x2"));
            }
            return hashString.ToString();
        }

        // Аргументы для командной строки 7-Zip
        private class SevenZipArgs
        {
            public SevenZipArgs()
            {

            }
            public SevenZipArgs(string input, string output)
            {
                Input = input;
                Output = output;
            }

            public string Input;
            public string Output;
            public int CompressionLevel = 9;
            public string Password;
            public bool EncryptFileStructure = true;

            public override string ToString()
            {
                StringBuilder stringArgs = new StringBuilder();
                stringArgs.Append("a"); // Добавление файлов в архив. Если архивного файла не существует, создает его
                stringArgs.Append(" \"" + Output + "\"");
                stringArgs.Append(" -ssw"); // Включить файл в архив, даже если он в данный момент используется. Для резервного копирования очень полезный ключ
                stringArgs.Append(" -mx" + CompressionLevel); // Уровень компрессии. 0 - без компрессии (быстро), 9 - самая большая компрессия (медленно)
                if (!string.IsNullOrEmpty(Password))
                {
                    stringArgs.Append(" -p" + Password); // Пароль для архива
                    if (EncryptFileStructure)
                    {
                        stringArgs.Append(" -mhe"); // Шифровать имена файлов
                    }
                }
                stringArgs.Append(" \"" + Input + "\"");
                return stringArgs.ToString();
            }
        }

        private int __StartProcess(Process process, string program, string args, bool printOutput, Color? outputColor, int padCount)
        {
            __CheckFileExists(program);
            using (process)
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
                        WriteLineLog(new string(' ', padCount) + e.Data, outputColor);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private void __CheckFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("Не удаётся найти '" + filePath + "'.");
            }
        }






    }
}

#endif
