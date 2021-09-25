﻿using System.Net;
using System.Net.Mail;

// utils.net
// РАБОТА С СЕТЬЮ
// ------------------------------------------------------------

///// #using System.Net;
///// #using System.Net.Mail;

///// #namespace

// Утилиты для работы с сетью
public static class NetUtils
{
    // Отправка электронного письма с использованием SMTP-сервера Mail.Ru
    public static void SendEmailFromMailRu(MailMessage message, string login, string password) {
        SmtpClient smtp = new SmtpClient("smtp.mail.ru", 25) {
            Credentials = new NetworkCredential(login, password),
            EnableSsl = true
        };
        smtp.Send(message);
    }
}
