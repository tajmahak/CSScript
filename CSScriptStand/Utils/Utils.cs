﻿using CSScript.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

/// СКРИПТОВЫЕ УТИЛИТЫ (15.05.2021)

///// #using CSScript.Core;
///// #using System;
///// #using System.Collections.Generic;
///// #using System.Diagnostics;
///// #using System.Drawing;
///// #using System.IO;
///// #using System.Net;
///// #using System.Net.Mail;
///// #using System.Security.Cryptography;
///// #using System.Text;
///// 
///// #init
/////
///// context = Context;
/////
///// #class

public static class __Utils /////
{ /////

    /// --- РАБОТА С КОНТЕКСТОМ ---

    // Получение входящего аргумента по индексу
    public static T GetArgument<T>(int index, T defaultValue) {
        if (context.Args.Length > index) {
            string value = context.Args[index];
            if (!string.IsNullOrEmpty(value)) {
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        return defaultValue;
    }

    // Вывод текста в стандартный выходной поток
    public static void Write(object value, ConsoleColor? color = null) {
        context.Write(value, color);
    }

    // Вывод текста в стандартный выходной поток из другого потока
    public static void Write(StreamReader reader, ConsoleColor? color = null) {
        char[] buffer = new char[1024];
        while (!reader.EndOfStream) {
            int readed = reader.Read(buffer, 0, buffer.Length);
            string value = new string(buffer, 0, readed);
            Write(value, color);
        }
    }

    // Вывод текста с переносом строки в стандартный выходной поток
    public static void WriteLine(object value, ConsoleColor? color = null) {
        context.WriteLine(value, color);
    }

    // Вывод переноса строки в стандартный выходной поток
    public static void WriteLine() {
        context.WriteLine();
    }

    // Вывод текста в стандартный поток ошибок
    public static void WriteError(object value) {
        context.WriteError(value);
    }

    // Вывод текста в стандартный поток ошибок из другого потока 
    public static void WriteError(StreamReader reader) {
        char[] buffer = new char[1024];
        while (!reader.EndOfStream) {
            int readed = reader.Read(buffer, 0, buffer.Length);
            string value = new string(buffer, 0, readed);
            WriteError(value);
        }
    }

    // Вывод текста с переносом строки в стандартный поток ошибок
    public static void WriteErrorLine(object value) {
        context.WriteErrorLine(value);
    }

    // Вывод переноса строки в стандартный поток ошибок
    public static void WriteErrorLine() {
        context.WriteErrorLine();
    }

    // Вывод штампа даты и времени в стандартный выходной поток
    public static void WriteTimeStamp() {
        Write("[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "]: ", colors.Info);
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
                ValidateIsTrue(fragmentBlock.Length == 2, "Не удалось распознать строку форматирования '" + fragment + "'.");
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
        return context.ReadLine(color);
    }

    // Получение текстового лога
    public static string GetLog() {
        StringBuilder log = new StringBuilder();
        foreach (CSScript.Core.LogFragment logFragment in context.OutLog) {
            log.Append(logFragment.Text);
        }
        return log.ToString();
    }

    // Получение лога в формате HTML (для отправки по E-Mail)
    public static string GetHtmlLog() {
        StringBuilder log = new StringBuilder();
        //log.Append("<div style=\"background-color: "+ ColorTranslator.ToHtml(__GetColor(Colors.Background)) + ";\">");
        log.Append("<pre>");
        foreach (CSScript.Core.LogFragment logFragment in context.OutLog) {
            if (logFragment.Color != colors.Foreground) {
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



    /// --- РАБОТА С ПРОЦЕССАМИ ---

    // Создание неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
    public static ScriptProcess CreateProcess(string fileName, string args = null) {
        CheckFileExists(fileName, false);
        return new ScriptProcess(fileName, args);
    }

    // Создание контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится)
    public static ScriptProcess CreateManagedProcess(string fileName, string args = null) {
        ScriptProcess process = CreateProcess(fileName, args);
        context.RegisterProcess(process);
        return process;
    }



    /// --- РАБОТА С ФАЙЛАМИ / ПАПКАМИ ---

    // Получение общего списка файлов. В качестве параметров могут быть пути к файлам и папкам (например из аргументов программы)
    public static string[] GetFiles(params string[] fsObjects) {
        List<string> files = new List<string>();
        for (int i = 0; i < fsObjects.Length; i++) {
            string fsObject = fsObjects[i];
            fsObject = fsObject.Trim('\"');
            if (!string.IsNullOrEmpty(fsObject)) {
                if (File.Exists(fsObject)) {
                    files.Add(fsObject);
                } else if (Directory.Exists(fsObject)) {
                    files.AddRange(Directory.GetFiles(fsObject, "*", SearchOption.AllDirectories));
                } else {
                    throw new Exception("Неизвестный файл/папка: '" + fsObject + "'");
                }
            }
        }
        return files.ToArray();
    }

    // Создание папки
    public static bool CreateDirectory(string path, bool isFilePath = false) {
        string dirPath = isFilePath ? Path.GetDirectoryName(path) : path;
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
            return true;
        }
        return false;
    }

    // Поиск файла по пути или маске
    public static string FindFile(string path) {
        if (File.Exists(path)) {
            return path;
        }
        string[] files = FindFiles(path);
        ValidateCondition(files.Length == 0, "По указанной маске '" + path + "' не найдено файлов.");
        ValidateCondition(files.Length > 1, "По указанной маске '" + path + "' найдено несколько файлов.");
        return files[0];
    }

    // Поиск файлов по пути или маске
    public static string[] FindFiles(string path) {
        string dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) {
            dir = Environment.CurrentDirectory;
        }
        string mask = Path.GetFileName(path);
        var files = Directory.GetFiles(dir, mask);
        return files;
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
    public static int DeleteOldFiles(string path, int remainCount, string searchPattern = null) {
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
                WriteLine("Не удалось удалить файл '" + fileName + "'", colors.Error);
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
    public static void CheckFileExists(string filePath, bool checkRelativePath = true) {
        if (!File.Exists(filePath)) {
            // игнорирование коротких путей файлов используется в случае,
            // если операционной системе известен путь к файлу, в отличие от программы
            // (например программы из папки WINDOWS\system32)
            if (Path.IsPathRooted(filePath) || (checkRelativePath && !Path.IsPathRooted(filePath))) {
                throw new Exception("Не удаётся найти '" + filePath + "'.");
            }
        }
    }

    // Вычисление MD5 хэша для файла
    public static string GetMD5Hash(string filePath) {
        CheckFileExists(filePath);
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
        CheckFileExists(file1Path);
        CheckFileExists(file2Path);

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



    /// --- ВАЛИДАЦИЯ ---

    // В случае, если условие выполнено, вызывается Exception
    public static void ValidateCondition(bool condition, string message = null) {
        if (condition) {
            throw message == null ? new Exception("Ошибка валидации.") : new Exception(message);
        }
    }

    // Проверка на выполнение условия, иначе Exception
    public static void ValidateIsTrue(bool condition, string message = null) {
        ValidateCondition(!condition, message);
    }

    // Проверка на не-null переменную, иначе Exception
    public static void ValidateIsNotNull(object obj, string message = null) {
        ValidateCondition(obj == null, message);
    }

    // Проверка на непустую строку, иначе Exception
    public static void ValidateIsNotBlank(string value, string message = null) {
        ValidateCondition(string.IsNullOrEmpty(value), message);
    }

    // Проверка процесса на корректное завершение, иначе Exception
    public static void ValidateProcess(Process process, int expectedExitCode = 0) {
        process.WaitForExit();
        if (process.HasExited && process.ExitCode != expectedExitCode) {
            throw new Exception(string.Format("Ошибка выполнения процесса (код возврата: {0}): \"{1}\" {2}",
                process.ExitCode,
                process.StartInfo.FileName,
                process.StartInfo.Arguments));
        }
    }



    /// --- ПРОЧЕЕ ---

    // Разбиение строки на 2 части.
    public static string[] DivideString(string value, string separator) {
        int index = value.IndexOf(separator);
        return index == -1 ?
            (new string[] { value }) :
            (new string[] { value.Remove(index), value.Substring(index + separator.Length) });
    }

    // Отправка электронного письма с использованием SMTP-сервера Mail.Ru
    public static bool SendEmailFromMailRu(MailMessage message, string login, string password) {
        try {
            SmtpClient smtp = new SmtpClient("smtp.mail.ru", 25) {
                Credentials = new NetworkCredential(login, password),
                EnableSsl = true
            };
            smtp.Send(message);
            return true;
        } catch (Exception ex) {
            WriteLine("Не удалось отправить сообщение: " + ex.Message, colors.Error);
            return false;
        }
    }



    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

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


    public static IScriptContext context { get; set; }
    private static ColorScheme colors { get { return context.ColorScheme; } }
} /////

///// #namespace

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


    public ScriptProcess Hidden() {
        StartInfo.CreateNoWindow = true;
        StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        return this;
    }

    public ScriptProcess RedirectOutput() {
        StartInfo.UseShellExecute = false; // перенаправление ввода/вывода невозможно без выключенной опции 'UseShellExecute'
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        return this;
    }

    public ScriptProcess RedirectInput() {
        StartInfo.UseShellExecute = false; // перенаправление ввода/вывода невозможно без выключенной опции 'UseShellExecute'
        StartInfo.RedirectStandardInput = true;
        return this;
    }

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

    public ScriptProcess StartAndWaitForExit() {
        StartAndReturn();
        WaitForExit();
        return this;
    }


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


    public bool Success { get { return HasExited && ExitCode == 0; } }
}