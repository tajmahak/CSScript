using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace CSScriptStand
{
    internal class Stand : CSScript.Core.ScriptContainer
    {
        public Stand(CSScript.Core.IScriptContext env) : base(env) { }

        public override void Start() {

        }



        #region --- СКРИПТОВЫЕ УТИЛИТЫ (02.01.2021) ---

        /// --- РАБОТА С КОНТЕКСТОМ ---

        // Получение входящего аргумента по индексу
        public T GetArgument<T>(int index, T defaultValue) {
            if (Context.Args.Length > index) {
                string value = Context.Args[index];
                if (!string.IsNullOrEmpty(value)) {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            return defaultValue;
        }

        // Вывод текста
        public void Write(object value, ConsoleColor? color = null) {
            Context.Write(value, color);
        }

        // Вывод текста с переносом строки
        public void WriteLine(object value, ConsoleColor? color = null) {
            Context.WriteLine(value, color);
        }

        // Вывод переноса строки
        public void WriteLine() {
            Context.WriteLine();
        }

        // Чтение текста из входного потока
        public string ReadLine(ConsoleColor? color = null) {
            return Context.ReadLine(color);
        }

        // Запуск неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
        public ProcessResult Start(string program, string args = null, bool redirectOutput = false, Encoding encoding = null) {
            Process process = new Process();
            return __StartProcess(process, program, args, redirectOutput, encoding);
        }

        // Запуск контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится) 
        public ProcessResult StartManaged(string program, string args = null, bool redirectOutput = false, Encoding encoding = null) {
            Process process = Context.CreateManagedProcess();
            return __StartProcess(process, program, args, redirectOutput, encoding);
        }

        // Получение текстового лога
        public string GetLog() {
            StringBuilder log = new StringBuilder();
            foreach (CSScript.Core.LogFragment logFragment in Context.Log) {
                log.Append(logFragment.Text);
            }
            return log.ToString();
        }

        // Получение лога в формате HTML (для отправки по E-Mail)
        public string GetHtmlLog() {
            StringBuilder log = new StringBuilder();
            //log.Append("<div style=\"background-color: "+ ColorTranslator.ToHtml(__GetColor(Colors.Background)) + ";\">");
            log.Append("<pre>");
            foreach (CSScript.Core.LogFragment logFragment in Context.Log) {
                if (logFragment.Color != Colors.Foreground && logFragment.Color != Colors.Background) {
                    log.Append("<font color=\"" + ColorTranslator.ToHtml(__GetColor(logFragment.Color)) + "\">" + logFragment.Text + "</font>");
                } else {
                    log.Append(logFragment.Text);
                }
            }
            log.Replace(Environment.NewLine, "<br>");
            log.Append("</pre>");
            //log.Append("</div>");

            return log.ToString();
        }


        /// --- РАБОТА С ФАЙЛАМИ / ПАПКАМИ ---

        // Создание папки
        public static bool CreateDirectory(string path, bool isFilePath = false) {
            string dirPath = isFilePath ? Path.GetDirectoryName(path) : path;
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
                return true;
            }
            return false;
        }

        // Добавление префикса к имени файла
        public static string AddNamePrefix(string path, string prefix) {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + prefix + fileName + ext;
        }

        // Добавление суффикса к имени файла
        public static string AddNameSuffix(string path, string suffix) {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + fileName + suffix + ext;
        }

        // Переименование файла (без учёта расширения)
        public static string RenameFile(string path, string newName) {
            string dirPath = Path.GetDirectoryName(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + newName + ext;
        }

        // Переименование файла (с учётом расширения)
        public static string RenameFileAndExtension(string path, string newName) {
            string dirPath = Path.GetDirectoryName(path);
            return dirPath + "\\" + newName;
        }

        // Удаление списка файлов
        public static int DeleteFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) {
            if (Directory.Exists(path)) {
                string[] files = Directory.GetFiles(path, searchPattern, searchOption);
                foreach (string file in files) {
                    File.Delete(file);
                }
                return files.Length;
            } else if (File.Exists(path)) {
                File.Delete(path);
                return 1;
            }
            return 0;
        }

        // Удаление старых файлов в папке (по дате изменения)
        public int DeleteOldFiles(string path, int remainCount, string searchPattern = null) {
            int deleted = 0;
            searchPattern = searchPattern ?? "*";
            string[] files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            SortFilesByDate(files, true);
            for (int i = remainCount; i < files.Length; i++) {
                string file = files[i];
                try {
                    File.Delete(file);
                    deleted++;
                } catch {
                    string fileName = Path.GetFileName(file);
                    WriteLine("Не удалось удалить файл '" + fileName + "'", Colors.Error);
                }
            }
            return deleted;
        }

        // Сортировка файлов по дате изменения (по умолчанию старые файлы в начале массива)
        public static void SortFilesByDate(string[] files, bool desc = false) {
            Dictionary<string, DateTime> fileDates = new Dictionary<string, DateTime>();
            foreach (string file in files) {
                fileDates.Add(file, new FileInfo(file).LastWriteTime);
            }
            if (!desc) {
                Array.Sort(files, (a, b) => fileDates[a].CompareTo(fileDates[b]));
            } else {
                Array.Sort(files, (a, b) => fileDates[b].CompareTo(fileDates[a]));
            }
        }

        // Вывод исключения в случае, если файла не существует
        public static void CheckFileExists(string filePath, bool checkRelativePath) {
            if (!File.Exists(filePath)) {
                // игнорирование коротких путей файлов используется в случае,
                // если операционной системе известен путь к файлу, в отличие от программы
                // (например программы из папки WINDOWS\system32)
                if (checkRelativePath && !Path.IsPathRooted(filePath)) {
                    throw new Exception("Не удаётся найти '" + filePath + "'.");
                }
            }
        }

        // Вычисление MD5 хэша для файла
        public static string GetMD5Hash(string filePath) {
            CheckFileExists(filePath, true);
            byte[] hash;
            using (MD5 md5 = MD5.Create()) {
                using (FileStream stream = File.OpenRead(filePath)) {
                    hash = md5.ComputeHash(stream);
                }
            }
            StringBuilder hashString = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) {
                hashString.Append(hash[i].ToString("x2"));
            }
            return hashString.ToString();
        }

        // Сравнение содержимого файлов
        public static bool CompareFiles(string file1Path, string file2Path) {
            CheckFileExists(file1Path, true);
            CheckFileExists(file2Path, true);

            long file1Length = new FileInfo(file1Path).Length;
            long file2Length = new FileInfo(file2Path).Length;
            if (file1Length != file2Length) {
                return false;
            }

            string file1Hash = GetMD5Hash(file1Path);
            string file2Hash = GetMD5Hash(file2Path);
            if (file1Hash != file2Hash) {
                return false;
            }

            return true;
        }

        // Получение абсолютного пути к файлу/директории
        public static string FullPath(string path) {
            return Path.GetFullPath(path);
        }


        /// --- ВАЛИДАЦИЯ ---

        // Проверка на выполнение условия, иначе Exception
        public static void ValidateIsTrue(bool condition, string message = null) {
            if (!condition) {
                throw message == null ? new Exception() : new Exception(message);
            }
        }

        // Проверка на не-null переменную, иначе Exception
        public static void ValidateIsNotNull(object obj, string message = null) {
            ValidateIsTrue(obj != null, message);
        }

        // Проверка на непустую строку, иначе Exception
        public static void ValidateIsNotBlank(string value, string message = null) {
            ValidateIsTrue(!string.IsNullOrEmpty(value), message);
        }


        /// --- КРИПТОГРАФИЯ ---

        // Выполняет симметричное шифрование с помощью алгоритма AES с использованием ключа (128/192/256 бит)
        public static byte[] EncryptAES(byte[] data, byte[] key) {
            using (Aes aes = Aes.Create()) {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.GenerateIV();

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
                    writer.Write(data.Length);
                    writer.Write(aes.IV);
                    writer.Write(__PerformCryptography(data, encryptor));
                    return ms.ToArray();
                }
            }
        }

        // Выполняет симметричное дешифрование с помощью алгоритма AES с использованием ключа (128/192/256 бит)
        public static byte[] DecryptAES(byte[] data, byte[] key) {
            using (Aes aes = Aes.Create()) {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms)) {
                    int dataLength = reader.ReadInt32();

                    aes.Key = key;
                    aes.IV = reader.ReadBytes(aes.IV.Length);

                    using (ICryptoTransform decryptor = aes.CreateDecryptor()) {
                        byte[] decryptData = reader.ReadBytes((int)(ms.Length - ms.Position));
                        decryptData = __PerformCryptography(decryptData, decryptor);

                        if (decryptData.Length != dataLength) {
                            Array.Resize(ref decryptData, dataLength);
                        }

                        return decryptData;
                    }
                }
            }
        }

        // Выполняет шифрование строки. Для расшифровки используется DecryptString
        public static string EncryptString(string data) {
            byte[] key = new byte[128 / 8];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) {
                random.GetBytes(key);
            }
            byte[] encrypt = EncryptAES(Encoding.UTF8.GetBytes(data), key);
            return Convert.ToBase64String(encrypt) + "-" + Convert.ToBase64String(key);
        }

        // Выполняет расшифровку строки, зашифрованной с помощью EncryptString
        public static string DecryptString(string encrypt) {
            string[] split = encrypt.Split('-');
            return Encoding.UTF8.GetString(DecryptAES(Convert.FromBase64String(split[0]), Convert.FromBase64String(split[1])));
        }


        /// --- ПРОЧЕЕ ---

        // Отправка электронного письма с использованием SMTP-сервера Mail.Ru
        public bool SendEmailFromMailRu(MailMessage message, string login, string password) {
            try {
                SmtpClient smtp = new SmtpClient("smtp.mail.ru", 25) {
                    Credentials = new NetworkCredential(login, password),
                    EnableSsl = true
                };
                smtp.Send(message);
                return true;
            } catch (Exception ex) {
                WriteLine("Не удалось отправить сообщение: " + ex.Message, Colors.Error);
                return false;
            }
        }

        // Аргументы для командной строки 7-Zip
        public class SevenZipArgs
        {
            public string Input { get; set; }
            public string Output { get; set; }
            public int CompressionLevel { get; set; }
            public string Password { get; set; }
            public bool EncryptFileStructure { get; set; }

            public SevenZipArgs() {
                CompressionLevel = 9;
                EncryptFileStructure = true;
            }
            public SevenZipArgs(string input, string output) : this() {
                Input = input;
                Output = output;
            }

            public override string ToString() {
                StringBuilder stringArgs = new StringBuilder();
                stringArgs.Append("a"); // Добавление файлов в архив. Если архивного файла не существует, создает его
                stringArgs.Append(" \"" + Output + "\"");
                stringArgs.Append(" -ssw"); // Включить файл в архив, даже если он в данный момент используется. Для резервного копирования очень полезный ключ
                stringArgs.Append(" -mx" + CompressionLevel); // Уровень компрессии. 0 - без компрессии (быстро), 9 - самая большая компрессия (медленно)
                if (!string.IsNullOrEmpty(Password)) {
                    stringArgs.Append(" -p" + Password); // Пароль для архива
                    if (EncryptFileStructure) {
                        stringArgs.Append(" -mhe"); // Шифровать имена файлов
                    }
                }
                stringArgs.Append(" \"" + Input + "\"");
                return stringArgs.ToString();
            }
        }


        /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

        // Результат выполнения процесса
        public class ProcessResult
        {
            public int ExitCode { get; set; }
            public string OutputText { get; set; }
            public string ErrorText { get; set; }
        }

        private ProcessResult __StartProcess(Process process, string program, string args, bool redirectOutput, Encoding encoding) {
            ProcessResult result = new ProcessResult();

            CheckFileExists(program, false);
            using (process) {
                ProcessStartInfo startInfo = process.StartInfo;
                startInfo.FileName = program;
                startInfo.Arguments = args;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                if (redirectOutput) {
                    startInfo.UseShellExecute = false; // перенаправление ввода/вывода невозможно без выключенной опции 'UseShellExecute'
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                }

                process.Start();
                process.WaitForExit();
                result.ExitCode = process.ExitCode;

                if (redirectOutput) {
                    encoding = encoding ?? Encoding.GetEncoding(866);
                    result.OutputText = new StreamReader(process.StandardOutput.BaseStream, encoding).ReadToEnd();
                    result.ErrorText = new StreamReader(process.StandardError.BaseStream, encoding).ReadToEnd();
                }
                return result;
            }
        }

        private static byte[] __PerformCryptography(byte[] data, ICryptoTransform cryptoTransform) {
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write)) {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return ms.ToArray();
            }
        }

        private static Color __GetColor(ConsoleColor consoleColor) {
            switch (consoleColor) {
                case ConsoleColor.Black: return Color.Black;
                case ConsoleColor.DarkBlue: return Color.DarkBlue;
                case ConsoleColor.DarkGreen: return Color.DarkGreen;
                case ConsoleColor.DarkCyan: return Color.DarkCyan;
                case ConsoleColor.DarkRed: return Color.DarkRed;
                case ConsoleColor.DarkMagenta: return Color.DarkMagenta;
                case ConsoleColor.DarkYellow: return Color.Orange;
                case ConsoleColor.Gray: return Color.Gray;
                case ConsoleColor.DarkGray: return Color.DarkGray;
                case ConsoleColor.Blue: return Color.Blue;
                case ConsoleColor.Green: return Color.Green;
                case ConsoleColor.Cyan: return Color.Cyan;
                case ConsoleColor.Red: return Color.Red;
                case ConsoleColor.Magenta: return Color.Magenta;
                case ConsoleColor.Yellow: return Color.Yellow;
                case ConsoleColor.White: return Color.White;
                default: throw new NotImplementedException();
            }
        }

        #endregion
    }
}
