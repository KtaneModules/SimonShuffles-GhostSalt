using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class SimonShufflesScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colourblind;
    public TextMesh[] ColourblindTexts;
    public KMSelectable[] Buttons;
    public MeshRenderer[] ButtonMats;
    public Material[] Mats;

    private Coroutine[] RunningCoroutines = new Coroutine[11];
    private List<int> FlashingSequence = new List<int>();
    private List<int> Inputs = new List<int>();
    private readonly int[,] Table = new int[,] {
        { 7, 5, 6, 1, 3, 2, 4, 0, 8 },
        { 8, 2, 3, 6, 0, 4, 5, 1, 7 },
        { 4, 1, 0, 5, 8, 7, 6, 3, 2 },
        { 1, 6, 8, 4, 2, 5, 0, 7, 3 },
        { 2, 3, 5, 7, 6, 0, 1, 8, 4 },
        { 0, 7, 4, 8, 1, 3, 2, 6, 5 },
        { 5, 8, 1, 3, 4, 6, 7, 2, 0 },
        { 3, 0, 7, 2, 5, 1, 8, 4, 6 },
        { 6, 4, 2, 0, 7, 8, 3, 5, 1 }
    };
    private int[] ButtonColours = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
    private int[] ReferredButtons = new int[9];
    private int Presses;
    private int Stage;
    private readonly string[] ColourNames = { "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Magenta", "White" };
    private bool[] Running = new bool[11];
    private bool ColourblindEnabled;
    private bool Pressed;
    private bool Inputting;
    private bool Active;
    private bool CannotPress;
    private bool Solved;
    private readonly Color[] Colours = { new Color(1, 0, 0), new Color(1, 0.5f, 0), new Color(1, 1, 0), new Color(0, 1, 0), new Color(0, 1, 1), new Color(0, 0, 1), new Color(0.5f, 0, 1), new Color(1, 0, 1), new Color(1, 1, 1) };

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        ButtonColours.Shuffle();
        ColourblindEnabled = Colourblind.ColorblindModeActive;
        if (ColourblindEnabled)
            for (int i = 0; i < 9; i++)
                ColourblindTexts[i].text = ColourNames[ButtonColours[i]][0].ToString();
        else
            for (int i = 0; i < 9; i++)
                ColourblindTexts[i].text = "";
        for (int i = 0; i < 9; i++)
        {
            ButtonMats[i].material = Mats[0];
            ButtonMats[i].material.color = Colours[ButtonColours[i]];
        }
        for (int i = 0; i < 9; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { if (!CannotPress && !Solved) StartCoroutine(ButtonPress(x)); return false; };
        }
        for (int i = 0; i < 3; i++)
            FlashingSequence.Add(Rnd.Range(0, 9));
        Calculate();
        Module.OnActivate += delegate { RunningCoroutines[10] = StartCoroutine(StartFlashing()); Running[10] = true; Active = true; };
    }

    void Calculate()
    {
        for (int i = 0; i < 9; i++)
            ReferredButtons[i] = -1;
        for (int i = 0; i < 9; i++)
        {
            int Cache = Table[ButtonColours[i], i];
            while (ReferredButtons.Contains(Cache))
                Cache = (Cache + 1) % 9;
            ReferredButtons[i] = Cache;
        }
        Debug.LogFormat("[Simon Shuffles #{0}] In stage {1}, the flashing sequence is: {2}.", _moduleID, Stage + 1, FlashingSequence.Select(x => ColourNames[x]).Join(", "));
        Debug.LogFormat("[Simon Shuffles #{0}] The buttons, in reading order, refer to: {1}.", _moduleID, ReferredButtons.Select(x => ColourNames[x]).Join(", "));
        Debug.LogFormat("[Simon Shuffles #{0}] The correct buttons to press are: {1}.", _moduleID, FlashingSequence.Select(x => ColourNames[ButtonColours[Array.IndexOf(ReferredButtons, x)]]).Join(", "));
    }

    void NextStage()
    {
        Stage++;
        ButtonColours.Shuffle();
        if (ColourblindEnabled)
            for (int i = 0; i < 9; i++)
                ColourblindTexts[i].text = ColourNames[ButtonColours[i]][0].ToString();
        else
            for (int i = 0; i < 9; i++)
                ColourblindTexts[i].text = "";
        for (int i = 0; i < 9; i++)
            ButtonMats[i].material = Mats[0];
        FlashingSequence.Add(Rnd.Range(0, 9));
        Calculate();
    }

    private IEnumerator FadeToGrey()
    {
        Presses = 0;
        yield return new WaitForSecondsRealtime(0.5f);
        Audio.PlaySoundAtTransform("swoosh", Buttons[4].transform);
        float Timer = 0f;
        while (Timer < 0.5f)
        {
            Timer += Time.deltaTime;
            for (int i = 0; i < 9; i++)
            {
                ButtonMats[i].material.color = new Color(Mathf.Lerp(Colours[ButtonColours[i]].r, 0.5f, Timer / 0.5f), Mathf.Lerp(Colours[ButtonColours[i]].g, 0.5f, Timer / 0.5f), Mathf.Lerp(Colours[ButtonColours[i]].b, 0.5f, Timer / 0.5f));
                ColourblindTexts[i].color = new Color(0, 0, 0, Mathf.Lerp(0.75f, 0, Timer / 0.5f));
            }
            yield return null;
        }
        if (!Solved)
        {
            Presses = 0;
            NextStage();
            while (Timer > 0)
            {
                Timer -= Time.deltaTime;
                for (int i = 0; i < 9; i++)
                {
                    ButtonMats[i].material.color = new Color(Mathf.Lerp(Colours[ButtonColours[i]].r, 0.5f, Timer / 0.5f), Mathf.Lerp(Colours[ButtonColours[i]].g, 0.5f, Timer / 0.5f), Mathf.Lerp(Colours[ButtonColours[i]].b, 0.5f, Timer / 0.5f));
                    ColourblindTexts[i].color = new Color(0, 0, 0, Mathf.Lerp(0.75f, 0, Timer / 0.5f));
                }
                yield return null;
            }
            CannotPress = false;
        }
    }

    private IEnumerator StartFlashing()
    {
        while (!Solved)
        {
            for (int i = 0; i < FlashingSequence.Count() && !Inputting; i++)
            {
                if (Pressed)
                    Audio.PlaySoundAtTransform("beep" + (FlashingSequence[i] + 1), Buttons[Array.IndexOf(ButtonColours, FlashingSequence[i])].transform);
                ButtonMats[Array.IndexOf(ButtonColours, FlashingSequence[i])].material = Mats[1];
                ButtonMats[Array.IndexOf(ButtonColours, FlashingSequence[i])].material.color = Colours[FlashingSequence[i]];
                yield return new WaitForSecondsRealtime(0.5f);
                for (int j = 0; j < 9; j++)
                {
                    ButtonMats[j].material = Mats[0];
                    ButtonMats[j].material.color = Colours[ButtonColours[j]];
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }
            yield return new WaitForSecondsRealtime(2f);
        }
    }

    private IEnumerator ButtonPress(int pos)
    {
        Pressed = true;
        Audio.PlaySoundAtTransform("press", Buttons[pos].transform);
        Buttons[pos].AddInteractionPunch(0.5f);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition -= new Vector3(0, 0.004f, 0);
            yield return null;
        }
        #region Checking solution and logging
        if (!Inputting && Active)
            Inputting = true;
        if (FlashingSequence[Presses] == ReferredButtons[pos])
        {
            Presses++;
            Debug.LogFormat("[Simon Shuffles #{0}] You pressed the {1} button, which was correct.", _moduleID, ColourNames[ButtonColours[pos]].ToLowerInvariant());
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[Simon Shuffles #{0}] You pressed the {1} button, which was incorrect. The {2} button should have been pressed. Strike!", _moduleID, ColourNames[ButtonColours[pos]].ToLowerInvariant(), ColourNames[ButtonColours[Array.IndexOf(ReferredButtons, FlashingSequence[Presses])]].ToLowerInvariant());
            Presses = 0;
        }
        if (Presses == 5)
        {
            Module.HandlePass();
            Debug.LogFormat("[Simon Shuffles #{0}] All three stages have been cleared. Module solved!", _moduleID);
            Audio.PlaySoundAtTransform("solve", Buttons[pos].transform);
            Solved = true;
            StartCoroutine(FadeToGrey());
        }
        else if (Presses == new int[] { 3, 4, -1 }[Stage])
        {
            Debug.LogFormat("[Simon Shuffles #{0}] All inputs for the current stage were correct. Next stage!", _moduleID);
            StartCoroutine(FadeToGrey());
            CannotPress = true;
        }
        #endregion
        #region Coroutine logic (eg. stopping and starting coroutines running one after each other)
        Inputs.Add(ButtonColours[pos]);
        for (int j = 0; j < 9; j++)
        {
            ButtonMats[j].material = Mats[0];
            ButtonMats[j].material.color = Colours[ButtonColours[j]];
        }
        if (Running[10])
            StopCoroutine(RunningCoroutines[10]);
        Running[10] = false;
        ButtonMats[pos].material = Mats[1];
        ButtonMats[pos].material.color = Colours[ButtonColours[pos]];
        if (Running[pos])
            StopCoroutine(RunningCoroutines[pos]);
        RunningCoroutines[pos] = StartCoroutine(TurnButtonOff(pos));
        if (Running[9] && RunningCoroutines[9] != null)
            StopCoroutine(RunningCoroutines[9]);
        RunningCoroutines[9] = StartCoroutine(WaitForReset());
        #endregion
        if (!Solved)
            Audio.PlaySoundAtTransform("beep" + (ButtonColours[pos] + 1).ToString(), Buttons[pos].transform);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition += new Vector3(0, 0.004f, 0);
            yield return null;
        }
    }

    private IEnumerator TurnButtonOff(int pos)
    {
        Running[pos] = true;
        yield return new WaitForSecondsRealtime(0.5f);
        ButtonMats[pos].material = Mats[0];
        ButtonMats[pos].material.color = Colours[ButtonColours[pos]];
        Running[pos] = false;
    }

    private IEnumerator WaitForReset()
    {
        Running[9] = true;
        if (CannotPress)
            yield return new WaitForSecondsRealtime(2f);
        else
            yield return new WaitForSecondsRealtime(3f);
        if (!Solved && Running[9])
        {
            Inputting = false;
            Presses = 0;
            Inputs = new List<int>();
            if (RunningCoroutines[10] != null)
                StopCoroutine(RunningCoroutines[10]);
            Debug.LogFormat("[Simon Shuffles #{0}] Resetting input...", _moduleID);
            RunningCoroutines[10] = StartCoroutine(StartFlashing());
        }
        Running[9] = false;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} roy' to press the red, orange and yellow buttons. A colour's abbreviation is the first letter of its name. Use '!{0} colo(u)rblind' to toggle colourblind support.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] ValidCommands = { "r", "o", "y", "g", "c", "b", "p", "m", "w" };
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            if (!ValidCommands.Contains(command[i].ToString()) && command != "colourblind" && command != "colorblind")
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            else if (command == "colourblind" || command == "colorblind")
            {
                ColourblindEnabled = !ColourblindEnabled;
                if (ColourblindEnabled)
                    for (int j = 0; j < 9; j++)
                        ColourblindTexts[j].text = ColourNames[ButtonColours[j]][0].ToString();
                else
                    for (int j = 0; j < 9; j++)
                        ColourblindTexts[j].text = "";
                break;
            }
            else
            {
                Buttons[Array.IndexOf(ButtonColours, Array.IndexOf(ValidCommands, command[i].ToString()))].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        while (!Solved)
        {
            for (int i = 0; i < 3 + Stage; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (FlashingSequence[i] == ReferredButtons[j])
                    {
                        Buttons[j].OnInteract();
                        yield return new WaitForSecondsRealtime(0.1f);
                    }
                }
            }
            yield return true;
            while (CannotPress)
                yield return true;
        }
    }
}
