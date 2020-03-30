using System;
using System.Collections.Generic;
using System.Text;

namespace KaprekarRoutineSolver
{
    /// <summary>
    ///     卡普雷卡尔变换
    /// </summary>
    public class KaprekarRoutine
    {
        /// <summary>
        ///     位数
        /// </summary>
        private readonly int digitLength;

        /// <summary>
        ///     进制
        /// </summary>
        private readonly int baseNumber;

        /// <summary>
        ///     当前计算的数值
        /// </summary>
        private readonly Number number;

        /// <summary>
        ///     变换序列
        /// </summary>
        private readonly RoutineSequence routineSequence;

        /// <summary>
        ///     降序排列的数 (缓存)
        /// </summary>
        private readonly Number descendingNumber;

        /// <summary>
        ///     升序排列的数 (缓存)
        /// </summary>
        private readonly Number ascendingNumber;

        // TODO: 循环的数列可以从任何一个数开始, 只需要记录一个(可以是数列中最小的那一个)
        // 解集可以包含所有数字, 只要出现了其中一个数字就可以结束了

        /// <summary>
        ///     解集
        /// </summary>
        private readonly HashSet<string> solutions;

        public KaprekarRoutine(int digitLength, int baseNumber)
        {
            this.digitLength = digitLength;
            this.baseNumber = baseNumber;

            number = new Number(digitLength, baseNumber);
            descendingNumber = new Number(digitLength, baseNumber);
            ascendingNumber = new Number(digitLength, baseNumber);

            routineSequence = new RoutineSequence();
            solutions = new HashSet<string>();
        }

        // 卡普雷卡尔变换有三种结果
        // 1. 进入A->B->C->...->A的循环 (卡普雷卡尔变换的结果是固定的, 并且4位数是有限的, 所以必然会进入到循环)
        // 2. 进入C->C->C的循环, 这个C就称之为卡普雷卡尔常数
        // 3. 进入0->0->0的循环, 对于各位数字相同的数会进入该循环

        public string Solve()
        {
            solutions.Clear();

            // 使用递归对各个位数进行赋值
            Set(-1, baseNumber - 1);

            var stringBuilder = new StringBuilder();
            foreach (string solution in solutions)
            {
                stringBuilder.Append(solution);
                stringBuilder.Append("|");
            }

            // TODO: 解应该按照数列的第一个数字从小到大排列
            return stringBuilder.ToString();
        }

        private void Set(int digit, int n)
        {
            // 设置当前位数
            if (digit >= 0)
                number[digit] = n;

            if (digit >= digitLength - 1)
            {
                // 已经设置到最后一位, 递归结束
                SolveOne();
                return;
            }

            // 对后一位进行设置, 要求数值不能超过当前位数
            for (int i = n; i >= 0; i--)
                Set(digit + 1, i);
        }

        /// <summary>
        ///     对当前的number, 进行求解
        /// </summary>
        private void SolveOne()
        {
            // 创建一个列表用于记录各次变换的结果
            // 一旦有一个结果与之前的值相同, 那么求解结束
            // 检查这个解是否存在, 如果不存在则进行记录

            var solution = Solve(number);
            string solutionText = solution.ToString();
            if (!solutions.Contains(solutionText))
                solutions.Add(solutionText);
        }

        public RoutineSequence Solve(Number n)
        {
            routineSequence.Clear();
            var result = n;
            while (true)
            {
                Console.WriteLine(result);
                result = PerformRoutine(result);
                if (routineSequence.Contains(result))
                    return routineSequence.GetSolution(result);
                routineSequence.Add(result);
            }
        }

        /// <summary>
        ///     进行一次变换
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private Number PerformRoutine(Number n)
        {
            n.Ascend(ascendingNumber);
            n.Descend(descendingNumber);

            return descendingNumber - ascendingNumber;
        }

        #region 遍历的测试

        private StringBuilder traverseStringBuilder;

        public string Traverse()
        {
            traverseStringBuilder = new StringBuilder();
            Traverse(-1, baseNumber - 1);
            return traverseStringBuilder.ToString();
        }

        private void Traverse(int digit, int n)
        {
            // 设置当前位数
            if (digit >= 0)
                number[digit] = n;

            if (digit >= digitLength - 1)
            {
                // 已经设置到最后一位, 递归结束
                traverseStringBuilder.Append(number);
                traverseStringBuilder.Append("|");
                return;
            }

            // 对后一位进行设置, 要求数值不能超过当前位数
            for (int i = n; i >= 0; i--)
                Traverse(digit + 1, i);
        }

        #endregion
    }
}
