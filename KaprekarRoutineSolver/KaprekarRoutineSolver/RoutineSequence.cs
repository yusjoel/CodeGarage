using System.Collections.Generic;
using System.Text;

namespace KaprekarRoutineSolver
{
    /// <summary>
    ///     变换序列
    /// </summary>
    public class RoutineSequence
    {
        private readonly List<Number> sequence = new List<Number>();

        public void Clear()
        {
            sequence.Clear();
        }

        public bool Contains(Number number)
        {
            foreach (var item in sequence)
            {
                if (item == number)
                    return true;
            }

            return false;
        }

        public RoutineSequence GetSolution(Number number)
        {
            var solution = new RoutineSequence();
            bool add = false;
            foreach (var item in sequence)
            {
                if (item == number) add = true;
                if (add)
                    solution.Add(item);
            }

            return solution;
        }

        public void Add(Number number)
        {
            sequence.Add(number);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < sequence.Count; i++)
            {
                stringBuilder.Append(sequence[i]);
                if (i < sequence.Count - 1)
                    stringBuilder.Append(" -> ");
            }

            return stringBuilder.ToString();
        }
    }
}
