#if DEBUG

using CSScript.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CSScript
{
    /// <summary>
    /// Представляет стенд для отладки скриптов.
    /// </summary>
    internal class _DebugScriptStand : ScriptContainer
    {
        public override void Execute()
        {

        }

#region --- СКРИПТОВЫЕ ФУНКЦИИ (версия 1.23) ---

        // Получение входящего аргумента по индексу
        private T GetArgument<T>(int index, T defaultValue)
        {
            if (env.Args.Length > index)
            {
                string value = env.Args[index];
                if (!string.IsNullOrEmpty(value))
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            return defaultValue;
        }

        // Вывод текста
        private void Write(object value, Color? foreColor = null)
        {
            env.Write(value, foreColor);
        }

        // Вывод текста с признаком конца строки
        private void WriteLine(object value, Color? foreColor = null)
        {
            env.WriteLine(value, foreColor);
        }

        // Вывод признака конца строки
        private void WriteLine()
        {
            env.WriteLine();
        }

        // Чтение текста из входного потока
        private string ReadLines()
        {
            return env.GetInputText();
        }

        // Чтение текста из входного потока с указанием заголовка
        private string ReadLines(string caption)
        {
            return env.GetInputText(caption);
        }

        // Запуск неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
        private int Start(string program, string args = null, bool printOutput = true, Color? outputColor = null, Encoding encoding = null)
        {
            Process process = new Process();
            return __StartProcess(process, program, args, printOutput, outputColor, encoding);
        }

        // Запуск контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится) 
        private int StartManaged(string program, string args = null, bool printOutput = true, Color? outputColor = null, Encoding encoding = null)
        {
            Process process = env.CreateManagedProcess();
            return __StartProcess(process, program, args, printOutput, outputColor, encoding);
        }

        // Настройки программы
        private Settings Settings
        {
            get
            {
                return Settings.Default;
            }
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
                    env.WriteLine("Не удалось удалить файл '" + fileName + "'", env.Settings.ErrorColor);
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
        private string GetMD5Hash(string filePath)
        {
            CheckFileExists(filePath, true);
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

        // Сравнение содержимого файлов
        private bool CompareFiles(string file1Path, string file2Path)
        {
            CheckFileExists(file1Path, true);
            CheckFileExists(file2Path, true);

            long file1Length = new FileInfo(file1Path).Length;
            long file2Length = new FileInfo(file2Path).Length;
            if (file1Length != file2Length)
            {
                return false;
            }

            string file1Hash = GetMD5Hash(file1Path);
            string file2Hash = GetMD5Hash(file2Path);
            if (file1Hash != file2Hash)
            {
                return false;
            }

            return true;
        }

        // Вывод исключения в случае, если файла не существует
        private void CheckFileExists(string filePath, bool checkRelativePath)
        {
            if (!File.Exists(filePath))
            {
                // игнорирование коротких путей файлов используется в случае,
                // если операционной системе известен путь к файлу, в отличие от программы
                // (например программы из папки WINDOWS\system32)
                if (checkRelativePath && !Path.IsPathRooted(filePath))
                {
                    throw new Exception("Не удаётся найти '" + filePath + "'.");
                }
            }
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

        private int __StartProcess(Process process, string program, string args, bool printOutput, Color? outputColor, Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }

            CheckFileExists(program, false);
            using (process)
            {
                ProcessStartInfo startInfo = process.StartInfo;
                startInfo.FileName = program;
                startInfo.Arguments = args;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (printOutput)
                {
                    startInfo.UseShellExecute = false; // в некоторых случаях выполнение при значении 'true' невозможно (например команда 'mode')
                    startInfo.RedirectStandardOutput = true; // перенаправление ввода/вывода невозможно без включенной опции 'UseShellExecute'
                }

                process.Start();
                if (printOutput)
                {
                    __AsyncStreamReader asyncReader = new __AsyncStreamReader(process.StandardOutput.BaseStream, 1024);
                    asyncReader.DataReceived += (byte[] buffer, int count) =>
                    {
                        string text = encoding.GetString(buffer, 0, count);
                        env.Write(text, outputColor);
                    };
                    asyncReader.BeginRead();
                }
                process.WaitForExit();
                return process.ExitCode;
            }
        }


        private class __AsyncStreamReader
        {
            private readonly Stream stream;
            private readonly byte[] buffer;
            public __AsyncStreamReader(Stream stream, int bufferSize)
            {
                this.stream = stream;
                buffer = new byte[bufferSize];
            }

            public delegate void DataReceivedHandler(byte[] buffer, int count);
            public event DataReceivedHandler DataReceived;

            public void BeginRead()
            {
                stream.BeginRead(buffer, 0, buffer.Length, AsyncCallback, null);
            }

            private void AsyncCallback(IAsyncResult ar)
            {
                int count = stream.EndRead(ar);
                if (count > 0)
                {
                    if (DataReceived != null)
                    {
                        DataReceived.Invoke(buffer, count);
                    }
                    stream.BeginRead(buffer, 0, buffer.Length, AsyncCallback, null);
                }
            }
        }

#endregion

        public _DebugScriptStand(IScriptEnvironment scriptEnvironment) : base(scriptEnvironment) { }
    }
}

#endif
