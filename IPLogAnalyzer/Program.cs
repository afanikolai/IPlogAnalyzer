using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IPLogAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string logFilePath = GetArgumentValue(args, "--file-log");
            string outputFilePath = GetArgumentValue(args, "--file-output");
            string addressStart = GetArgumentValue(args, "--address-start");
            string addressMask = GetArgumentValue(args, "--address-mask");
            string timeStart = GetArgumentValue(args, "--time-start");
            string timeEnd = GetArgumentValue(args, "--time-end");

            // Проверка обязательных параметров
            if (string.IsNullOrEmpty(logFilePath) || string.IsNullOrEmpty(outputFilePath) ||
                string.IsNullOrEmpty(timeStart) || string.IsNullOrEmpty(timeEnd))
            {
                Console.WriteLine("Необходимо указать путь к файлу журнала, путь к выходному файлу, " +
                    "начальное и конечное время.");
                return;
            }

            // Чтение файла журнала
            List<string> logEntries = new List<string>();
            try
            {
                logEntries = File.ReadAllLines(logFilePath).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла журнала: {ex.Message}");
                return;
            }

            // Фильтрация записей по заданным параметрам
            IEnumerable<string> filteredEntries = FilterLogEntries(logEntries, addressStart, addressMask, timeStart, timeEnd);

            // Подсчет количества обращений с каждого адреса
            Dictionary<string, int> ipCounts = CountIPAddresses(filteredEntries);

            // Запись результатов в выходной файл
            try
            {
                if (ipCounts.Count > 0)
                {
                    using (StreamWriter writer = new StreamWriter(outputFilePath))
                    {
                        foreach (var entry in ipCounts)
                        {
                            writer.WriteLine($"{entry.Key}: {entry.Value}");
                        }
                    }
                    Console.WriteLine("Результаты успешно записаны в файл.");
                }
                else
                {
                    throw new Exception("В диапазон не вошел ни один адрес.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи результатов. {ex.Message}");
            }

        }

        static IEnumerable<string> FilterLogEntries(List<string> logEntries, string addressStart, string addressMask, string timeStart, string timeEnd)
        {
            if (!DateTime.TryParseExact(timeStart, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime startTime) ||
                !DateTime.TryParseExact(timeEnd, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime endTime))
            {
                Console.WriteLine("Некорректный формат даты. Используйте формат dd.MM.yyyy");
                return Enumerable.Empty<string>();
            }

            IEnumerable<string> filteredEntries = logEntries.Where(entry =>
            {
                string[] separators = { ": " };
                string[] parts = entry.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    return false;

                // Фильтрация по времени
                if (!DateTime.TryParseExact(parts[1], "dd.MM.yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime entryTime))
                    return false;
                if (entryTime < startTime || entryTime > endTime)
                    return false;

                // Фильтрация по адресу 
                if (!string.IsNullOrEmpty(addressStart) && !CheckAddressStart(parts[0], addressStart))
                    return false;
                if (!string.IsNullOrEmpty(addressMask) && !CheckMask(parts[0], addressMask))
                    return false;

                return true;
            });

            return filteredEntries;
        }

        static bool CheckAddressStart(string ipAddress, string addressStart)
        {
            string[] ipParts = ipAddress.Split('.');
            string[] startParts = addressStart.Split('.');

            for (int i = 0; i < Math.Min(ipParts.Length, startParts.Length); i++)
            {
                if (int.Parse(ipParts[i]) < int.Parse(startParts[i]))
                    return false;
            }
            return true;
        }

        static bool CheckMask(string ipAddress, string addressMask)
        {
            string[] ipParts = ipAddress.Split('.');
            string[] maskParts = addressMask.Split('.');
            int[] ipBytes = new int[4];
            int[] maskBytes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                ipBytes[i] = int.Parse(ipParts[i]);
                maskBytes[i] = int.Parse(maskParts[i]);
            }
            // Применяем маску подсети к каждому байту IP-адреса
            for (int i = 0; i < 4; i++)
            {
                int maskedIpByte = ipBytes[i] & maskBytes[i];
                if (maskedIpByte != ipBytes[i])
                    return false;
            }
            return true;
        }




        static Dictionary<string, int> CountIPAddresses(IEnumerable<string> entries)
        {
            Dictionary<string, int> ipCounts = new Dictionary<string, int>();
            foreach (var entry in entries)
            {
                string ipAddress = entry.Split(':')[0];
                if (ipCounts.ContainsKey(ipAddress))
                    ipCounts[ipAddress]++;
                else
                    ipCounts[ipAddress] = 1;
            }
            return ipCounts;
        }

        static string GetArgumentValue(string[] args, string argument)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argument)
                    return args[i + 1];
            }
            return null;
        }
    }
}
