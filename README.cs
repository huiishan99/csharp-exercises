using UnityEngine;

public class MultiDisplayActivator : MonoBehaviour
{
    [SerializeField] private bool activateOnStart = true;

    private void Start()
    {
        if (!activateOnStart)
        {
            return;
        }

        ActivateDisplays();
    }

    public void ActivateDisplays()
    {
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            Debug.Log("[Display] Activated Display " + (i + 1));
        }

        Debug.Log("[Display] Display count = " + Display.displays.Length);
    }
}
