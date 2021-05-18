using CSScript.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// utils
// СКРИПТОВЫЕ УТИЛИТЫ (17.05.2021)
// ------------------------------------------------------------

///// #init
///// __Utils_Init(Context);

///// #class

// Утилиты общей направленности
public static class __Utils /////
{ /////

    /// --- РАБОТА С КОНТЕКСТОМ ---

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

    // Вывод штампа даты и времени в стандартный выходной поток
    public static void WriteTimeStamp() {
        Write("[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "]: ", __utils_Context.ColorScheme.Info);
    }

    // Вывод штампа даты и времени в стандартный поток ошибок
    public static void WriteErrorTimeStamp() {
        WriteError("[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "]: ");
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
                Validate.IsTrue(fragmentBlock.Length == 2, "Не удалось распознать строку форматирования '" + fragment + "'.");
                fragment = fragmentBlock[1];
                switch (fragmentBlock[0].ToLowerInvariant()) {
                    case "black": writeColor = ConsoleColor.Black; break;
                    case "darkblue": writeColor = ConsoleColor.DarkBlue; break;
                    case "darkgreen": writeColor = ConsoleColor.DarkGreen; break;
                    case "darkcyan": writeColor = ConsoleColor.DarkCyan; break;
                    case "darkred": writeColor = ConsoleColor.DarkRed; break;
                    case "darkmagenta": writeColor = ConsoleColor.DarkMagenta; break;
                    case "darkyellow": writeColor = ConsoleColor.DarkYellow; break;
                    case "gray": writeColor = ConsoleColor.Gray; break;
                    case "darkgray": writeColor = ConsoleColor.DarkGray; break;
                    case "blue": writeColor = ConsoleColor.Blue; break;
                    case "green": writeColor = ConsoleColor.Green; break;
                    case "cyan": writeColor = ConsoleColor.Cyan; break;
                    case "red": writeColor = ConsoleColor.Red; break;
                    case "magenta": writeColor = ConsoleColor.Magenta; break;
                    case "yellow": writeColor = ConsoleColor.Yellow; break;
                    case "white": writeColor = ConsoleColor.White; break;
                    default: throw new Exception("Не удалось распознать идентификатор цвета '" + fragmentBlock[0] + "'.");
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
        foreach (LogFragment logFragment in __utils_Context.OutLog) {
            log.Append(logFragment.Text);
        }
        return log.ToString();
    }

    // Получение лога в формате HTML (для отправки по E-Mail)
    public static string GetHtmlLog() {
        StringBuilder log = new StringBuilder();
        //log.Append("<div style=\"background-color: "+ ColorTranslator.ToHtml(__GetColor(Colors.Background)) + ";\">");
        log.Append("<pre>");
        foreach (LogFragment logFragment in __utils_Context.OutLog) {
            if (logFragment.Color != __utils_Context.ColorScheme.Foreground) {
                log.Append("<font color=\"" + ColorTranslator.ToHtml(__Utils_ConvertColor(logFragment.Color)) + "\">" + logFragment.Text + "</font>");
            } else {
                log.Append(logFragment.Text);
            }
        }
        log.Replace(Environment.NewLine, "<br>");
        log.Append("</pre>");
        //log.Append("</div>");

        return log.ToString();
    }


    /// --- РАБОТА С ПРОЦЕССАМИ ---

    // Создание неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
    public static ScriptProcess CreateProcess(string fileName, object args = null) {
        CheckFileExists(fileName, false);
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
        process.RedirectOutput();
        process.StartAndReturn();
        if (outputEncoding == null) {
            Write(process.GetOutput());
            WriteError(process.GetError());
        } else {
            Write(process.GetOutput(outputEncoding), __utils_Context.ColorScheme.Info);
            WriteError(process.GetError(outputEncoding));
        }
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

    // Поиск файлов по пути или маске
    public static FileList GetFiles(string searchMask, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        FileList fileList = new FileList();
        if (File.Exists(searchMask)) {
            fileList.Add(searchMask);

        } else if (Directory.Exists(searchMask)) {
            fileList.AddRange(Directory.GetFiles(searchMask, "*", searchOption));

        } else {
            string directoryPath = Path.GetDirectoryName(searchMask);
            if (string.IsNullOrEmpty(directoryPath)) {
                directoryPath = Environment.CurrentDirectory;
            }
            string fileMask = Path.GetFileName(searchMask);
            fileList.AddRange(Directory.GetFiles(directoryPath, fileMask, searchOption));
        }
        return fileList;
    }
    public static FileList GetFiles(IList<string> searchMasks, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        FileList fileList = new FileList();
        foreach (string searchMask in searchMasks) {
            fileList.AddRange(GetFiles(searchMask, searchOption));
        }
        return fileList;
    }

    // Поиск файла по пути или маске
    public static string GetFile(string searchMask, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        FileList files = GetFiles(searchMask, searchOption);
        Validate.ThrowIf(files.Count == 0, "По указанной маске '" + searchMask + "' не найдено файлов.");
        Validate.ThrowIf(files.Count > 1, "По указанной маске '" + searchMask + "' найдено несколько файлов.");
        return files[0];
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
    public static string NewFileName(string path, string newName, bool includeExtension = false) {
        string dirPath = Path.GetDirectoryName(path);
        if (includeExtension) {
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + newName + ext;
        } else {
            return dirPath + "\\" + newName;
        }
    }

    // Вывод исключения в случае, если файла не существует
    public static void CheckFileExists(string filePath, bool checkRelativePath = true) {
        if (!File.Exists(filePath)) {
            // игнорирование коротких путей файлов используется в случае,
            // если операционной системе известен путь к файлу, в отличие от программы
            // (например программы из папки WINDOWS\system32)
            if (Path.IsPathRooted(filePath) || (checkRelativePath && !Path.IsPathRooted(filePath))) {
                throw new FileNotFoundException("Не удаётся найти '" + filePath + "'.", filePath);
            }
        }
    }

    // Сравнение содержимого файлов
    public static bool CompareFiles(string file1Path, string file2Path) {
        CheckFileExists(file1Path);
        CheckFileExists(file2Path);

        long file1Length = new FileInfo(file1Path).Length;
        long file2Length = new FileInfo(file2Path).Length;
        if (file1Length != file2Length) {
            return false;
        }

        string file1Hash = ToHexString(__Utils_GetMD5HashFromFile(file1Path));
        string file2Hash = ToHexString(__Utils_GetMD5HashFromFile(file2Path));
        if (file1Hash != file2Hash) {
            return false;
        }

        return true;
    }


    /// --- РАБОТА СО СТРОКАМИ ---

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


    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

    private static Color __Utils_ConvertColor(ConsoleColor consoleColor) {
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

    private static byte[] __Utils_GetMD5HashFromFile(string filePath) {
        using (MD5 md5 = MD5.Create()) {
            using (FileStream stream = File.OpenRead(filePath)) {
                return md5.ComputeHash(stream);
            }
        }
    }

    public static void __Utils_Init(IScriptContext context) {
        __utils_Context = context;
    }
    private static IScriptContext __utils_Context;
} /////

///// #namespace

// Работа со списком файлов
public class FileList : List<string>
{
    public FileList() { }

    public FileList(IEnumerable<string> collection) : base(collection) { }

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
            } catch {
            }
        }
        return this;
    }

    // Удаление файла по индексу. Если файл удалить невозможно, исключение не создаётся
    public FileList TryDeleteAt(int index) {
        try {
            DeleteAt(index);
        } catch {
        }
        return this;
    }

    // Сортировка файлов по полным путям
    public FileList SortFiles(bool desc = false) {
        if (desc) {
            Sort((a, b) => string.Compare(a, b, true));
        } else {
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
        } else {
            Sort((a, b) => fileDates[a].CompareTo(fileDates[b]));
        }
        return this;
    }
}

// Валидация данных
public static class Validate
{
    // В случае, если условие выполнено, вызывается Exception
    public static void ThrowIf(bool condition, string message = null) {
        if (condition) {
            throw message == null ? new ValidationException() : new ValidationException(message);
        }
    }

    // Проверка на выполнение условия, иначе Exception
    public static void IsTrue(bool condition, string message = null) {
        ThrowIf(!condition, message);
    }

    // Проверка на не-null переменную, иначе Exception
    public static void IsNotNull(object obj, string message = null) {
        ThrowIf(obj == null, message);
    }

    // Проверка на заполненность, иначе Exception
    public static void IsNotEmpty(string value, string message = null) {
        ThrowIf(string.IsNullOrEmpty(value), message);
    }
    public static void IsNotEmpty(ICollection collection, string message = null) {
        ThrowIf(collection == null || collection.Count == 0, message);
    }

    // Проверка на заполненность строки символами, отличными от пробела, иначе Exception
    public static void IsNotBlank(string value, string message = null) {
        ThrowIf(string.IsNullOrWhiteSpace(value), message);
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
        return new StreamReader(StandardOutput.BaseStream, encoding);
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
        return new StreamReader(StandardError.BaseStream, encoding);
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