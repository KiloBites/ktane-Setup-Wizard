using PuzzleSolvers;

namespace SetupWizard
{
    class Equation
    {
        public int Left { get; private set; }
        public Operator Op { get; private set; }
        public int Right { get; private set; }
        public int Result { get; set; }
        public override string ToString()
        {
            switch (Op)
            {
                case Operator.Add: return $"{(char) ('A' + Left)} + {(char) ('A' + Right)} = {Result}";
                case Operator.Subtract: return $"{(char) ('A' + Left)} - {(char) ('A' + Right)} = {Result}";
                case Operator.Multiply: return $"{(char) ('A' + Left)} * {(char) ('A' + Right)} = {Result}";
                case Operator.Divide: return $"{(char) ('A' + Left)} / {(char) ('A' + Right)} = {Result}";
                default: return $"{(char) ('A' + Left)} || {(char) ('A' + Right)} = {Result}";
            }
        }
        public Constraint Constraint
        {
            get
            {
                switch (Op)
                {
                    case Operator.Add: return new TwoCellLambdaConstraint(Left, Right, (a, b) => a + b == Result);
                    case Operator.Subtract: return new TwoCellLambdaConstraint(Left, Right, (a, b) => a - b == Result);
                    case Operator.Multiply: return new TwoCellLambdaConstraint(Left, Right, (a, b) => a * b == Result);
                    case Operator.Divide: return new TwoCellLambdaConstraint(Left, Right, (a, b) => a == b * Result);
                    default: return new TwoCellLambdaConstraint(Left, Right, (a, b) => a * 10 + b == Result);
                }
            }
        }

        public Equation(int left, Operator op, int right, int result)
        {
            Left = left;
            Op = op;
            Right = right;
            Result = result;
        }
    }
}