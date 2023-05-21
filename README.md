# SimpleCommandConsole
![simple command console window for unity.](https://media.discordapp.net/attachments/901542013999136851/1109970215636320326/Screenshot_2023-05-22_002401.png)

## features
- custom commands
- commands on monobehaviours or static classes
- parameters: floats, ints and bools
- basic auto-complete
- less than 500 loc, easy to modify source code

## examples
you can find these scripts inside the Examples folder.

### custom commands
```
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
```
### opening the command console
```
// attach this script to a gameobject to open and close the console
public class ExampleHowToOpenConsole : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SimpleCommandConsole.Open(closeKeyCode: KeyCode.Escape);
        }
    }
}
```