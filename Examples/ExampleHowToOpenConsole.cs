using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PunIntended.Tools;

// attach this script to a gameobject to open and close the console
public class ExampleHowToOpenConsole : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeveloperConsole.Open(closeKeyCode: KeyCode.Escape);
        }
    }
}
