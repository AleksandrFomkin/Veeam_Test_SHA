using System.Threading;

namespace Veeam_Test_SHA
{
    /// <summary>
    /// Представляет собой поток, для которого можно задать статус завершения работы
    /// </summary>
    class ManualThread
    {
        public Thread Thread { get; }
        public ManualResetEvent ManualEvent { get; }
        public bool IsWorkCompleted { get; set; } = false;

        public ManualThread(Thread thread)
        {
            Thread = thread;
            ManualEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Позволяет указать, что данный поток завершил свою работу
        /// </summary>
        public void Complete()
        {
            IsWorkCompleted = true;
            ManualEvent.Set();        
        }
    }
}
