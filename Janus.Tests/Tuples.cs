using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Janus.Tests
{
    [TestClass]
    public class Tuples
    {
        [TestMethod]
        public void ATupleWithW1IsAPoint()
        {
            var tuple = new Tuple(4.3f, -4.2f, 3.1f, 1.0f);
            Assert.IsTrue(tuple.X == 4.3f);
            Assert.IsTrue(tuple.Y == -4.2f);
            Assert.IsTrue(tuple.Z == 3.1f);
            Assert.IsTrue(tuple.W == 1.0f);
            Assert.IsTrue(tuple.IsPoint());
            Assert.IsFalse(tuple.IsVector());
        }

        [TestMethod]
        public void ATupleWithW0IsAVector()
        {
            var tuple = new Tuple(4.3f, -4.2f, 3.1f, 0.0f);
            Assert.IsTrue(tuple.X == 4.3f);
            Assert.IsTrue(tuple.Y == -4.2f);
            Assert.IsTrue(tuple.Z == 3.1f);
            Assert.IsTrue(tuple.W == 0.0f);
            Assert.IsFalse(tuple.IsPoint());
            Assert.IsTrue(tuple.IsVector());
        }
    }
}
