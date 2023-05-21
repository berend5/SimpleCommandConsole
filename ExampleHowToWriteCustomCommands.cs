using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PunIntended.Tools;

// if commands are written inside monobehaviours,
// each instance of the monobehaviour will have its command called
public class ExampleHowToWriteCustomCommands : MonoBehaviour
{
    [Command] // without parameters, the command will take the name of the method
    private void SetCubeHeight(float y)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    [Command("set_cube_active")] // you can enter a string to give the command a custom name
    private void SetCubeActive(bool activated)
    {
        gameObject.SetActive(activated);
    }
}

// you can also have commands in static classes
public static class ExampleHowToWriteCustomCommandsInStaticClass
{
    [Command("add_two_integers")]
    private static void AddTwoIntegers(int a, int b)
    {
        int result = a + b;
        Debug.Log(result);
    }

    [Command("set_timescale")]
    public static void SetTimeScaleCommand(float timeScale)
    {
        Time.timeScale = timeScale;
    }

    [Command("set_target_fps")]
    public static void SetTargetFramerateCommand(int targetFrameRate)
    {
        Application.targetFrameRate = targetFrameRate;
    }
}