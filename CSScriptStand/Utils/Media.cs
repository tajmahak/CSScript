﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

// utils.media
// РАБОТА С МЕДИАФАЙЛАМИ (15.05.2021)
// ------------------------------------------------------------

///// #namespace

// Общие утилиты для работы с медиа
public static class MediaUtils
{
    // Разрешение изображения в мегапикселях
    public static float GetResolutionMP(int width, int height) {
        return (width * height) / 1000000f;
    }
}

// Утилиты для работы с "ExifTool"
public static class ExifToolUtils
{
    // Поворот изображения без потери качества
    public static string RotateImageArgs(string searchMask, bool overwriteOriginal, RotationAngle angle) {
        StringBuilder arg = new StringBuilder();
        int orientation;
        switch (angle) {
            case RotationAngle._0: orientation = 1; break;
            case RotationAngle._90: orientation = 6; break;
            case RotationAngle._180: orientation = 3; break;
            case RotationAngle._270: orientation = 8; break;
            default: throw new NotSupportedException("Неизвестный угол поворота изображения.");
        }
        arg.AppendFormat(" -Orientation=" + orientation);
        arg.AppendFormat(" -n");
        if (overwriteOriginal) {
            arg.AppendFormat(" -overwrite_original");
        }
        arg.AppendFormat(" \"{0}\"", searchMask);

        return arg.Remove(0, 1).ToString();
    }

    public enum RotationAngle
    {
        _0,
        _90,
        _180,
        _270,
    }
}

// Утилиты для работы с "FFMpeg"
public static class FFMpegUtils
{
    // Поворот видео без потери качества
    public static string RotateVideoArgs(string input, string output, RotationAngle angle) {
        StringBuilder arg = new StringBuilder();
        int rotate;
        switch (angle) {
            case RotationAngle._0: rotate = 0; break;
            case RotationAngle._90: rotate = 90; break;
            case RotationAngle._180: rotate = 180; break;
            case RotationAngle._270: rotate = 270; break;
            default: throw new NotSupportedException("Неизвестный угол поворота изображения.");
        }
        arg.AppendFormat(" -i \"{0}\"", Path.GetFullPath(input));
        arg.AppendFormat(" -c copy");
        arg.AppendFormat(" -metadata:s:v:0 rotate={0}", rotate);
        arg.AppendFormat(" \"{0}\"", Path.GetFullPath(output));
        return arg.Remove(0, 1).ToString();
    }

    // Стабилизация видео
    public static string VideoStabilizationArgs(string input, string output) {
        StringBuilder arg = new StringBuilder();
        arg.AppendFormat(" -i \"{0}\"", Path.GetFullPath(input));
        arg.AppendFormat(" -vf deshake");
        arg.AppendFormat(" \"{0}\"", Path.GetFullPath(output));
        return arg.Remove(0, 1).ToString();
    }

    public enum RotationAngle
    {
        _0,
        _90,
        _180,
        _270,
    }
}

// Утилиты для работы с "ImageMagick"
public static class ImageMagickUtils
{
    // Значение качества для JPEG (0-100)
    public static string GetJpegQualityArgs(string input) {
        StringBuilder arg = new StringBuilder();
        arg.AppendFormat(" -format %Q");
        arg.AppendFormat(" \"{0}\"", Path.GetFullPath(input));
        return arg.Remove(0, 1).ToString();
    }
}

// Утилиты для работы с "MediaInfo"
public static class MediaInfoUtils
{
    // Получение информации о файлах в формате XML
    public static string GetXmlInfoArgs(params string[] inputs) {
        StringBuilder arg = new StringBuilder();
        arg.AppendFormat(" --Output=XML");
        foreach (string input in inputs) {
            arg.AppendFormat(" \"{0}\"", Path.GetFullPath(input));
        }
        return arg.Remove(0, 1).ToString();
    }

    // Получение информации из XML-данных
    public static List<Media> ParseByXml(string xml) {
        List<Media> list = new List<Media>();

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xml);

        XmlNodeList xmlMediaInfoList = xmlDocument.GetElementsByTagName("MediaInfo");
        XmlElement xmlMediaInfo = (XmlElement)xmlMediaInfoList[0];
        XmlNodeList xmlMedia = xmlMediaInfo.GetElementsByTagName("media");
        foreach (XmlElement xmlMediaElement in xmlMedia) {
            Media media = new Media {
                Ref = xmlMediaElement.GetAttribute("ref")
            };

            foreach (XmlElement xmlTrackElement in xmlMediaElement.GetElementsByTagName("track")) {
                string trackType = xmlTrackElement.GetAttribute("type");
                Track track;
                switch (trackType) {
                    case "General":
                        track = new GeneralTrack();
                        media.GeneralTracks.Add((GeneralTrack)track);
                        break;
                    case "Video":
                        track = new VideoTrack();
                        media.VideoTracks.Add((VideoTrack)track);
                        break;
                    case "Audio":
                        track = new AudioTrack();
                        media.AudioTracks.Add((AudioTrack)track);
                        break;
                    case "Image":
                        track = new ImageTrack();
                        media.ImageTracks.Add((ImageTrack)track);
                        break;
                    default:
                        track = new Track();
                        media.OtherTracks.Add(track);
                        break;
                }
                track.Type = trackType;

                foreach (XmlElement xmlTrackNode in xmlTrackElement.ChildNodes) {
                    string name = xmlTrackNode.Name;
                    string value = xmlTrackNode.InnerText;
                    track.Values.Add(name, value);
                }
            }
            list.Add(media);
        }
        return list;
    }


    public class Media
    {
        public string Ref { get; set; }
        public List<GeneralTrack> GeneralTracks { get; private set; }
        public List<VideoTrack> VideoTracks { get; private set; }
        public List<AudioTrack> AudioTracks { get; private set; }
        public List<ImageTrack> ImageTracks { get; private set; }
        public List<Track> OtherTracks { get; private set; }

        public Media() {
            GeneralTracks = new List<GeneralTrack>();
            VideoTracks = new List<VideoTrack>();
            AudioTracks = new List<AudioTrack>();
            ImageTracks = new List<ImageTrack>();
            OtherTracks = new List<Track>();
        }

        public override string ToString() {
            return Ref;
        }
    }

    public class Track
    {
        public string Type { get; set; }
        public Dictionary<string, string> Values { get; private set; }

        public Track() {
            Values = new Dictionary<string, string>();
        }

        public string GetValue(string name) {
            return Values.ContainsKey(name) ? Values[name] : null;
        }

        public override string ToString() {
            return Type;
        }
    }

    public class GeneralTrack : Track
    {
        public string VideoCount { get { return GetValue("VideoCount"); } }
        public string AudioCount { get { return GetValue("AudioCount"); } }
        public string FileExtension { get { return GetValue("FileExtension"); } }
        public string Format { get { return GetValue("Format"); } }
        public string Format_Profile { get { return GetValue("Format_Profile"); } }
        public string CodecID { get { return GetValue("CodecID"); } }
        public string CodecID_Compatible { get { return GetValue("CodecID_Compatible"); } }
        public string FileSize { get { return GetValue("FileSize"); } }
        public string Duration { get { return GetValue("Duration"); } }
        public string OverallBitRate { get { return GetValue("OverallBitRate"); } }
        public string FrameRate { get { return GetValue("FrameRate"); } }
        public string FrameCount { get { return GetValue("FrameCount"); } }
        public string StreamSize { get { return GetValue("StreamSize"); } }
        public string HeaderSize { get { return GetValue("HeaderSize"); } }
        public string DataSize { get { return GetValue("DataSize"); } }
        public string FooterSize { get { return GetValue("FooterSize"); } }
        public string IsStreamable { get { return GetValue("IsStreamable"); } }
        public string Encoded_Date { get { return GetValue("Encoded_Date"); } }
        public string Tagged_Date { get { return GetValue("Tagged_Date"); } }
        public string File_Created_Date { get { return GetValue("File_Created_Date"); } }
        public string File_Created_Date_Local { get { return GetValue("File_Created_Date_Local"); } }
        public string File_Modified_Date { get { return GetValue("File_Modified_Date"); } }
        public string File_Modified_Date_Local { get { return GetValue("File_Modified_Date_Local"); } }
        public string extra { get { return GetValue("extra"); } }
    }

    public class VideoTrack : Track
    {
        public string StreamOrder { get { return GetValue("StreamOrder"); } }
        public string ID { get { return GetValue("ID"); } }
        public string Format { get { return GetValue("Format"); } }
        public string Format_Profile { get { return GetValue("Format_Profile"); } }
        public string Format_Level { get { return GetValue("Format_Level"); } }
        public string Format_Settings_CABAC { get { return GetValue("Format_Settings_CABAC"); } }
        public string Format_Settings_RefFrames { get { return GetValue("Format_Settings_RefFrames"); } }
        public string Format_Settings_GOP { get { return GetValue("Format_Settings_GOP"); } }
        public string CodecID { get { return GetValue("CodecID"); } }
        public string Duration { get { return GetValue("Duration"); } }
        public string BitRate { get { return GetValue("BitRate"); } }
        public string Width { get { return GetValue("Width"); } }
        public string Height { get { return GetValue("Height"); } }
        public string Sampled_Width { get { return GetValue("Sampled_Width"); } }
        public string Sampled_Height { get { return GetValue("Sampled_Height"); } }
        public string PixelAspectRatio { get { return GetValue("PixelAspectRatio"); } }
        public string DisplayAspectRatio { get { return GetValue("DisplayAspectRatio"); } }
        public string Rotation { get { return GetValue("Rotation"); } }
        public string FrameRate_Mode { get { return GetValue("FrameRate_Mode"); } }
        public string FrameRate { get { return GetValue("FrameRate"); } }
        public string FrameRate_Minimum { get { return GetValue("FrameRate_Minimum"); } }
        public string FrameRate_Maximum { get { return GetValue("FrameRate_Maximum"); } }
        public string FrameCount { get { return GetValue("FrameCount"); } }
        public string ColorSpace { get { return GetValue("ColorSpace"); } }
        public string ChromaSubsampling { get { return GetValue("ChromaSubsampling"); } }
        public string BitDepth { get { return GetValue("BitDepth"); } }
        public string ScanType { get { return GetValue("ScanType"); } }
        public string StreamSize { get { return GetValue("StreamSize"); } }
        public string Title { get { return GetValue("Title"); } }
        public string Language { get { return GetValue("Language"); } }
        public string Encoded_Date { get { return GetValue("Encoded_Date"); } }
        public string Tagged_Date { get { return GetValue("Tagged_Date"); } }
        public string colour_description_present { get { return GetValue("colour_description_present"); } }
        public string colour_description_present_Source { get { return GetValue("colour_description_present_Source"); } }
        public string colour_range { get { return GetValue("colour_range"); } }
        public string colour_range_Source { get { return GetValue("colour_range_Source"); } }
        public string colour_primaries { get { return GetValue("colour_primaries"); } }
        public string colour_primaries_Source { get { return GetValue("colour_primaries_Source"); } }
        public string transfer_characteristics { get { return GetValue("transfer_characteristics"); } }
        public string transfer_characteristics_Source { get { return GetValue("transfer_characteristics_Source"); } }
        public string matrix_coefficients { get { return GetValue("matrix_coefficients"); } }
        public string matrix_coefficients_Source { get { return GetValue("matrix_coefficients_Source"); } }
        public string extra { get { return GetValue("extra"); } }
    }

    public class AudioTrack : Track
    {
        public string StreamOrder { get { return GetValue("StreamOrder"); } }
        public string ID { get { return GetValue("ID"); } }
        public string Format { get { return GetValue("Format"); } }
        public string Format_AdditionalFeatures { get { return GetValue("Format_AdditionalFeatures"); } }
        public string CodecID { get { return GetValue("CodecID"); } }
        public string Duration { get { return GetValue("Duration"); } }
        public string BitRate_Mode { get { return GetValue("BitRate_Mode"); } }
        public string BitRate { get { return GetValue("BitRate"); } }
        public string Channels { get { return GetValue("Channels"); } }
        public string ChannelPositions { get { return GetValue("ChannelPositions"); } }
        public string ChannelLayout { get { return GetValue("ChannelLayout"); } }
        public string SamplesPerFrame { get { return GetValue("SamplesPerFrame"); } }
        public string SamplingRate { get { return GetValue("SamplingRate"); } }
        public string SamplingCount { get { return GetValue("SamplingCount"); } }
        public string FrameRate { get { return GetValue("FrameRate"); } }
        public string FrameCount { get { return GetValue("FrameCount"); } }
        public string Compression_Mode { get { return GetValue("Compression_Mode"); } }
        public string StreamSize { get { return GetValue("StreamSize"); } }
        public string StreamSize_Proportion { get { return GetValue("StreamSize_Proportion"); } }
        public string Title { get { return GetValue("Title"); } }
        public string Language { get { return GetValue("Language"); } }
        public string Encoded_Date { get { return GetValue("Encoded_Date"); } }
        public string Tagged_Date { get { return GetValue("Tagged_Date"); } }
        public string extra { get { return GetValue("extra"); } }
    }

    public class ImageTrack : Track
    {
        public string Format { get { return GetValue("Format"); } }
        public string Width { get { return GetValue("Width"); } }
        public string Height { get { return GetValue("Height"); } }
        public string ColorSpace { get { return GetValue("ColorSpace"); } }
        public string ChromaSubsampling { get { return GetValue("ChromaSubsampling"); } }
        public string BitDepth { get { return GetValue("BitDepth"); } }
        public string Compression_Mode { get { return GetValue("Compression_Mode"); } }
        public string StreamSize { get { return GetValue("StreamSize"); } }
    }
}

// Аргументы для работы с "youtube-dl"
public class YoutubeDlArgs
{
    public List<string> Urls { get; private set; }
    public string Format { get; set; }
    public string Output { get; set; }
    public bool NoPlaylist { get; set; }
    public string PlaylistItems { get; set; }
    public string FFMpegLocation { get; set; }
    public bool ListFormats { get; set; }
    public bool AddMetaData { get; set; }

    public YoutubeDlArgs() {
        Urls = new List<string>();
    }

    public YoutubeDlArgs AddUrls(string urls) {
        string[] split = urls.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string url in split) {
            Urls.Add(url);
        }
        return this;
    }

    public override string ToString() {
        StringBuilder args = new StringBuilder();

        if (ListFormats) {
            args.Append(" --list-formats");
        }
        if (Format != null) {
            args.Append(" --format \"" + Format + "\"");
        }
        if (Output != null) {
            args.Append(" --output \"" + Output + "\"");
        }
        if (PlaylistItems != null) {
            args.Append(" --playlist-items \"" + PlaylistItems + "\"");
        }
        if (NoPlaylist) {
            args.Append(" --no-playlist");
        }
        if (AddMetaData) {
            args.Append(" --add-metadata");
        }
        if (FFMpegLocation != null) {
            args.Append(" --ffmpeg-location \"" + FFMpegLocation + "\"");
        }
        foreach (string url in Urls) {
            args.Append(" \"" + url + "\"");
        }

        return args.Remove(0, 1).ToString();
    }
}