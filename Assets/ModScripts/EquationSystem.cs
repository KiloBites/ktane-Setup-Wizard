using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class EquationSystem
{
    public int[] GeneratedPassword()
    {
        var numbs = new int[6];
        do
        {
            for (int i = 0; i < numbs.Length; i++)
                numbs[i] = Range(0, 10);
        }
        while (Enumerable.Range(0, 10).Any(a => numbs.Count(b => b == a) >= 3) || numbs.Count(a => a == 0) > 1);
        return numbs;
    }

    public int Equation(int ix, int a, int b)
    {
        switch (ix)
        {
            case 0:
                return a + b;
            case 1:
                return a - b;
            case 2:
                return a * b;
            case 3:
                return a / b;
            case 4:
                return int.Parse($"{a}{b}");
        }

        return -1;
    }

    public bool IsSetValid(int[] password, int[] randomIxes, out WizardExpression[] generatedPuzzle)
    {
        try
        {
            generatedPuzzle = GeneratedPuzzle(password, randomIxes);
            return true;
        }
        catch (Exception)
        {
            generatedPuzzle = null;
            return false;
        }
    }

    public WizardExpression[] GeneratedPuzzle(int[] password, int[] randomIxes)
    {
        int tries = 0;

    tryagain:

        var expressions = new WizardExpression[6];
		
		int[] listA = Enumerable.Range(0, 6).ToList().Shuffle().ToArray();
		int[] listB = listA.ToList().Shuffle().ToArray();
		for (int x = 0; x < 6; x++)
			if (listA[x] == listB[x])
				goto tryagain;

        var allPrev = new List<int[]>();

        for (int i = 0; i < 6; i++)
        {

            var letters = "ABCDEF";

            while (true)
            {

                if (tries >= 1000) // If at any point it cannot generate valid equations, the set used is completely busted, and it will have to start all over again.
                    throw new Exception();
                    

                var ixesToCheck = new[] { randomIxes[i], listA[i], listB[i] };
                /* A slew of checks to determine if the expression generated:
                * - uses the exact same combination of variables and operations as its previous
                * - shares the same value as another value.
                * - follows special rules denoting the operations.
                */
                if (CheckCase(randomIxes[i], password[listA[i]], password[listB[i]]) &&
                    (expressions.Count() == 0 ||
                    !allPrev.Any(a => ixesToCheck.SequenceEqual(a) ||
                    (ixesToCheck[0] == a[0] && ixesToCheck[1] == a[2] && ixesToCheck[2] == a[1]) ||
                    (Equation(ixesToCheck[0], password[ixesToCheck[1]], password[ixesToCheck[2]]) == Equation(a[0], password[a[1]], password[a[2]])))
                    ))
                {
                    allPrev.Add(ixesToCheck);
                    break;
                }
                tries++;
            }

            expressions[i] = new WizardExpression(letters[listA[i]], "+,-,*,/,||".Split(',')[randomIxes[i]], letters[listB[i]], Equation(randomIxes[i], password[listA[i]], password[listB[i]]));

        }

        if ("ABCDEF".Any(x => expressions.All(y => x != y.NumIxA && x != y.NumIxB)))
            goto tryagain;

             
        return expressions;
    }

    private bool CheckCase(int ix, int a, int b)
    {
        switch (ix)
        {
            case 0:
                return Enumerable.Range(0, 18).Any(x => (a + b) == x);
            case 1:
                return Enumerable.Range(-9, 9).Any(x => (a - b) == x);
            case 2:
            case 3:
                if (new[] { a, b }.Any(x => x == 0))
                    return false;

                return a % b == 0;
            case 4:
                return Enumerable.Range(0, 99).Any(x => int.Parse($"{a}{b}") == x);

        }

        return false;
    }

 

}

public class WizardExpression
{
    public char NumIxA { get; private set; }
    public string EquationExpression { get; private set; }
    public char NumIxB { get; private set; }
    public int Answer { get; private set; }

    public WizardExpression(char numIxA, string equationExpression, char numIxB, int answer)
    {
        NumIxA = numIxA;
        EquationExpression = equationExpression;
        NumIxB = numIxB;
        Answer = answer;
    }


}