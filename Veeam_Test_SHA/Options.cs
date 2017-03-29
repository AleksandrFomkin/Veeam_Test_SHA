using System;
using System.Diagnostics;
using System.IO;

namespace Veeam_Test_SHA
{
    /// <summary>
    /// Отвечает за хранение настроек программы
    /// </summary>
    public class Options
    { 
        private static readonly int maxThreadsCount = 20;
        private static readonly int minThreadsCount = 1;
        private static readonly int minPartSize = 5000;
        private static readonly int maxPartSize = 5000000;

        public static float RamCoefficient { get; } = 1048576.0F;

        private int _partSize;
        private int _numThreads;
        private string _fileName;

        /// <summary>
        /// Размер части, максимально считываемой потоком за раз
        /// </summary>
        public int PartSize
        {
            get
            {
                return _partSize;
            }
        }

        /// <summary>
        /// Количество потоков, работающих с шифрованием в SHA256
        /// </summary>
        public int NumThreads
        {
            get
            {
                return _numThreads;
            }
        }

        /// <summary>
        /// Имя файла, подлежащего обработке
        /// </summary>
        public string Filename
        {
            get
            {
                return _fileName;
            }
        }

        /// <summary>
        /// Позволяет получить количество доступной в данный момент оперативной памяти
        /// </summary>
        /// <returns>Количество операвтивной памяти в байтах</returns>
        public static ulong GetAvailableRAMInBytes() =>
            Convert.ToUInt64(new PerformanceCounter("Memory", "Available MBytes").NextValue() * RamCoefficient);

        /// <summary>
        /// Позволяет получить установленное на ПК количество оперативной памяти
        /// </summary>
        /// <returns>Общее количество операвтивной памяти в байтах</returns>
        public static ulong GetTotalRAM() =>
            new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;


        /// <summary>
        /// Позволяет проверить аргументы командной строки на корректность
        /// </summary>
        /// <param name="arguments">Аргументы командной строки</param>
        /// <example> 
        /// Аргументы необходимо вводить следующим образом:
        /// Первый аргумент - размер части, считываемой за раз
        /// Второй аргумент - количество потоков, работающих над шифрованием ("auto" - получить количество потоков автоматически)
        /// Третий аргумент - Путь к файлу, который будет шифроваться
        /// <code>
        /// Options options = new Options(new string[] {
        ///     "2000",
        ///     "4",
        ///     @"C:\data\MyFile.txt" 
        /// });
        /// 
        /// Options options = new Options(new string[] {
        ///     "3000",
        ///     "auto",
        ///     @"C:\data\MySmallFile.txt" 
        /// });    
        /// </code>
        /// </example>
        /// <exception cref="ArgumentException">Если какой-либо аргумент командной строки был задан некорректно</exception>
        public Options(string[] arguments)
        {
            if(arguments.Length != 3)
            {
                throw new ArgumentException("Количество аргументов, передаваемых в программу, должно быть равно трем.");
            }

            int partSize = 0;
            int numThreads = 0;
            bool getThreadsNum = false;
            string fileName = arguments[2];
            
            if(!int.TryParse(arguments[0], out partSize))
            {
                throw new ArgumentException("Размер блока данных не является целым числом.");
            }

            if(partSize <= 0)
            {
                throw new ArgumentException("Размер блока данных должен быть положительным числом.");
            }

            if(partSize < minPartSize || partSize > maxPartSize)
            {
                throw new ArgumentException($"Размер блока данных должен быть в диапазоне от {minPartSize} до {maxPartSize} байт.");
            }

            if((uint)partSize > GetAvailableRAMInBytes())
            {
                throw new ArgumentException("Размер блока данных превышает размер свободной оперативной памяти.");
            }

            if (arguments[1] == "auto")
            {
                getThreadsNum = true;
            }
            else
            {
                if(!int.TryParse(arguments[1], out numThreads))
                {
                    throw new ArgumentException("Допустимые значение для количества потоков - auto или целое число.");
                }
            }

            if(getThreadsNum)
            {
                numThreads = Environment.ProcessorCount;
            }

            if(numThreads < minThreadsCount || numThreads > maxThreadsCount)
            {
                throw new ArgumentException($"Недопустимое количество потоков. Допустимо не менее - {minThreadsCount} и не более {maxThreadsCount}.");
            }

            if (!File.Exists(fileName))
            {
                throw new ArgumentException($"Файла с именем {fileName} не существует.");
            }

            _numThreads = numThreads;
            _partSize = partSize;
            _fileName = fileName;
        }
    }
}
