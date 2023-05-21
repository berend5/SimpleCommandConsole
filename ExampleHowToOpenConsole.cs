using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PunIntended.Tools;

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
