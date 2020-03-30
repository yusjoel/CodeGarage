using KaprekarRoutineSolver;
using NUnit.Framework;

namespace KaprekarRoutineSolverTests
{
    [TestFixture]
    public class NumberTests
    {
        [Test]
        public void AscendTest()
        {
            var n = new Number(4, 10) { [3] = 6, [2] = 1, [1] = 7, [0] = 4 };
            var ascendingNumber = new Number(4, 10);
            n.Ascend(ascendingNumber);

            Assert.AreEqual(ascendingNumber.ToString(), "1467");
        }

        [Test]
        public void DescendTest()
        {
            var n = new Number(4, 10) { [3] = 6, [2] = 1, [1] = 7, [0] = 4 };
            var descendingNumber = new Number(4, 10);
            n.Descend(descendingNumber);

            Assert.AreEqual(descendingNumber.ToString(), "7641");
        }

        [Test]
        public void SubtractTest()
        {
            var n = new Number(4, 10) { [3] = 6, [2] = 1, [1] = 7, [0] = 4 };
            var ascendingNumber = new Number(4, 10);
            n.Ascend(ascendingNumber);
            var descendingNumber = new Number(4, 10);
            n.Descend(descendingNumber);

            var difference = descendingNumber - ascendingNumber;
            Assert.AreEqual(difference.ToString(), "6174");
        }
    }
}
