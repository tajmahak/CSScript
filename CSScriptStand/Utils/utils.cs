using CSScript.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

// utils
// УТИЛИТЫ
// ------------------------------------------------------------

//## #import System.ComponentModel.DataAnnotations.dll

//## #init
//## __Utils_Init(Context);

//## #class

// Утилиты общей направленности
public static class __Utils //##
{ //##

    // --- РАБОТА С КОНТЕКСТОМ ---

    // Установка режима ожидания действий со стороны пользователя
    public static void Pause(bool pause = true) {
        __utils_Context.Pause = pause;
    }

    // Получение входящего аргумента по индексу
    public static T GetArgument<T>(int index, T defaultValue) {
        if (__utils_Context.Args.Length > index) {
            string value = __utils_Context.Args[index];
            if (!string.IsNullOrEmpty(value)) {
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        return defaultValue;
    }

    // Вывод текста в контекст
    public static void Write(object value, ConsoleColor? color = null) {
        __utils_Context.Write(value, color);
    }
    public static void Write(StreamReader reader, ConsoleColor? color = null) {
        char[] buffer = new char[1024];
        while (!reader.EndOfStream) {
            int readed = reader.Read(buffer, 0, buffer.Length);
            string value = new string(buffer, 0, readed);
            Write(value, color);
        }
    }

    // Вывод текста с переносом строки в контекст
    public static void WriteLine(object value, ConsoleColor? color = null) {
        __utils_Context.WriteLine(value, color);
    }
    public static void WriteLine() {
        __utils_Context.WriteLine();
    }

    // Асинхронный вывод текста с переносом строки в контекст
    public static void BeginWrite(StreamReader reader, ConsoleColor? color = null) {
        Encoding encoding = reader.CurrentEncoding;
        __Utils_BeginReadFromStream(reader.BaseStream, (data) => {
            string value = encoding.GetString(data);
            Write(value, color);
        });
    }

    // Вывод текста ошибки в контекст
    public static void WriteError(object value) {
        __utils_Context.WriteError(value);
    }
    public static void WriteError(StreamReader reader) {
        char[] buffer = new char[1024];
        while (!reader.EndOfStream) {
            int readed = reader.Read(buffer, 0, buffer.Length);
            string value = new string(buffer, 0, readed);
            WriteError(value);
        }
    }

    // Вывод текста ошибки с переносом строки в контекст
    public static void WriteErrorLine(object value) {
        __utils_Context.WriteErrorLine(value);
    }
    public static void WriteErrorLine() {
        __utils_Context.WriteErrorLine();
    }

    // Асинхронный вывод текста с переносом строки в контекст
    public static void BeginWriteError(StreamReader reader) {
        Encoding encoding = reader.CurrentEncoding;
        __Utils_BeginReadFromStream(reader.BaseStream, (data) => {
            string value = encoding.GetString(data);
            WriteError(value);
        });
    }

    // Вывод штампа даты и времени в стандартный выходной поток
    public static void WriteTimeStamp(string pattern = "[yyy.MM.dd HH:mm:ss]: ") {
        Write(DateTime.Now.ToString(pattern), __utils_Context.ColorScheme.Info);
    }

    // Вывод штампа даты и времени в стандартный поток ошибок
    public static void WriteErrorTimeStamp(string pattern = "[yyy.MM.dd HH:mm:ss]: ") {
        WriteError(DateTime.Now.ToString(pattern));
    }

    // Вывод форматированной строки формата "... `COLOR:VALUE` ...", где:
    //  COLOR - цвет, поддерживаемый консолью;
    //  VALUE - значение, которое нужно вывести в указанном цвете.
    public static void WriteFormatted(string helpText, ConsoleColor? color = null) {
        string[] split = helpText.Split('`');
        for (int i = 0; i < split.Length; i++) {
            ConsoleColor? writeColor = color;
            bool newBlock = i % 2 != 0;
            string fragment = split[i];
            if (newBlock) {
                string[] fragmentBlock = DivideString(fragment, ":");
                Validate.True(fragmentBlock.Length == 2, "Не удалось распознать строку форматирования '" + fragment + "'.");
                fragment = fragmentBlock[1];
                switch (fragmentBlock[0].ToLowerInvariant()) {
                    case "black":
                        writeColor = ConsoleColor.Black;
                        break;
                    case "darkblue":
                        writeColor = ConsoleColor.DarkBlue;
                        break;
                    case "darkgreen":
                        writeColor = ConsoleColor.DarkGreen;
                        break;
                    case "darkcyan":
                        writeColor = ConsoleColor.DarkCyan;
                        break;
                    case "darkred":
                        writeColor = ConsoleColor.DarkRed;
                        break;
                    case "darkmagenta":
                        writeColor = ConsoleColor.DarkMagenta;
                        break;
                    case "darkyellow":
                        writeColor = ConsoleColor.DarkYellow;
                        break;
                    case "gray":
                        writeColor = ConsoleColor.Gray;
                        break;
                    case "darkgray":
                        writeColor = ConsoleColor.DarkGray;
                        break;
                    case "blue":
                        writeColor = ConsoleColor.Blue;
                        break;
                    case "green":
                        writeColor = ConsoleColor.Green;
                        break;
                    case "cyan":
                        writeColor = ConsoleColor.Cyan;
                        break;
                    case "red":
                        writeColor = ConsoleColor.Red;
                        break;
                    case "magenta":
                        writeColor = ConsoleColor.Magenta;
                        break;
                    case "yellow":
                        writeColor = ConsoleColor.Yellow;
                        break;
                    case "white":
                        writeColor = ConsoleColor.White;
                        break;
                    default:
                        throw new Exception("Не удалось распознать идентификатор цвета '" + fragmentBlock[0] + "'.");
                }
            }
            Write(fragment, writeColor);
        }
    }

    // Вывод форматированной строки формата "... `COLOR:VALUE` ...", где:
    //  COLOR - цвет, поддерживаемый консолью;
    //  VALUE - значение, которое нужно вывести в указанном цвете.
    public static void WriteFormattedLine(string heplLine, ConsoleColor? color = null) {
        WriteFormatted(heplLine + Environment.NewLine, color);
    }

    // Чтение текста из входного потока
    public static string ReadLine(ConsoleColor? color = null) {
        return __utils_Context.ReadLine(color);
    }

    // Получение текстового лога
    public static string GetLog() {
        StringBuilder log = new StringBuilder();
        foreach (LogFragment logFragment in __utils_Context.Log) {
            log.Append(logFragment.Text);
        }
        return log.ToString();
    }

    // Получение лога в формате HTML (для отправки по E-Mail)
    public static string GetHtmlLog() {
        StringBuilder log = new StringBuilder();
        //log.AppendFiles("<div style=\"background-color: "+ ColorTranslator.ToHtml(__GetColor(Colors.Background)) + ";\">");
        log.Append("<pre>");
        foreach (LogFragment logFragment in __utils_Context.Log) {
            if (logFragment.Color != __utils_Context.ColorScheme.Foreground) {
                log.Append("<font color=\"" + System.Drawing.ColorTranslator.ToHtml(__Utils_ConvertColor(logFragment.Color)) + "\">" + logFragment.Text + "</font>");
            }
            else {
                log.Append(logFragment.Text);
            }
        }
        log.Replace(Environment.NewLine, "<br>");
        log.Append("</pre>");
        //log.AppendFiles("</div>");

        return log.ToString();
    }


    // --- РАБОТА С ПРОЦЕССАМИ ---

    // Создание неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
    public static ScriptProcess CreateProcess(string fileName, object args = null) {
        Validate.FileExists(fileName, false);
        string stringArgs = args == null ? null : args.ToString();
        return new ScriptProcess(fileName, stringArgs);
    }

    // Создание контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится)
    public static ScriptProcess CreateManagedProcess(string fileName, object args = null) {
        ScriptProcess process = CreateProcess(fileName, args);
        __utils_Context.RegisterProcess(process);
        return process;
    }

    // Запуск процесса с выводом потоков в консоль и ожиданием его завершения
    public static int StartProcess(string fileName, object args = null, Encoding outputEncoding = null) {
        ScriptProcess process = CreateManagedProcess(fileName, args);
        return StartProcess(process, outputEncoding);
    }
    public static int StartProcess(ScriptProcess process, Encoding outputEncoding = null) {
        process.RedirectOutput();
        process.StartAndReturn();

        BeginWrite(process.GetOutput(outputEncoding), __utils_Context.ColorScheme.Info);
        BeginWriteError(process.GetError(outputEncoding));

        process.WaitForExit();
        return process.ExitCode;
    }

    // Запуск процесса в отдельном окне и ожиданием его завершения
    public static int StartNormalProcess(string fileName, object args = null) {
        ScriptProcess process = CreateManagedProcess(fileName, args);
        process.StartAndWaitForExit();
        return process.ExitCode;
    }

    // Запуск скрытого процесса и ожидание его завершения
    public static int StartHiddenProcess(string fileName, object args = null) {
        ScriptProcess process = CreateManagedProcess(fileName, args);
        process.Hidden();
        process.StartAndWaitForExit();
        return process.ExitCode;
    }


    // --- РАБОТА С ФАЙЛАМИ / ПАПКАМИ ---

    // Создание папки
    public static bool CreateDirectory(string path, bool isFilePath = false) {
        string dirPath = isFilePath ? Path.GetDirectoryName(path) : path;
        if (Empty(dirPath)) {
            dirPath = Environment.CurrentDirectory;
        }
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
            return true;
        }
        return false;
    }

    // Поиск файлов по пути или маске
    public static FileList GetFiles(string searchMask, bool searchToAllDirectories = false) {
        return new FileList().AppendFiles(searchMask, searchToAllDirectories);
    }
    public static FileList GetFiles(IList<string> searchMasks, bool searchToAllDirectories = false) {
        return new FileList().AppendFiles(searchMasks, searchToAllDirectories);
    }

    // Поиск файла по пути или маске. Исключение в случае, если файл не найден или найдено несколько файлов
    public static string GetFile(string searchMask, bool searchToAllDirectories = false) {
        FileList files = GetFiles(searchMask, searchToAllDirectories);
        Validate.Throw(files.Count == 0, "По указанной маске '" + searchMask + "' не найдено файлов.");
        Validate.Throw(files.Count > 1, "По указанной маске '" + searchMask + "' найдено несколько файлов.");
        return files[0];
    }

    // Копирование файла (с созданием директорий)
    public static void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) {
        Validate.FileExists(sourceFileName);
        CreateDirectory(destFileName, true);
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    // Перемещение файла (с созданием директорий)
    public static void MoveFile(string sourceFileName, string destFileName, bool overwrite = false) {
        Validate.FileExists(sourceFileName);
        CreateDirectory(destFileName, true);
        if (overwrite) {
            File.Delete(destFileName);
        }
        File.Move(sourceFileName, destFileName);
    }

    // Получение пути с добавлением префикса к имени файла
    public static string AddFileNamePrefix(string path, string prefix) {
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        return Empty(dir) ? prefix + name + ext : dir + "\\" + prefix + name + ext;
    }

    // Получение пути с добавлением суффикса к имени файла
    public static string AddFileNameSuffix(string path, string suffix) {
        string dirPath = Path.GetDirectoryName(path);
        string fileName = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        return Empty(dirPath) ? fileName + suffix + ext : dirPath + "\\" + fileName + suffix + ext;
    }

    // Получение пути с новым именем файла
    public static string NewFileName(string path, string newName, bool includeExtension = false) {
        string dir = Path.GetDirectoryName(path);
        bool emptyDir = Empty(dir);
        if (includeExtension) {
            return emptyDir ? newName : dir + '\\' + newName;
        }
        string ext = Path.GetExtension(path);
        return emptyDir ? newName + ext : dir + '\\' + newName + ext;
    }

    // Получение пути с новым именем директории
    public static string NewDirectoryName(string path, string newDirectory) {
        string name = Path.GetFileName(path);
        return Empty(newDirectory) ? name : Path.Combine(newDirectory, name);
    }

    // Получение пути с новым расширением файла
    public static string NewFileExtension(string path, string newExtension) {
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        return Empty(dir) ? name + newExtension : dir + '\\' + name + newExtension;
    }

    // Сравнение содержимого файлов
    public static bool CompareFiles(string path1, string path2) {
        Validate.FileExists(path1);
        Validate.FileExists(path2);

        long file1Length = new FileInfo(path1).Length;
        long file2Length = new FileInfo(path2).Length;
        if (file1Length != file2Length) {
            return false;
        }

        string file1Hash = ToHexString(__Utils_GetMD5HashFromFile(path1));
        string file2Hash = ToHexString(__Utils_GetMD5HashFromFile(path2));
        return file1Hash == file2Hash;
    }


    // --- РАБОТА СО СТРОКАМИ ---

    // Разбиение строки на 2 части по первому вхождению разделителя
    public static string[] DivideString(string value, string separator) {
        int index = value.IndexOf(separator);
        return index == -1 ?
            (new string[] { value }) :
            (new string[] { value.Remove(index), value.Substring(index + separator.Length) });
    }

    // Получение HEX-строки из массива байт
    public static string ToHexString(byte[] data) {
        StringBuilder hashString = new StringBuilder(data.Length * 2);
        for (int i = 0; i < data.Length; i++) {
            hashString.Append(data[i].ToString("x2"));
        }
        return hashString.ToString();
    }


    // --- СЕРИАЛИЗАЦИЯ ---

    public static T DeserializeFromFile<T>(string filePath) {
        if (Exists(filePath)) {
            string xml = ReadText(filePath);
            return DeserializeFromXml<T>(xml);
        }
        return default(T);
    }

    public static void SerializeToFile(object obj, string filePath) {
        string xml = SerializeToXml(obj);
        if (Exists(filePath)) {
            File.Delete(filePath);
        }
        CreateDirectory(filePath, true);
        File.WriteAllText(filePath, xml, Encoding.UTF8);
    }

    public static string SerializeToXml(object obj) {
        XmlSerializer xmlSerializer = __Utils_GetXmlSerializer(obj.GetType());
        string data;

        using (StringWriter stringWriter = new StringWriter()) {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings {
                OmitXmlDeclaration = true,
                Indent = true
            };
            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings)) {
                xmlSerializer.Serialize(xmlWriter, obj);
            }
            data = stringWriter.ToString();
        }

        // костыль, чтобы сократить размер выходных данных
        string[] splitData1 = DivideString(data, " ");
        string[] splitData2 = DivideString(splitData1[1], ">");
        data = splitData1[0] + ">" + splitData2[1];

        return data;
    }

    public static T DeserializeFromXml<T>(string xml) {
        XmlSerializer xmlSerializer = __Utils_GetXmlSerializer(typeof(T));
        using (StringReader stringReader = new StringReader(xml)) {
            T obj = (T)xmlSerializer.Deserialize(stringReader);

            //  исправление многострочного string после десериализации XML
            foreach (System.Reflection.PropertyInfo property in obj.GetType().GetProperties()) {
                if (property.PropertyType == typeof(string)) {
                    if (!property.CanWrite || property.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0) {
                        continue;
                    }
                    string value = (string)property.GetValue(obj, null);
                    if (value != null) {
                        value = value.Replace("\n", Environment.NewLine);
                        property.SetValue(obj, value, null);
                    }
                }
            }

            return obj;
        }
    }


    // --- ШИФРОВАНИЕ --- <summary>

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
                writer.Write(__Utils_PerformCryptography(data, encryptor));
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
                    decryptData = __Utils_PerformCryptography(decryptData, decryptor);

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

    // --- РАБОТА С СЕТЬЮ

    // Отправка электронного письма с использованием SMTP-сервера Mail.Ru
    public static void SendEmailFromMailRu(MailMessage message, string login, string password) {
        SmtpClient smtp = new SmtpClient("smtp.mail.ru", 25) {
            Credentials = new NetworkCredential(login, password),
            EnableSsl = true
        };
        smtp.Send(message);
    }


    // --- УПРОЩЁННЫЕ КОНСТРУКЦИИ ---

    public static bool Empty(object value) {
        return value == null
|| (value is string ? string.IsNullOrEmpty((string)value) : value is IEnumerable && (value as ICollection).Count == 0);
    }

    public static bool NotEmpty(object value) {
        return !Empty(value);
    }

    public static bool Blank(object value) {
        return value is string
            ? string.IsNullOrWhiteSpace((string)value)
            : value is ICollection ? (value as ICollection).Count == 0 : Empty(value);
    }

    public static bool NotBlank(object value) {
        return !Blank(value);
    }

    public static bool Single(ICollection collection) {
        return collection.Count == 1;
    }

    public static bool Exists(string path) {
        return File.Exists(path) || Directory.Exists(path);
    }

    public static string[] Split(string value, params string[] separators) {
        return value.Split(separators, StringSplitOptions.None);
    }

    public static string[] Split(string value, params char[] separators) {
        return value.Split(separators, StringSplitOptions.None);
    }

    public static string[] CleanSplit(string value, params string[] separators) {
        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] CleanSplit(string value, params char[] separators) {
        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] ReadLines(string searchMask, bool searchToAllDirectories = false) {
        string file = GetFile(searchMask, searchToAllDirectories);
        return File.ReadAllLines(file, Encoding.UTF8);
    }

    public static string ReadText(string searchMask, bool searchToAllDirectories = false) {
        string file = GetFile(searchMask, searchToAllDirectories);
        return File.ReadAllText(file, Encoding.UTF8);
    }


    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

    private static System.Drawing.Color __Utils_ConvertColor(ConsoleColor consoleColor) {
        switch (consoleColor) {
            case ConsoleColor.Black:
                return System.Drawing.Color.Black;
            case ConsoleColor.DarkBlue:
                return System.Drawing.Color.DarkBlue;
            case ConsoleColor.DarkGreen:
                return System.Drawing.Color.DarkGreen;
            case ConsoleColor.DarkCyan:
                return System.Drawing.Color.DarkCyan;
            case ConsoleColor.DarkRed:
                return System.Drawing.Color.DarkRed;
            case ConsoleColor.DarkMagenta:
                return System.Drawing.Color.DarkMagenta;
            case ConsoleColor.DarkYellow:
                return System.Drawing.Color.Orange;
            case ConsoleColor.Gray:
                return System.Drawing.Color.Gray;
            case ConsoleColor.DarkGray:
                return System.Drawing.Color.DarkGray;
            case ConsoleColor.Blue:
                return System.Drawing.Color.Blue;
            case ConsoleColor.Green:
                return System.Drawing.Color.Green;
            case ConsoleColor.Cyan:
                return System.Drawing.Color.Cyan;
            case ConsoleColor.Red:
                return System.Drawing.Color.Red;
            case ConsoleColor.Magenta:
                return System.Drawing.Color.Magenta;
            case ConsoleColor.Yellow:
                return System.Drawing.Color.Yellow;
            case ConsoleColor.White:
                return System.Drawing.Color.White;
            default:
                throw new NotImplementedException();
        }
    }

    private static byte[] __Utils_GetMD5HashFromFile(string filePath) {
        using (MD5 md5 = MD5.Create()) {
            using (FileStream stream = File.OpenRead(filePath)) {
                return md5.ComputeHash(stream);
            }
        }
    }

    private static void __Utils_BeginReadFromStream(Stream stream, Action<byte[]> onRead) {
        byte[] buffer = new byte[102400];
        AsyncCallback callback = null;
        callback = (asyncResult) => {
            int readed = stream.EndRead(asyncResult);
            if (readed > 0) {
                byte[] data = new byte[readed];
                Array.Copy(buffer, data, readed);
                onRead(data);
                stream.BeginRead(buffer, 0, buffer.Length, callback, null);
            }
        };
        stream.BeginRead(buffer, 0, buffer.Length, callback, null);
    }

    private static byte[] __Utils_PerformCryptography(byte[] data, ICryptoTransform cryptoTransform) {
        using (MemoryStream ms = new MemoryStream())
        using (CryptoStream cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write)) {
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return ms.ToArray();
        }
    }

    private static XmlSerializer __Utils_GetXmlSerializer(Type type) {
        if (!__Utils_XmlSerializers.ContainsKey(type)) {
            XmlSerializer xmlSerializer = new XmlSerializer(type);
            __Utils_XmlSerializers.Add(type, xmlSerializer);
        }
        return __Utils_XmlSerializers[type];
    }
    private static readonly Dictionary<Type, XmlSerializer> __Utils_XmlSerializers = new Dictionary<Type, XmlSerializer>();

    public static void __Utils_Init(IScriptContext context) {
        __utils_Context = context;
    }
    private static IScriptContext __utils_Context;
} //##

//## #namespace

// Работа со списком файлов
public class FileList : List<string>
{
    public FileList() : base() { }

    public FileList(IEnumerable<string> collection) : base(collection) { }


    // Поиск файлов по пути или маске и добавление их в список
    public FileList AppendFiles(string searchMask, bool searchToAllDirectories = false) {
        SearchOption searchOption = searchToAllDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        if (File.Exists(searchMask)) {
            Add(searchMask);

        }
        else if (Directory.Exists(searchMask)) {
            AddRange(Directory.GetFiles(searchMask, "*", searchOption));

        }
        else {
            string directoryPath = Path.GetDirectoryName(searchMask);
            if (string.IsNullOrEmpty(directoryPath)) {
                directoryPath = Environment.CurrentDirectory;
            }
            string fileMask = Path.GetFileName(searchMask);
            AddRange(Directory.GetFiles(directoryPath, fileMask, searchOption));
        }
        return this;
    }

    // Поиск файлов по пути или маске и добавление их в список
    public FileList AppendFiles(IList<string> searchMasks, bool searchToAllDirectories = false) {
        foreach (string searchMask in searchMasks) {
            AppendFiles(searchMask, searchToAllDirectories);
        }
        return this;
    }

    // Удаление файлов
    public FileList Delete(int startIndex = 0) {
        for (int i = startIndex; i < Count; i++) {
            string file = this[i];
            File.Delete(file);
            RemoveAt(i--);
        }
        return this;
    }

    // Удаление файла по индексу
    public FileList DeleteAt(int index) {
        File.Delete(this[index]);
        RemoveAt(index);
        return this;
    }

    // Удаление файлов. Если файл удалить невозможно, исключение не создаётся
    public FileList TryDelete(int startIndex = 0) {
        for (int i = startIndex; i < Count; i++) {
            string file = this[i];
            try {
                File.Delete(file);
                RemoveAt(i--);
            }
            catch {
            }
        }
        return this;
    }

    // Удаление файла по индексу. Если файл удалить невозможно, исключение не создаётся
    public FileList TryDeleteAt(int index) {
        try {
            DeleteAt(index);
        }
        catch {
        }
        return this;
    }

    // Сортировка файлов по полным путям
    public FileList SortFiles(bool desc = false) {
        if (desc) {
            Sort((a, b) => string.Compare(a, b, true));
        }
        else {
            Sort((a, b) => string.Compare(b, a, true));
        }
        return this;
    }

    // Сортировка файлов по дате изменения (по умолчанию старые файлы в начале массива)
    public FileList SortFilesByWriteTime(bool desc = false) {
        Dictionary<string, DateTime> fileDates = new Dictionary<string, DateTime>();
        foreach (string file in this) {
            fileDates.Add(file, new FileInfo(file).LastWriteTime);
        }
        if (desc) {
            Sort((a, b) => fileDates[b].CompareTo(fileDates[a]));
        }
        else {
            Sort((a, b) => fileDates[a].CompareTo(fileDates[b]));
        }
        return this;
    }

    public FileList Filter(Func<string, bool> filter) {
        for (int i = 0; i < Count; i++) {
            if (!filter(this[i])) {
                RemoveAt(i);
                i--;
            }
        }
        return this;
    }

    public FileList FilterByExtension(string extension) {
        extension = extension.ToLower();
        return Filter(x => Path.GetExtension(x).ToLower() == extension);
    }

    public static implicit operator string[](FileList fileList) {
        return fileList.ToArray();
    }
}

// Валидация данных
public static class Validate
{
    // В случае, если условие выполнено, вызывается Exception
    public static void Throw(bool condition, string message = null) {
        if (condition) {
            throw message == null ? new ValidationException() : new ValidationException(message);
        }
    }

    // Проверка на выполнение условия, иначе Exception
    public static void True(bool condition, string message = null) {
        Throw(!condition, message);
    }

    // Проверка на не-null переменную, иначе Exception
    public static void NotNull(object obj, string message = null) {
        Throw(obj == null, message);
    }

    // Проверка на заполненность, иначе Exception
    public static void NotEmpty(string value, string message = null) {
        Throw(string.IsNullOrEmpty(value), message);
    }
    public static void NotEmpty(ICollection collection, string message = null) {
        Throw(collection == null || collection.Count == 0, message);
    }

    // Проверка на заполненность строки символами, отличными от пробела, иначе Exception
    public static void NotBlank(string value, string message = null) {
        Throw(string.IsNullOrWhiteSpace(value), message);
    }

    // Проверка процесса на корректное завершение, иначе Exception
    public static void Process(Process process, int expectedExitCode = 0) {
        process.WaitForExit();
        if (process.HasExited && process.ExitCode != expectedExitCode) {
            throw new Exception(string.Format("Ошибка выполнения процесса (код возврата: {0}): \"{1}\" {2}",
                process.ExitCode,
                process.StartInfo.FileName,
                process.StartInfo.Arguments));
        }
    }

    // Вывод исключения в случае, если файла не существует
    public static void FileExists(string filePath, bool checkRelativePath = true) {
        if (!File.Exists(filePath)) {
            // игнорирование коротких путей файлов используется в случае,
            // если операционной системе известен путь к файлу, в отличие от программы
            // (например программы из папки WINDOWS\system32)
            if (Path.IsPathRooted(filePath) || (checkRelativePath && !Path.IsPathRooted(filePath))) {
                throw new FileNotFoundException("Не удаётся найти '" + filePath + "'.", filePath);
            }
        }
    }
}

// Оболочка процесса с расширенными возможностями
public class ScriptProcess : Process
{
    public ScriptProcess() : base() {
    }

    public ScriptProcess(string fileName) : base() {
        StartInfo.FileName = fileName;
    }

    public ScriptProcess(string fileName, string arguments) : this(fileName) {
        if (arguments != null) {
            StartInfo.Arguments = arguments;
        }
    }


    // Процесс должен быть запущен в скрытом режиме
    public ScriptProcess Hidden() {
        StartInfo.CreateNoWindow = true;
        StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        return this;
    }

    // Процесс должен быть запущен с перенаправлением вывода
    public ScriptProcess RedirectOutput() {
        StartInfo.UseShellExecute = false; // перенаправление ввода/вывода невозможно без выключенной опции 'UseShellExecute'
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        return this;
    }

    // Процесс должен быть запущен с перенаправлением ввода
    public ScriptProcess RedirectInput() {
        StartInfo.UseShellExecute = false; // перенаправление ввода/вывода невозможно без выключенной опции 'UseShellExecute'
        StartInfo.RedirectStandardInput = true;
        return this;
    }

    // Запуск процесса (вместо стандартного Start)
    public ScriptProcess StartAndReturn() {
        if (!StartInfo.UseShellExecute) {
            if (StartInfo.FileName.ToLowerInvariant().EndsWith(".py")) {
                // System.ComponentModel.Win32Exception: "Указанный исполняемый файл не является действительным приложением для этой операционной системы."
                StartInfo.Arguments = string.Format("\"{0}\" ", StartInfo.FileName) + StartInfo.Arguments;
                StartInfo.FileName = "python";
            }
        }
        Start();
        return this;
    }

    // Запуск процесса и ожидание его завершения
    public ScriptProcess StartAndWaitForExit() {
        StartAndReturn();
        WaitForExit();
        return this;
    }

    // Возвращает выходной поток
    public StreamReader GetOutput(Encoding encoding) {
        return encoding == null ? StandardOutput : new StreamReader(StandardOutput.BaseStream, encoding);
    }
    public StreamReader GetOutput(int encodingCodepage) {
        return GetOutput(Encoding.GetEncoding(encodingCodepage));
    }
    public StreamReader GetOutput(string encodingName) {
        return GetOutput(Encoding.GetEncoding(encodingName));
    }
    public StreamReader GetOutput() {
        return StandardOutput;
    }

    // Возвращает выходной поток ошибок
    public StreamReader GetError(Encoding encoding) {
        return encoding == null ? StandardError : new StreamReader(StandardError.BaseStream, encoding);
    }
    public StreamReader GetError(int encodingCodepage) {
        return GetError(Encoding.GetEncoding(encodingCodepage));
    }
    public StreamReader GetError(string encodingName) {
        return GetError(Encoding.GetEncoding(encodingName));
    }
    public StreamReader GetError() {
        return StandardError;
    }
}


public abstract class CommandArgument : ICloneable
{
    public object Clone() {
        return MemberwiseClone();
    }

    public override string ToString() {
        return ToString(this);
    }

    public static implicit operator string(CommandArgument args) {
        return args.ToString();
    }

    public static string ToString(CommandArgument args) {
        StringBuilder str = new StringBuilder();

        Type objType = args.GetType();
        PropertyInfo[] properties = objType.GetProperties();
        foreach (PropertyInfo property in properties) {
            object[] attrArray = property.GetCustomAttributes(typeof(CommandOptionAttribute), false);
            if (attrArray.Length == 0) {
                continue;
            }
            CommandOptionAttribute attr = (CommandOptionAttribute)attrArray[0];

            object value = property.GetValue(args, null);
            if (value == null) {
                if (attr.DefaultValue != null) {
                    value = attr.DefaultValue;
                }
                else {
                    if (attr.Required) {
                        throw new ArgumentException("Не указано значение для обязательного параметра \"" + property.Name + "\".");
                    }
                    else {
                        continue;
                    }
                }
            }

            if (attr.Flag) {
                if (Equals(value, true)) {
                    if (str.Length > 0) {
                        str.Append(attr.OptionSeparator);
                    }
                    str.Append(attr.Options[0]);
                }
            }
            else {
                if (value is ICollection) {
                    ICollection collection = (ICollection)value;
                    if (collection.Count > 0) {

                        if (str.Length > 0) {
                            str.Append(attr.OptionSeparator);
                        }
                        if (!attr.RepeatOption) {
                            if (attr.Options.Length > 0) {
                                str.Append(attr.Options[0]);
                            }
                        }

                        int collectionOptionsCount = 0;
                        foreach (object item in collection) {

                            if (attr.RepeatOption) {
                                if (str.Length > 0) {
                                    str.Append(' ');
                                }
                                if (attr.Options.Length > 0) {
                                    str.Append(attr.Options[0]);
                                }
                            }

                            if (collectionOptionsCount == 0) {
                                if (str.Length > 0) {
                                    str.Append(attr.OptionSeparator);
                                }
                            }
                            else {
                                str.Append(attr.ValueSeparator);
                            }

                            if (attr.Quoted) {
                                str.Append('\"');
                            }
                            str.Append(GetArgumentValue(item, attr));
                            if (attr.Quoted) {
                                str.Append('\"');
                            }

                            collectionOptionsCount++;
                        }
                    }
                }
                else {

                    if (str.Length > 0) {
                        str.Append(attr.OptionSeparator);
                    }

                    if (attr.Options.Length > 0) {
                        str.Append(attr.Options[0]);
                    }

                    str.Append(attr.ValueSeparator);

                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                    str.Append(GetArgumentValue(value, attr));
                    if (attr.Quoted) {
                        str.Append('\"');
                    }
                }
            }
        }
        return str.ToString();
    }

    private static string GetArgumentValue(object value, CommandOptionAttribute attr) {
        string convertedValue;
        if (attr.Converter != null) {
            IConsoleArgumentConverter converter = (IConsoleArgumentConverter)Activator.CreateInstance(attr.Converter);
            convertedValue = converter.Convert(value);
        }
        else {
            convertedValue = value is bool ? Equals(value, true) ? "true" : "false" : value.ToString();
        }
        return convertedValue;
    }
}

public abstract class CommandArgument<T> : CommandArgument
{
    public new T Clone() {
        return (T)base.Clone();
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class CommandOptionAttribute : Attribute
{
    /// <summary>
    /// Указывает, является ли параметр флагом (без указания значения)
    /// </summary>
    public bool Flag { get; set; }

    /// <summary>
    /// Будет ли значение параметра указано в кавычках
    /// </summary>
    public bool Quoted { get; set; }

    /// <summary>
    /// Разделитель между названием параметра и значением.
    /// </summary>
    public string ValueSeparator { get; set; }

    public string OptionSeparator { get; set; }

    /// <summary>
    /// Конвертер значений типа <see cref="IConsoleArgumentConverter"/>.
    /// </summary>
    public Type Converter { get; set; }

    /// <summary>
    /// Указывает, является ли параметр обязательным.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Указывается значение по умолчанию в случае, если поле не заполнено.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// Если в качестве значения указано перечисление, название опции добавляется 1 раз в начале перечисления
    /// </summary>
    public bool RepeatOption { get; set; }

    /// <summary>
    /// Названия аргумента.
    /// </summary>
    public string[] Options { get; private set; }


    public CommandOptionAttribute(params string[] options) {
        Options = options;
        Quoted = true;
        ValueSeparator = " ";
        OptionSeparator = " ";
    }
}

public interface IConsoleArgumentConverter
{
    string Convert(object value);
}

/// <summary>
/// Конструктор строки аргументов.
/// </summary>
public class CommandArgumentBuilder
{
    public CommandArgumentBuilder() {

    }

    public CommandArgumentBuilder(string value) : this() {
        Add(value);
    }

    public CommandArgumentBuilder Add(string format, params object[] args) {
        string value = string.Format(format, args);
        value = value.Replace(Environment.NewLine, " ");
        if (builder.Length > 0 && builder[builder.Length - 1] != ' ') {
            builder.Append(' ');
        }
        builder.Append(value);
        return this;
    }

    public CommandArgumentBuilder Add(bool condition, string format, params object[] args) {
        if (condition) {
            Add(format, args);
        }
        return this;
    }

    public CommandArgumentBuilder Add(IEnumerable<string> values) {
        foreach (string value in values) {
            Add(value);
        }
        return this;
    }

    public CommandArgumentBuilder Add(bool condition, IEnumerable<string> values) {
        if (condition) {
            Add(values);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(string value) {
        if (value.StartsWith("\"") && value.EndsWith("\"")) {
            Add(value);
        }
        else {
            Add("\"{0}\"", value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(bool condition, string value) {
        if (condition) {
            AddQuote(value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(IEnumerable<string> values) {
        foreach (string value in values) {
            AddQuote(value);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote(bool condition, IEnumerable<string> values) {
        if (condition) {
            AddQuote(values);
        }
        return this;
    }

    public CommandArgumentBuilder AddQuote() {
        Add("\"");
        return this;
    }

    public CommandArgumentBuilder Clone() {
        return new CommandArgumentBuilder(ToString());
    }


    public override string ToString() {
        return builder.ToString();
    }

    public static implicit operator string(CommandArgumentBuilder builder) {
        return builder.ToString();
    }

    private readonly StringBuilder builder = new StringBuilder();
}

public class ScriptWebClient : WebClient
{
    public CookieContainer CookieContainer { get; set; }

    public ScriptWebClient() {
        Encoding = Encoding.UTF8;
        Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
    }

    protected override WebRequest GetWebRequest(Uri address) {
        HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        request.CookieContainer = CookieContainer;
        return request;
    }
}
