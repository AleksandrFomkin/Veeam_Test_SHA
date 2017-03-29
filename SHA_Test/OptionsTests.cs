using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Veeam_Test_SHA;
using System.IO;

namespace SHA_Tests
{
    [TestClass]
    public class OptionsTests
    {
        private static string GoodPath => (new FileInfo(AppDomain.CurrentDomain.BaseDirectory)).Directory.Parent.FullName + "\\Files\\Small.txt";
        private static string GoodPartSize = "2000";
        private static string GoodNumThreads = "4";

        [TestMethod]
        public void OptionsCtor_GoodArguments()
        {
            Options options = new Options(new string[] { GoodPartSize, GoodNumThreads, GoodPath });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OptionsCtor_EmptyArguments()
        {
            Options options = new Options(new string[] { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OptionsCtor_WrongThreadsNum()
        {
            Options options = new Options(new string[] { GoodPartSize, "-1", GoodPath });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OptionsCtor_WrongFileName()
        {
            Options options = new Options(new string[] { GoodPartSize, GoodNumThreads, "sdfdrgfgty" });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OptionsCtor_WrongPartSize()
        {
            Options options = new Options(new string[] { "0", GoodNumThreads, GoodPath });
        }
    }
}
