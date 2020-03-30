using System;
using System.Text;

namespace KaprekarRoutineSolver
{
    /// <summary>
    ///     一个不定位数的自然数
    ///    0存放最低位, 长度-1 存放最高为
    /// </summary>
    public class Number
    {
        private readonly int[] digits;

        /// <summary>
        /// 位数
        /// </summary>
        private readonly int digitLength;

        /// <summary>
        /// 进制
        /// </summary>
        private readonly int baseNumber;

        public Number(int digitLength, int baseNumber)
        {
            this.digitLength = digitLength;
            this.baseNumber = baseNumber;
            digits = new int[digitLength];
        }

        public int this[int index]
        {
            get => digits[index];
            set => digits[index] = value;
        }

        /// <summary>
        /// 升序排列
        /// </summary>
        /// <param name="ascendingNumber"></param>
        public void Ascend(Number ascendingNumber)
        {
            for (int i = 0; i < digitLength; i++)
                ascendingNumber[i] = digits[i];

            for (int i = 0; i < digitLength; i++)
                for (int j = i + 1; j < digitLength; j++)
                    if (ascendingNumber[i] < ascendingNumber[j])
                    {
                        int t = ascendingNumber[j];
                        ascendingNumber[j] = ascendingNumber[i];
                        ascendingNumber[i] = t;
                    }
        }

        public void Descend(Number descendingNumber)
        {
            for (int i = 0; i < digitLength; i++)
                descendingNumber[i] = digits[i];

            for (int i = 0; i < digitLength; i++)
                for (int j = i + 1; j < digitLength; j++)
                    if (descendingNumber[i] > descendingNumber[j])
                    {
                        int t = descendingNumber[j];
                        descendingNumber[j] = descendingNumber[i];
                        descendingNumber[i] = t;
                    }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (baseNumber == 10)
                for (int i = digitLength - 1; i >= 0; i--)
                    stringBuilder.Append(digits[i]);
            else if (baseNumber == 16)
                for (int i = digitLength - 1; i >= 0; i--)
                    stringBuilder.Append(digits[i].ToString("X"));
            else
                for (int i = digitLength - 1; i >= 0; i--)
                {
                    stringBuilder.Append(digits[i]);
                    if (i > 0)
                        stringBuilder.Append(',');
                }

            return stringBuilder.ToString();
        }

        public static Number operator -(Number a, Number b)
        {
            if (a.digitLength != b.digitLength) throw new ArgumentException("位数不一致");
            if (a.baseNumber != b.baseNumber) throw new ArgumentException("进制不一致");

            int digitLength = a.digitLength;
            int baseNumber = a.baseNumber;
            var n = new Number(digitLength, baseNumber);
            int borrow = 0;
            for (int i = 0; i < digitLength; i++)
            {
                int a0 = a[i] + borrow;
                int b0 = b[i];
                if (a0 >= b0)
                {
                    n[i] = a0 - b0;
                    borrow = 0;
                }
                else
                {
                    n[i] = a0 + baseNumber - b0;
                    borrow = -1;
                }
            }

            if (borrow < 0)
                throw new ArgumentException("必须大的数字减小的数字");

            return n;
        }

        public static bool operator ==(Number a, Number b)
        {
            if (a is null) return false;
            if (b is null) return false;

            if (a.digitLength != b.digitLength) return false;
            if (a.baseNumber != b.baseNumber) return false;
            for (int i = 0; i < a.digitLength; i++)
                if (a[i] != b[i]) return false;

            return true;
        }

        public static bool operator !=(Number a, Number b)
        {
            return !(a == b);
        }
    }
}
