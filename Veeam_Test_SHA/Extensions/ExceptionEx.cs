using System;

namespace Veeam_Test_SHA.Extensions
{
    public static class ExceptionEx
    {
        /// <summary>
        /// Выводит информацию об исключении в виде сообщения об ошибке и стека вызовов
        /// </summary>
        public static void Print(this Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
