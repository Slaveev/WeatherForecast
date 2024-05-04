using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

// to run this you need to add 4 arguments throught the terminal like this ./exe <folder path> <email from> <password> <email to>
// then a message on the terminal will be displayed and its going to ask for language

namespace WeatherHitachi
{
    internal class WeatherForecast
    {
        public string Day, Clouds, Lightning, Location;
        public double Temperature, Wind, Humidity, Precipitation, Latitude;

        public WeatherForecast(string[] data, string location, double latitude)
        {
            this.Day = data[0];
            this.Temperature = Convert.ToDouble(data[1]);
            this.Wind = Convert.ToDouble(data[2]);
            this.Humidity = Convert.ToDouble(data[3]);
            this.Precipitation = Convert.ToDouble(data[4]);
            this.Lightning = data[5];
            this.Clouds = data[6];
            this.Latitude = latitude;
            this.Location = location;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 4)
                {
                    Console.WriteLine("Please provide correct arguments ./exe <folder path> <email from> <password> <email to>");
                    return;
                }
                var folderPath = args[0];
                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine($"Error occurred with the folder path: {folderPath}");
                    return;
                }
                var senderMail = args[1];

                // my password for gmail - iuwd pstc lqoi zpsa
                var password = args[2];

                var receiverMail = args[3];

                if (!senderMail.Contains("@") || !receiverMail.Contains("@"))
                {
                    Console.WriteLine("Invalid email format");
                    return;
                }

                var language = DisplayMessage();

                var locationLatitude = new Dictionary<string, double>
                {
                    { "Kourou, French Guyana", 5.1 },
                    { "Tanegashima, Japan", 30.4 },
                    { "Cape Canaveral, USA", 28.4 },
                    { "Mahia, New Zealand", -39.2 },
                    { "Kodiak, USA", 57.8 }
                };

                var fileNameToLocation = new Dictionary<string, string>
                {
                    { "Cape Canaveral, USA.csv", "Cape Canaveral" },
                    { "Kodiak, USA.csv", "Kodiak"},
                    { "Kourou, French Guyana.csv", "Kourou"},
                    { "Mahia, New Zealand.csv", "Mahia" },
                    { "Tanegashima, Japan.csv", "Tanegashima" }
                };

                var csvFiles = Directory.GetFiles(folderPath, "*.csv");
                var bestDays = new List<WeatherForecast>();

                // iterate over each file so i can get the best day
                foreach (var file in csvFiles)
                {
                    var locationName = Path.GetFileNameWithoutExtension(file);
                    var arrayCSV = File.ReadAllLines(file).Select(line => line.Split(',')).ToList();
                    var convertedCsv = AdjustRows(arrayCSV);
                    var forecasts = convertedCsv.Select(row => new WeatherForecast(row, locationName, locationLatitude[locationName])).ToList();
                    var filteredForecasts = FilterForecasts(forecasts);
                    var bestDay = FindBestDay(filteredForecasts);
                    bestDays.Add(bestDay);
                    //Console.WriteLine($"The best day for launch in {locationName} is: {bestDay.Day}");
                }

                var overallBest = FindBestDay(bestDays);
                //Console.WriteLine($"The best overall launch day is at {overallBest.Location}");

                var csvName = "LaunchAnalysisReport.csv";
                CreateCsvFile(bestDays, csvName, language);
                SendEmail(senderMail, password, receiverMail, csvName, language);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex}");
            }
        }

        // swapping rows with columns to convert it to column-oriented format
        public static List<string[]> AdjustRows(List<string[]> data)
        {
            var result = new List<string[]>();

            for (int i = 1; i < data[0].Length; i++)
            {
                var row = new string[data.Count];
                for (int j = 0; j < data.Count; j++)
                {
                    row[j] = data[j][i];
                }
                result.Add(row);
            }

            return result;
        }


        // criteria for launch day
        public static List<WeatherForecast> FilterForecasts(List<WeatherForecast> forecasts)
        {
            return forecasts.Where(forecast =>
                forecast.Temperature >= 1 &&
                forecast.Temperature <= 32 &&
                forecast.Wind <= 11 &&
                forecast.Humidity <= 55 &&
                forecast.Precipitation == 0 &&
                forecast.Lightning == "No" &&
                forecast.Clouds != "Cumulus" &&
                forecast.Clouds != "Nimbus"
            ).ToList();
        }


        // sorting them based on the criteria for best day
        public static WeatherForecast FindBestDay(List<WeatherForecast> forecasts)
        {
            return forecasts.OrderBy(forecast => Math.Abs(forecast.Latitude))
                            .ThenBy(forecast => forecast.Wind)
                            .ThenBy(forecast => forecast.Humidity)
                            .First();
        }

        // creating a csv file that is going to be send
        public static void CreateCsvFile(List<WeatherForecast> bestDays, string filename, string language)
        {
            if (language == "1")
            {
                using (var writer = new StreamWriter(filename))
                {
                    writer.WriteLine("Spacesport, Best Day for launch");
                    foreach (var day in bestDays)
                    {
                        var locationName = day.Location.Split(',')[0];
                        writer.WriteLine($"{locationName}, {day.Day}");
                    }
                }
            }
            else
            {
                using (var writer = new StreamWriter(filename))
                {
                    writer.WriteLine("Starttag, Bester Tag für den Start");
                    foreach (var day in bestDays)
                    {
                        var locationName = day.Location.Split(',')[0];
                        writer.WriteLine($"{locationName}, {day.Day}");
                    }
                }
            }
        }

        // simple output message for english or german (not sure if thats what i was supposed to do)
        public static string DisplayMessage()
        {
            string input;
            do
            {
                Console.WriteLine("Report is ready. Press 1 for English or 2 for German");
                input = Console.ReadLine();
            } while (input != "1" && input != "2");
            //Console.WriteLine("Report Sended");
            return input;
        }

        // send email method which works with gmail
        public static void SendEmail(string senderEmail, string password, string receiverEmail, string filename, string language)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Your Name", senderEmail));
            message.To.Add(new MailboxAddress("Receiver Name", receiverEmail));

            if (language == "1") // English
            {
                message.Subject = "Analysis for launch day";
            }
            else if (language == "2") // German
            {
                message.Subject = "Analyse für den Starttag";
            }

            var attachment = new MimePart("text", "csv")
            {
                Content = new MimeContent(File.OpenRead(filename)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(filename)
            };

            var multipart = new Multipart("mixed");
            multipart.Add(attachment);

            message.Body = multipart;

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate(senderEmail, password);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}