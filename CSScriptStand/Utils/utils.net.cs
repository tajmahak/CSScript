using System;
using System.Net;
using System.Net.Mail;
using System.Text;

// utils.net
// РАБОТА С СЕТЬЮ
// ------------------------------------------------------------

//## #namespace

// Утилиты для работы с сетью
public static class NetUtils
{
    // Отправка электронного письма с использованием SMTP-сервера Mail.Ru
    public static void SendEmailFromMailRu(MailMessage message, string login, string password) {
        var smtp = new SmtpClient("smtp.mail.ru", 25) {
            Credentials = new NetworkCredential(login, password),
            EnableSsl = true
        };
        smtp.Send(message);
    }
}

public class ScriptWebClient : WebClient
{
    public CookieContainer CookieContainer { get; set; }

    public ScriptWebClient() {
        Encoding = Encoding.UTF8;
        Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
    }

    protected override WebRequest GetWebRequest(Uri address) {
        var request = base.GetWebRequest(address) as HttpWebRequest;
        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        request.CookieContainer = CookieContainer;
        return request;
    }
}

