using KaprekarRoutineSolver;
using NUnit.Framework;

namespace KaprekarRoutineSolverTests
{
    [TestFixture]
    public class KaprekarRoutineTests
    {
        [Test]
        public void SolveTest()
        {
            var kaprekarRoutine = new KaprekarRoutine(4, 10);
            var n = new Number(4, 10) { [3] = 1, [2] = 2, [1] = 3, [0] = 4 };

            var solution = kaprekarRoutine.Solve(n);
            Assert.AreEqual(solution.ToString(), "6174");
        }

        [Test]
        public void SolveTest1()
        {
            var kaprekarRoutine = new KaprekarRoutine(3, 10);
            string result = kaprekarRoutine.Solve();
            Assert.AreEqual(result, "000|495|");
        }

        [Test]
        public void SolveTest2()
        {
            var kaprekarRoutine = new KaprekarRoutine(4, 10);
            string result = kaprekarRoutine.Solve();
            Assert.AreEqual(result, "0000|6174|");
        }

        //[Test]
        public void SolveTest3()
        {
            var kaprekarRoutine = new KaprekarRoutine(6, 10);
            string result = kaprekarRoutine.Solve();
            //Assert.AreEqual(result, "0000|6174|");
        }

        [Test]
        public void SolveTest4()
        {
            var kaprekarRoutine = new KaprekarRoutine(3, 16);
            string result = kaprekarRoutine.Solve();
            Assert.AreEqual(result, "000|7F8|");
        }

        [Test]
        public void TraverseTest()
        {
            var kaprekarRoutine = new KaprekarRoutine(2, 10);
            string result = kaprekarRoutine.Traverse();
            Assert.AreEqual(result, "99|89|79|69|59|49|39|29|19|09|" +
                "88|78|68|58|48|38|28|18|08|" +
                "77|67|57|47|37|27|17|07|" +
                "66|56|46|36|26|16|06|" +
                "55|45|35|25|15|05|" +
                "44|34|24|14|04|" +
                "33|23|13|03|" +
                "22|12|02|" +
                "11|01|" +
                "00|");
        }
    }
}
