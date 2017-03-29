using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Veeam_Test_SHA
{
    /// <summary>
    /// Выполняет работу по чтению и шифрованию файла в SHA256 частями
    /// </summary>
    class SHAConverter: IDisposable
    {
        /// <summary>
        /// Хранит в себе информацию и номер зашифрованной части
        /// </summary>
        class NumberedPart
        {
            public long Number { get; }  
            public byte[] Data { get; }

            public NumberedPart(long number, byte[] part)
            {
                Number = number;
                Data = part;
            }      
        }

        /// <summary>
        /// Приблизительное количество свободной оперативной памяти (в процентах) когда Windows начинает использовать файл подкачки
        /// </summary>
        private uint swapBoundary = 16;

        object dequeueSync = new object();

        private int partIndex = 0;
        private Queue queue = Queue.Synchronized(new Queue());
        private BinaryReader inputStream;

        public int PartSize { get; }
        public string FileName { get; }
        public int NumThreads { get; }

        /// <summary>
        /// Потоки, отвечающие за шифрование
        /// </summary>
        List<ManualThread> workers = new List<ManualThread>();

        /// <summary>
        /// Поток, отвечающий за чтение
        /// </summary>
        ManualThread reader;

        public SHAConverter(Options options)
        {
            PartSize = options.PartSize;
            FileName = options.Filename;
            inputStream = new BinaryReader(new FileStream(options.Filename, FileMode.Open, FileAccess.Read, FileShare.Read));
            NumThreads = options.NumThreads;

            reader = new ManualThread(new Thread(ReadDataPart));

            for (int i = 0; i < NumThreads; i++)
            {
                ManualThread worker = new ManualThread(new Thread(ConvertToSHA));
                workers.Add(worker);
            }
        }

        private bool IsWorkInProgress()
        {
            foreach (var worker in workers)
            {
                if (!worker.IsWorkCompleted)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Считывает часть данных из файла и помещает ее в очередь.
        /// </summary>
        /// <param name="obj">Поток, который работает с данным методом (класса ManualThread)</param>
        /// <exception cref="ArgumentException">Если аргумент метода не является объектом класса ManualThread</exception>
        private void ReadDataPart(object obj)
        {
            if (!(obj is ManualThread))
            {
                throw new ArgumentException("Метод чтения данных требует объект класса ManualThread");
            }

            ManualThread worker = obj as ManualThread;
            byte[] buffer = new byte[PartSize];
            long fileLength = inputStream.BaseStream.Length;
            ulong totalRam = Options.GetTotalRAM();

            while (fileLength > 0)
            {
                ulong ram = Options.GetAvailableRAMInBytes();

                if (ram < (uint)(PartSize * NumThreads) || ram < (totalRam / 100.0) * swapBoundary)
                {
                    Thread.Sleep(10);
                    if (queue.Count == 0)
                    {
                        GC.Collect();
                    }
                    continue;
                }

                int numBytes = inputStream.Read(buffer, 0, PartSize);
                if (numBytes == 0)
                {
                    break;
                }

                byte[] realPart = null;

                try
                {
                    realPart = new byte[numBytes];
                }
                catch (OutOfMemoryException)
                {
                    GC.Collect();
                    if (IsWorkInProgress())
                    {
                        while(queue.Count > 0)
                        {
                            Thread.Sleep(100);
                        }
                    }
                    realPart = new byte[numBytes];

                }

                Array.Copy(buffer, realPart, numBytes);
                queue.Enqueue(new NumberedPart(partIndex, realPart));
                partIndex++;
            }

            worker.Complete();
        }

        /// <summary>
        /// Запускает процесс чтения файла и шифрования его частей
        /// </summary>
        public void StartConverting()
        {
            reader.Thread.Start(reader);
            for (int i = 0; i < NumThreads; i++)
            {
                workers[i].Thread.Start(workers[i]);
            }
        }

        /// <summary>
        /// Шифрует данные с помощью алгоритма SHA256
        /// </summary>
        /// <param name="data">Данные в виде массива байтов</param>
        /// <returns>Зашифрованная строка</returns>
        private static string Sha256(byte[] data)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            StringBuilder hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(data, 0, data.Length);
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        /// <summary>
        /// Выполняет извлечение части данных из очереди, шифрует их с помощью SHA256 и выводит в стандартный поток вывода
        /// </summary>
        /// <param name="obj">Поток, который работает с даннным методом (класса ManualThread)</param>
        /// <exception cref="ArgumentException">Если аргумент метода не является объектом класса ManualThread</exception>
        /// <exception cref="InvalidOperationException">Если в очереди был объект класса типа, отличного от NumberedPart</exception>
        private void ConvertToSHA(object obj)
        {
            if (!(obj is ManualThread))
            {
                throw new ArgumentException("Метод шифрования требует объект класса ManualThread");
            }

            ManualThread worker = obj as ManualThread;

            while (true)
            {
                if (queue.Count == 0)
                {
                    if (!reader.IsWorkCompleted)
                    {
                        continue;
                    }
                    else
                    {
                        worker.Complete();
                        break;
                    }
                }
                else
                {
                    NumberedPart part = null;

                    lock (dequeueSync)
                    {
                        if (queue.Count > 0)
                        {
                            var tempPart = queue.Dequeue();
                            if (tempPart is NumberedPart)
                            {
                                part = tempPart as NumberedPart;
                            }
                            else
                            {
                                throw new InvalidOperationException("В очередь попал объект класса, отличающийся от NumberedPart");
                            }
                        }
                    }

                    if (part != null)
                    {
                        Console.WriteLine($"Часть № {part.Number}\n{Sha256(part.Data)}\n");
                    }
                }
            }
        }

        public void Dispose()
        {
            inputStream.Close();
        }

        /// <summary>
        /// Ожидает, пока все потоки завершат свое выполнение
        /// </summary>
        public void WaitResult()
        {
            List<ManualResetEvent> manualEvents = new List<ManualResetEvent>();
            manualEvents.Add(reader.ManualEvent);
            foreach (var worker in workers)
            {
                manualEvents.Add(worker.ManualEvent);
            }
            WaitHandle.WaitAll(manualEvents.ToArray());
        }
    }
}
