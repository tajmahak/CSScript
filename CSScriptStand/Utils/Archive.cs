﻿using System.Text;

/// АРХИВАЦИЯ (15.05.2021)

///// #using System.Text;
///// 
///// #namespace

// Аргументы для командной строки 7-Zip
public class SevenZipArgs
{
    public string Input; // Путь к каталогам и файлам для упаковки в архив
    public string Output; // Путь к файлу архива
    public int CompressionLevel = 9; // Уровень компрессии. 0 - без компрессии (быстро), 9 - самая большая компрессия (медленно)
    public string Password; // Пароль для архива
    public bool EncryptFileStructure = true; // Шифровать имена файлов (в случае установки пароля на архив)
    public bool IncludeOpenFiles = true; // Включить файл в архив, даже если он в данный момент используется. Для резервного копирования очень полезный ключ
    public string PartSize = null; // Разбиение архива на указанный размер [b|k|m|g]
    public bool ZipFormat = false; // Создание архива в формате ZIP

    public SevenZipArgs() {
    }
    public SevenZipArgs(string input, string output) : this() {
        Input = input;
        Output = output;
    }

    public override string ToString() {
        StringBuilder args = new StringBuilder();
        args.AppendFormat(" a"); // Добавление файлов в архив. Если архивного файла не существует, создает его
        args.AppendFormat(" -y"); // Утвердительно ответить на все вопросы, которые может запросить система.
        args.AppendFormat(" -mx{0}", CompressionLevel);
        if (ZipFormat) {
            args.AppendFormat(" -tzip");
        }
        if (IncludeOpenFiles) {
            args.AppendFormat(" -ssw");
        }
        if (Password != null) {
            args.AppendFormat(" -p{0}", Password);
        }
        if (EncryptFileStructure) {
            args.AppendFormat(" -mhe");
        }
        if (PartSize != null) {
            args.AppendFormat(" -v{0}", PartSize);
        }
        args.AppendFormat(" \"{0}\"", Output);
        args.AppendFormat(" \"{0}\"", Input);
        return args.Remove(0, 1).ToString();
    }
}
