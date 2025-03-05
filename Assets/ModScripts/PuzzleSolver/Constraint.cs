using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    /// <summary>Abstract base class for all constraints in a puzzle.</summary>
    abstract class Constraint
    {
        /// <summary>The group of cells affected by this constraint, or <c>null</c> if it affects all of them.</summary>
        public int[] AffectedCells { get; private set; }

        /// <summary>
        ///     Constructor for derived types.</summary>
        /// <param name="affectedCells">
        ///     The set of cells affected by this constraint.</param>
        /// <param name="color">
        ///     See <see cref="CellColor"/>.</param>
        /// <param name="backgroundColor">
        ///     See <see cref="CellBackgroundColor"/>.</param>
        protected Constraint(IEnumerable<int> affectedCells)
        {
            AffectedCells = affectedCells == null ? null : affectedCells.ToArray();
        }

        /// <summary>
        ///     Constraint implementations must modify <paramref name="takens"/> to mark values as taken that are known to be
        ///     impossible given the specified incomplete grid.</summary>
        /// <param name="takens">
        ///     The array to be modified. The first dimension equates to the cells in the puzzle. The second dimension equates
        ///     to the possible values for the cell, indexed from 0. In a standard Sudoku, indexes 0 to 8 are used for the
        ///     numbers 1 to 9. Only set values to <c>true</c> that are now impossible to satisfy; implementations must not
        ///     change other values back to <c>false</c>.</param>
        /// <param name="grid">
        ///     The incomplete grid at the current point during the algorithm. Implementations must not modify this array. In
        ///     order to communicate that a cell must have a specific value, mark all other possible values on that cell as
        ///     taken in the <paramref name="takens"/> array.</param>
        /// <param name="ix">
        ///     If <c>null</c>, this method was called either at the very start of the algorithm or because this constraint
        ///     was returned from another constraint. In such a case, the method must examine all filled-in values in the
        ///     provided grid. Otherwise, specifies which value has just been placed and allows the method to update <paramref
        ///     name="takens"/> based only on the value in that square.</param>
        /// <param name="minValue">
        ///     The minimum value that squares can have in this puzzle. For standard Sudoku, this is 1. This is also the
        ///     difference between the real-life values in the grid and the indexes used in the <paramref name="takens"/>
        ///     array.</param>
        /// <param name="maxValue">
        ///     The maximum value that squares can have in this puzzle. For standard Sudoku, this is 9.</param>
        /// <returns>
        ///     <para>
        ///         Implementations must return <c>null</c> if the constraint remains valid for the remainder of filling this
        ///         grid, or a collection of constraints that this constraint shall be replaced with (can be empty).</para>
        ///     <para>
        ///         For example, in <see cref="EqualSumsConstraint"/>, since the sum is not initially known, the constraint
        ///         waits until one of its regions is filled and then uses this return value to replace itself with several
        ///         <see cref="SumConstraint"/>s to ensure the other regions have the same sum.</para>
        ///     <para>
        ///         The algorithm will automatically call this method again on all the new constraints for all cells already
        ///         placed in the grid. The constraints returned MUST NOT themselves return yet more constraints at that
        ///         point.</para></returns>
        public abstract IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue);
    }
}
