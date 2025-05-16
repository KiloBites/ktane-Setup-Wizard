using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using PuzzleSolvers;
using SetupWizard;
using UnityEngine;
using static UnityEngine.Debug;
using static UnityEngine.Random;

public class SetupWizardScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable[] mainButtons, keyboardLetters, keyboardNumbers, folderButtons, accountPrompts, page2Buttons;
    public KMSelectable backSpace, shift, reset, submit;

    public TextMesh[] mainTexts, expressionDisplays;
    public TextMesh windowText;

    public GameObject window;
    public GameObject[] pages;

    public Material[] blackScreens, backgrounds, windowIcons;
    public MeshRenderer windowIcon, screen;

    static int moduleIdCounter = 1, setupWizardIdCounter = 1;
    int moduleId, setupWizardId;
    private bool moduleSolved, isActivated, moduleSelected, canTypeUser, canTypePassword, shiftedLetters = true, canSubmit, canSolve;

    private readonly Folder[] folders =
    {
        new Folder("Downloads", new int[] { 1, 4 }),
        new Folder("Pictures", new int[] { 3, 5 }),
        new Folder("Documents", new int[] { 0, 1 }),
        new Folder("Music", new int[] { 0, 5 }),
        new Folder("Homework", new int[] { 2, 3 }),
        new Folder("Videos", new int[] { 2, 4 })
    };

    private Folder startingFolder;
    private Folder[] shuffledFolders;

    private ObtainUsername username;
    private string obtainedUsername;

    private Equation[] equations = null;

    private int currentPos, currentPage = 0, page2Ix = 0;

    private string usernameInput = string.Empty, passwordInput = string.Empty, passwordAsterisk = string.Empty;

    private int[]  password;
    private string finalPass;

    private string Shift(string s, int count) => s.Substring(s.Length - count) + s.Substring(0, s.Length - count);

    private Folder[] SwapFolders(int[] swaps)
    {
        var foldersSwapped = new Folder[2];

        var foldersToSwap = swaps.Select(x => folders[x].FolderName).ToArray();
        var foldersDirectories = swaps.Select(x => folders[x].Directories).ToArray();

        for (int i = 0; i < 2; i++)
            foldersSwapped[i] = new Folder(foldersToSwap[i == 0 ? 1 : 0], foldersDirectories[i]);

        return foldersSwapped.ToArray();
    }

    private int GetLetterIndex(char c) => "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);

    void Awake()
    {
        moduleId = moduleIdCounter++;
        setupWizardId = setupWizardIdCounter++;

        foreach (KMSelectable letter in keyboardLetters)
            letter.OnInteract += delegate () { KeyboardLetterPress(letter); return false; };

        foreach (KMSelectable number in keyboardNumbers)
            number.OnInteract += delegate () { KeyboardNumberPress(number); return false; };

        foreach (KMSelectable folder in folderButtons)
            folder.OnInteract += delegate () { FolderPress(folder); return false; };

        foreach (KMSelectable mainButton in mainButtons)
            mainButton.OnInteract += delegate () { MainButtonPress(mainButton); return false; };

        foreach (KMSelectable prompt in accountPrompts)
            prompt.OnInteract += delegate () { AccountPromptPress(prompt); return false; };

        foreach (KMSelectable pg in page2Buttons)
            pg.OnInteract += delegate () { Page2Press(pg); return false; };

        reset.OnInteract += delegate () { StopAllCoroutines(); StartCoroutine(ResetPress()); return false; };
        backSpace.OnInteract += delegate () { BackSpacePress(); return false; };
        shift.OnInteract += delegate () { ShiftPress(); return false; };
        submit.OnInteract += delegate () { SubmitPress(); return false; };

        Module.OnActivate += delegate () { StopAllCoroutines(); StartCoroutine(Startup()); };
        Module.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };
    }

    void Start()
    {
        foreach (var obj in pages)
            obj.SetActive(false);

        submit.gameObject.SetActive(false);

        window.SetActive(false);

        StartCoroutine(Initialize());

        shuffledFolders = folders.ToArray();

        var folderSwapA = Range(0, folders.Length);
        var folderSwapB = Enumerable.Range(0, folders.Length).Where(x => x != folderSwapA).PickRandom();

        var foldersToShuffle = new[] { folderSwapA, folderSwapB };

        currentPos = Range(0, folders.Length);

        startingFolder = folders[currentPos];
        username = new ObtainUsername(foldersToShuffle.Select(x => folders[x]).OrderBy(x => x.FolderName).ToArray(), folders, startingFolder);
        obtainedUsername = username.GetUsername("SETUPWIZARD".Contains(Bomb.GetSerialNumberLetters().First()), "COMPUTERLAB".Contains(Bomb.GetSerialNumberLetters().Last()));

        Log($"[Setup Wizard #{moduleId}] The starting folder for step 1 is: {startingFolder.FolderName}");
        Log($"[Setup Wizard #{moduleId}] The swapped folders for step 1 were: {foldersToShuffle.Select(x => folders[x].FolderName).Join(", ")}");
        var firstLetterResult = "SETUPWIZARD".Contains(Bomb.GetSerialNumberLetters().First()) ? "The first letter of the serial number contains a letter in \"SETUPWIZARD\". Use the first swapped folder in alphabetical order" : "The first letter of the serial number doesn't contain a letter in \"SETUPWIZARD\". Use the second swapped folder in alphabetical order.";
        Log($"[Setup Wizard #{moduleId}] {firstLetterResult}");
        var lastLetterResult = "COMPUTERLAB".Contains(Bomb.GetSerialNumberLetters().Last()) ? "The last letter of the serial number contains a letter in \"COMPUTERLAB\". Use the swapped folders for the rows and the starting folder for the columns." : "The last letter of the serial number doesn't contain a letter in \"COMPUTERLAB\". Use the swapped folders for the columns and the starting folder for the rows.";
        Log($"[Setup Wizard #{moduleId}] {lastLetterResult}");
        Log($"[Setup Wizard #{moduleId}] The username should be {obtainedUsername}");

        var swappedFolders = SwapFolders(foldersToShuffle).ToArray();

        for (int i = 0; i < 2; i++)
            shuffledFolders[foldersToShuffle[i]].FolderName = swappedFolders[i].FolderName;

        int swEq1, swEq2;
        GeneratePasswordPuzzle(out swEq1, out swEq2);
        Log($"[Setup Wizard #{moduleId}] The password unmodified is: {password.Join("")}");
        Log($"[Setup Wizard #{moduleId}] *====================*");
        foreach (var eq in equations)
            Log($"[Setup Wizard #{moduleId}] {eq}");
        Log($"[Setup Wizard #{moduleId}] *====================*");
        Log($"[Setup Wizard #{moduleId}] The results of equations #{swEq1 + 1} and #{swEq2 + 1} have been swapped.");
        Log($"[Setup Wizard #{moduleId}] The final password after shifting is: {finalPass}");
    }

    public void GeneratePasswordPuzzle(out int swEq1, out int swEq2)
    {
        const int numDigits = 6;
        const int numEqs = 6;

        tryAgain:
        password = Enumerable.Range(0, numDigits).Select(i => Range(0, 10)).ToArray();
        var allEquations = new List<Equation>();

        for (var left = 0; left < numDigits; left++)
            for (var right = 0; right < numDigits; right++)
            {
                // Commutative operators
                if (right > left)
                {
                    allEquations.Add(new Equation(left, Operator.Add, right, password[left] + password[right]));
                    allEquations.Add(new Equation(left, Operator.Multiply, right, password[left] * password[right]));
                }
                // Non-commutative operators
                if (right != left)
                {
                    allEquations.Add(new Equation(left, Operator.Subtract, right, password[left] - password[right]));
                    if (password[right] != 0 && password[left] % password[right] == 0)
                        allEquations.Add(new Equation(left, Operator.Divide, right, password[left] / password[right]));
                    allEquations.Add(new Equation(left, Operator.Concatenate, right, password[left] * 10 + password[right]));
                }
            }

        // Pick 6 equations with different variables and different results
        var generatedEquations = new List<Equation>();
        while (generatedEquations.Count < numEqs)
        {
            if (allEquations.Count == 0)
                goto tryAgain;
            var pick = Range(0, allEquations.Count);
            var newEq = allEquations[pick];
            generatedEquations.Add(newEq);
            allEquations.RemoveAll(eq => (eq.Left == newEq.Left && eq.Right == newEq.Right) || (eq.Left == newEq.Right && eq.Right == newEq.Left) || eq.Result == newEq.Result);
        }

        // Make sure that the puzzle is unique at this point
        if (new Puzzle(6, 0, 9, generatedEquations.Select<Equation, Constraint>(eq => eq.Constraint)).Solve().Skip(1).Any())
            goto tryAgain;

        // Swap the results of two equations
        swEq1 = Range(0, numEqs);
        swEq2 = Range(0, numEqs - 1);
        if (swEq2 >= swEq1)
            swEq2++;
        var swap = new Action<int, int>((int eqIx1, int eqIx2) =>
        {
            var t = generatedEquations[eqIx1].Result;
            generatedEquations[eqIx1].Result = generatedEquations[eqIx2].Result;
            generatedEquations[eqIx2].Result = t;
        });
        swap(swEq1, swEq2);

        // Make sure that the puzzle is still unique (no other swaps produce a solution)
        for (var eq1 = 0; eq1 < numEqs; eq1++)
            for (var eq2 = eq1 + 1; eq2 < numEqs; eq2++)
                if (eq1 != swEq1 || eq2 != swEq2)
                {
                    swap(eq1, eq2);
                    if (new Puzzle(6, 0, 9, generatedEquations.Select<Equation, Constraint>(eq => eq.Constraint)).Solve().Any())
                        goto tryAgain;
                    swap(eq1, eq2);
                }

        equations = generatedEquations.ToArray();

        var x = Math.Max(generatedEquations[swEq1].Result, generatedEquations[swEq2].Result);
        var y = Math.Min(generatedEquations[swEq1].Result, generatedEquations[swEq2].Result);
        var op = (Operator)(Bomb.GetSerialNumberNumbers().Last() % 5);
        int z = 0;


        try
        {
            switch (op)
            {
                case Operator.Add:
                    z = x + y;
                    break;
                case Operator.Subtract:
                    z = x - y;
                    break;
                case Operator.Multiply:
                    z = x * y;
                    break;
                case Operator.Divide:
                    z = x / y;
                    break;
                case Operator.Concatenate:
                    z = 10 * x + y;
                    break;
            }
        }
        catch (DivideByZeroException)
        {
            goto tryAgain;
        }

        finalPass = Shift(password.Join(""), Math.Abs(z) % password.Length);



    }

    private void OnDestroy()
    {
        setupWizardIdCounter = 1;
    }

    void AccountPromptPress(KMSelectable prompt)
    {
        prompt.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("Click", transform);

        if (moduleSolved || !isActivated || !canSubmit)
            return;

        switch (Array.IndexOf(accountPrompts, prompt))
        {
            case 0:
                canTypePassword = false;

                canTypeUser = true;
                break;
            case 1:
                canTypeUser = false;

                canTypePassword = true;
                break;
        }
    }

    void MainButtonPress(KMSelectable button)
    {
        button.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("Click", transform);

        if (moduleSolved || !isActivated)
            return;

        switch (Array.IndexOf(mainButtons, button))
        {
            case 0:
                switch (currentPage)
                {
                    case 0:
                        return;
                    case 3:
                        canSubmit = false;
                        goto default;
                    default:
                        currentPage--;
                        break;
                }
                break;
            case 1:
                switch (currentPage)
                {
                    default:
                        currentPage++;
                        break;
                    case 0:
                        FolderUpdate();
                        goto default;
                    case 1:
                        Page2Update();
                        goto default;
                    case 2:
                        canSubmit = true;
                        goto default;
                    case 3:
                        if (usernameInput == obtainedUsername && passwordInput == finalPass)
                        {
                            Log($"[Setup Wizard #{moduleId}] The username and password are correct. The setup is now finished and the application is running.");
                            currentPage++;
                            canSolve = true;
                            canSubmit = false;
                        }
                        else
                        {
                            var result = new List<string>();

                            if (usernameInput != obtainedUsername)
                                result.Add(usernameInput.Length > 0 ? $"(Expected username is {obtainedUsername}, but inputted {usernameInput})" : "(Username input is empty)");

                            if (passwordInput != finalPass)
                                result.Add(passwordInput.Length > 0 ? $"(Expected password is {finalPass}, but inputted {passwordInput})" : "(Password input is empty)");

                            Log($"[Setup Wizard #{moduleId}] Either the username or password is invalid {result.Join()}. Strike!");
                            Module.HandleStrike();
                        }
                        break;
                    case 4:
                        return;
                }
                break;
        }

        WindowUpdate();
    }

    void WindowUpdate()
    {
        mainButtons[1].GetComponentInChildren<TextMesh>().text = currentPage == 3 ? "Finish" : "Next >";

        Color32[] grayed = { new Color32(0, 0, 0, 60), Color.black };

        mainButtons[0].GetComponentInChildren<TextMesh>().color = currentPage == 0 ? grayed[0] : grayed[1];

        if (currentPage == 4)
        {
            foreach (KMSelectable mainButton in mainButtons)
                mainButton.gameObject.SetActive(false);
            reset.gameObject.SetActive(false);

            foreach (var obj in pages)
                obj.SetActive(false);

            windowIcon.material = windowIcons[1];
            submit.gameObject.SetActive(true);
            windowText.text = "Button Defuser";

            return;
        }



        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == currentPage);
    }

    void FolderPress(KMSelectable folder)
    {
        folder.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("Click", transform);

        if (moduleSolved || !isActivated || currentPage != 1)
            return;

        var ix = Array.IndexOf(folderButtons, folder);

        currentPos = shuffledFolders[currentPos].Directories[ix];

        FolderUpdate();
    }

    void FolderUpdate()
    {

        var folderNames = shuffledFolders[currentPos].Directories.Select(x => folders[x].FolderName).ToArray();

        for (int i = 0; i < 2; i++)
            folderButtons[i].GetComponentInChildren<TextMesh>().text = folderNames[i];
    }

    void Page2Update()
    {
        for (int i = 0; i < 4; i++)
        {
            var ix = i + page2Ix;
            expressionDisplays[i].text = equations[ix].ToString();
        }
    }

    void Page2Press(KMSelectable pg)
    {
        pg.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("Click", transform);

        if (moduleSolved || !isActivated)
            return;

        switch (Array.IndexOf(page2Buttons, pg))
        {
            case 0:
                if (page2Ix == 0)
                    return;
                page2Ix--;
                Page2Update();
                break;
            case 1:
                if (page2Ix == 2)
                    return;
                page2Ix++;
                Page2Update();
                break;
        }
    }


    void KeyboardLetterPress(KMSelectable letter)
    {
        letter.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("KeyPress", transform);

        if (moduleSolved || !isActivated || !canSubmit && !canTypeUser)
            return;

        if (usernameInput.Length < obtainedUsername.Length)
        {
            usernameInput += letter.GetComponentInChildren<TextMesh>().text;
            accountPrompts[0].GetComponentInChildren<TextMesh>().text = usernameInput;
        }

    }

    void KeyboardNumberPress(KMSelectable number)
    {
        number.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("KeyPress", transform);

        if (moduleSolved || !isActivated || !canSubmit && !canTypePassword)
            return;

        if (passwordInput.Length < 6)
        {
            passwordInput += number.GetComponentInChildren<TextMesh>().text;
            passwordAsterisk += '*';
            accountPrompts[1].GetComponentInChildren<TextMesh>().text = passwordAsterisk;
        }

    }

    void ShiftPress()
    {
        shift.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("KeyPress", transform);

        if (moduleSolved || !isActivated || !canSubmit)
            return;

        shiftedLetters = !shiftedLetters;

        var letters = "QWERTYUIOPASDFGHJKLZXCVBNM";

        for (int i = 0; i < 26; i++)
            keyboardLetters[i].GetComponentInChildren<TextMesh>().text = shiftedLetters ? letters[i].ToString().ToUpperInvariant() : letters[i].ToString().ToLowerInvariant();
    }

    void SubmitPress()
    {
        submit.AddInteractionPunch(0.4f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        if (moduleSolved || !isActivated || !canSolve)
            return;

        Audio.PlaySoundAtTransform("Solve", transform);

        Log($"[Setup Wizard #{moduleSolved}] The button has been pressed. Solved!");

        moduleSolved = true;
        Module.HandlePass();
    }

    IEnumerator ResetPress()
    {
        reset.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("Click", transform);

        if (moduleSolved || !isActivated || currentPage == 0)
            yield break;

        currentPage = 0;
        currentPos = Array.IndexOf(folders, startingFolder);
        window.SetActive(false);
        isActivated = false;
        WindowUpdate();

        yield return new WaitForSeconds(1);

        Audio.PlaySoundAtTransform("Reset", transform);
        isActivated = true;
        window.SetActive(true);
    }

    void BackSpacePress()
    {
        backSpace.AddInteractionPunch(0.4f);
        Audio.PlaySoundAtTransform("KeyPress", transform);

        if (moduleSolved || !isActivated || !canSubmit)
            return;

        if (canTypeUser)
            if (usernameInput.Length > 0)
            {
                usernameInput = usernameInput.Remove(usernameInput.Length - 1);
                accountPrompts[0].GetComponentInChildren<TextMesh>().text = usernameInput;
            }

        if (canTypePassword)
            if (passwordInput.Length > 0 && passwordAsterisk.Length > 0)
            {
                passwordInput = passwordInput.Remove(passwordInput.Length - 1);
                passwordAsterisk = passwordAsterisk.Remove(passwordAsterisk.Length - 1);
                accountPrompts[1].GetComponentInChildren<TextMesh>().text = passwordAsterisk;
            }
    }

    IEnumerator Startup()
    {
        if (setupWizardId == 1)
            Audio.PlaySoundAtTransform("Window Setup", transform);

        screen.material = backgrounds.PickRandom();

        yield return new WaitForSeconds(1);

        WindowUpdate();
        window.SetActive(true);
        pages[0].SetActive(true);
        isActivated = true;

    }

    IEnumerator Initialize()
    {
        yield return new WaitForSeconds(0.5f);
        screen.material = blackScreens[1];

    }

    void Update()
    {
        if (moduleSolved || !isActivated)
            return;

        if (moduleSelected)
        {
            if (canTypeUser)
            {
                for (int ltr = 0; ltr < 26; ltr++)
                    if (Input.GetKeyDown(((char) ('a' + ltr)).ToString()))
                    {
                        keyboardLetters[GetLetterIndex((char) ('A' + ltr))].OnInteract();
                        return;
                    }
            }
            else if (canTypePassword)
            {
                var validNumPresses = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };

                for (int num = 0; num < validNumPresses.Length; num++)
                    if (Input.GetKeyDown(validNumPresses[num]))
                    {
                        keyboardNumbers[num].OnInteract();
                        return;
                    }
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
                backSpace.OnInteract();

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                shift.OnInteract();
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} back/next goes to the previous/next page. || !{0} reset resets back to the original state. || !{0} folder 1/2 navigates to said folder. || !{0} folders outputs what folders are currently present. || !{0} page up/down to navigate through the system of equations. || !{0} username [input] types down the username you want to input. || !{0} password [input] types down the password you want to input. || !{0} done solves the module once the green defuse button appears.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        yield return null;

        if (!isActivated)
        {
            yield return "sendtochaterror The module isn't activated yet!";
            yield break;
        }

        if ("BACK".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            if (currentPage == 0)
            {
                yield return "sendtochaterror You cannot go back any further!";
                yield break;
            }

            if (currentPage == 4)
            {
                yield return "sendtochaterror You cannot go back since the setup wizard is finished!";
                yield break;
            }

            mainButtons[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if ("RESET".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            if (currentPage == 0)
            {
                yield return "sendtochaterror You have already resetted!";
                yield break;
            }

            reset.OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if ("NEXT".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            if (currentPage == 4)
            {
                yield return "sendtochaterror You cannot go any further!";
                yield break;
            }

            mainButtons[1].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if ("FOLDER".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 1)
            {
                yield return "sendtochaterror You are not at step 1!";
                yield break;
            }

            if (split.Length == 1)
            {
                yield return "sendtochaterror Please follow it up with either 1 or 2!";
                yield break;
            }

            if (split[1].Length > 1 || split.Length > 2)
                yield break;

            if (!"12".Contains(split[1]))
            {
                yield return $"sendtochaterror {split[1]} is not 1 or 2!";
                yield break;
            }

            folderButtons[int.Parse(split[1]) - 1].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if ("FOLDERS".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 1)
            {
                yield return "sendtochaterror You are not at step 1!";
                yield break;
            }

            if (split.Length > 1)
                yield break;

            var folderNames = new List<string>();

            foreach (KMSelectable folder in folderButtons)
                folderNames.Add(folder.GetComponentInChildren<TextMesh>().text);

            yield return $"sendtochat The current folders present are: {folderNames.Join(", ")}";
            yield break;
        }

        if ("PAGE".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 2)
            {
                yield return "sendtochaterror You are not at step 2!";
                yield break;
            }

            if (split.Length == 1)
            {
                yield return "sendtochaterror Please specify whether you want to go up or down!";
                yield break;
            }

            if (split.Length > 2)
                yield break;

            var validPg = new[] { "UP", "DOWN" };


            if (!validPg.Contains(split[1].ToUpperInvariant()))
            {
                yield return $"sendtochaterror {split[1]} is not valid!";
                yield break;
            }

            var press = Array.IndexOf(validPg, split[1].ToUpperInvariant());

            if (press == 0 && page2Ix == 0)
            {
                yield return "sendtochaterror You cannot go further up!";
                yield break;
            }

            if (press == 1 && page2Ix == 2)
            {
                yield return "sendtochaterror You cannot go further down!";
                yield break;
            }


            page2Buttons[press].OnInteract();
            yield return new WaitForSeconds(0.1f);

            yield break;
        }

        if ("USERNAME".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 3)
            {
                yield return "sendtochaterror You are not at step 3!";
                yield break;
            }

            if (split.Length == 1)
            {
                yield return "sendtochaterror Please input your username!";
                yield break;
            }

            if (split[1].Length > obtainedUsername.Length)
                yield break;

            if (!split[1].All(x => char.IsLetter(x)))
            {
                yield return "sendtochaterror Make sure the username you're inputting doesn't contain numbers!";
                yield break;
            }

            if (!canTypeUser)
            {
                accountPrompts[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            while (usernameInput.Length != 0)
            {
                backSpace.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            foreach (var letter in split[1])
            {
                if ((char.IsUpper(letter) && !shiftedLetters) || (char.IsLower(letter) && shiftedLetters))
                {
                    shift.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }

                keyboardLetters[GetLetterIndex(char.ToUpperInvariant(letter))].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            yield break;
        }

        if ("PASSWORD".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 3)
            {
                yield return "sendtochaterror You are not at step 3!";
                yield break;
            }

            if (split.Length == 1)
            {
                yield return "sendtochaterror Please input your password!";
                yield break;
            }

            if (split[1].Length > 6)
                yield break;

            if (!split[1].All(x => char.IsNumber(x)))
            {
                yield return "sendtochaterror Your password input contains letters rather than all digits!";
                yield break;
            }

            if (!canTypePassword)
            {
                accountPrompts[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            while (passwordInput.Length != 0)
            {
                backSpace.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            foreach (var num in split[1].Select(x => "1234567890".IndexOf(x)).ToArray())
            {
                keyboardNumbers[num].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            yield break;
        }

        if ("DONE".ContainsIgnoreCase(split[0]))
        {
            if (currentPage != 4)
            {
                yield return "sendtochaterror The module is not installed yet!";
                yield break;
            }

            if (split.Length > 1)
                yield break;

            submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;

        while (!isActivated)
            yield return true;

        if (currentPage == 4)
            goto solved;

        while (currentPage != 3)
        {
            mainButtons[1].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        var usernameToType = obtainedUsername;

        if (usernameInput != usernameToType)
        {
            if (!canTypeUser)
            {
                accountPrompts[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            while (!usernameToType.StartsWith(usernameInput))
            {
                backSpace.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            for (int i = usernameInput.Length; i < usernameToType.Length; i++)
            {
                if ((char.IsUpper(usernameToType[i]) && !shiftedLetters) || (char.IsLower(usernameToType[i]) && shiftedLetters))
                {
                    shift.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }

                keyboardLetters[GetLetterIndex(char.ToUpperInvariant(usernameToType[i]))].OnInteract();

                yield return new WaitForSeconds(0.1f);
            }
        }

        if (passwordInput != finalPass)
        {
            if (!canTypePassword)
            {
                accountPrompts[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            while (!finalPass.StartsWith(passwordInput))
            {
                backSpace.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            var digitsToPress = finalPass.Select(x => "1234567890".IndexOf(x)).ToArray();

            for (int i = passwordInput.Length; i < finalPass.Length; i++)
            {
                keyboardNumbers[digitsToPress[i]].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }

        mainButtons[1].OnInteract();
        yield return new WaitForSeconds(0.1f);

        solved:;

        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }
}





