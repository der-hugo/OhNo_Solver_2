using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundSetter : MonoBehaviour
{

    private Image image;

    private InputField inputField;

    // Use this for initialization
    void Start()
    {
        image = GetComponent<Image>();
        inputField = GetComponentInChildren<InputField>();
    }

    public void SetBackground()
    {
        switch (inputField.text)
        {
            case "":
                image.color = Color.white;
                break;

            case "r":
                image.color = Color.red;
                break;

            default:
                int x = 0;
                int.TryParse(inputField.text, out x);
                image.color = x == 0 ? Color.magenta : Color.blue;
                break;
        }
    }
}
