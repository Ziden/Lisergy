using Game.Engine;
using Game.Engine.Events;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class TestFixture : IDisposable
    {
        public TestFixture()
        {
            // runs before all tests
        }

        [TearDown]
        public void Dispose()
        {
            UnmanagedMemory.FlagMemoryToBeReused();
            if(EventPoolValidator.Used.Count > 0)
            {
                throw new Exception("Some events have not been returned to pool: " + string.Join(',', EventPoolValidator.Used));
            }
            Console.WriteLine("Disposing Test");
        }
    }
}
