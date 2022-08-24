using System.Collections.Generic;
using System.Text;

// utils.args.archive
// АРХИВАЦИЯ
// ------------------------------------------------------------

//## #import utils
//## #namespace

/// <summary>
/// Параметры для 7-Zip.
/// </summary>
public class SevenZipCommandArgs : CommandArgument<SevenZipCommandArgs>
{
    /// <summary>
    /// a - Add;
    /// b - Benchmark;
    /// d - Delete;
    /// e - Extract;
    /// h - Hash;
    /// i - Show information about supported formats;
    /// l - List;
    /// rn - Rename;
    /// t - Test;
    /// u - Update;
    /// x - eXtract with full paths;
    /// </summary>
    [CommandOption(Quoted = false, Required = true)]
    public string Command { get; set; }

    /// <summary>
    /// Disables most of the normal user queries during 7-Zip execution.
    /// </summary>
    [CommandOption("-y", Flag = true)]
    public bool AssumeYes { get; set; }

    /// <summary>
    /// Specifies the type of archive. It can be: *, #, 7z, xz, split, zip, gzip, bzip2, tar, .... 
    /// </summary>
    [CommandOption("-t", Quoted = false, ValueSeparator = "")]
    public string ArchiveType { get; set; }

    /// <summary>
    /// Sets level of compression.
    /// </summary>
    [CommandOption("-mx", Quoted = false, ValueSeparator = "")]
    public int? CompressionLevel { get; set; }

    /// <summary>
    /// Compresses files open for writing by another applications. If this switch is not set, 7-zip doesn't include such files to archive.
    /// </summary>
    [CommandOption("-ssw", Flag = true)]
    public bool IncludeOpenForWritingFiles { get; set; }

    /// <summary>
    /// Specifies password.
    /// </summary>
    [CommandOption("-p", Quoted = false, ValueSeparator = "")]
    public string Password { get; set; }

    /// <summary>
    /// Enables or disables archive header encryption.
    /// </summary>
    [CommandOption("-mhe", Quoted = false, Flag = true)]
    public bool? HeaderEncryption { get; set; }

    /// <summary>
    /// Specifies volume sizes.
    /// </summary>
    [CommandOption("-v", Quoted = false, ValueSeparator = "")]
    public string VolumeSize { get; set; }

    [CommandOption]
    public string Output { get; set; }

    [CommandOption]
    public string Input { get; set; }


    public SevenZipCommandArgs() {
        Command = "a";
        AssumeYes = true;
    }
}

// Аргументы для командной строки WinRAR
public class WinRarArgs
{
    public List<string> Input = new List<string>();
    public string Output; // при установке рабочей папки по пути архива входные файлы можно не указывать
    public int? Compression = 5; // Метод сжатия (0-без сжатия...3-обычный...5-максимальный)
    public bool Sfx;
    public string SfxIconFilePath;
    public string SfxImageFilePath;
    public string SfxCommentFilePath;

    public WinRarArgs() {

    }

    public WinRarArgs(string output, params string[] input) : this() {
        Output = output;
        Input.AddRange(input);
    }

    public override string ToString() {
        StringBuilder args = new StringBuilder();
        args.AppendFormat(" a");
        if (Sfx) {
            args.AppendFormat(" -sfx");
        }
        if (Compression != null) {
            args.AppendFormat(" -m{0}", Compression);
        }
        if (SfxIconFilePath != null) {
            args.AppendFormat(" -iicon\"{0}\"", SfxIconFilePath);
        }
        if (SfxImageFilePath != null) {
            args.AppendFormat(" -iimg\"{0}\"", SfxImageFilePath);
        }
        if (SfxCommentFilePath != null) {
            args.AppendFormat(" -z\"{0}\"", SfxCommentFilePath);
        }
        if (Output != null) {
            args.AppendFormat(" \"{0}\"", Output);
        }
        foreach (string input in Input) {
            args.AppendFormat(" \"{0}\"", input);
        }
        return args.Remove(0, 1).ToString();
    }
}