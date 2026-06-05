using System;
using UnityEngine;

public class OledTouchRouter : MonoBehaviour
{
    [SerializeField] private bool logRawJson = true;
    [SerializeField] private bool logParsedEvent = true;

    public event Action<OledTouchEvent> TouchReceived;
    public event Action<OledTouchEvent> DriverTouchReceived;
    public event Action<OledTouchEvent> PassengerTouchReceived;

    public void ReceiveRawJson(string rawJson)
    {
        if (logRawJson)
        {
            Debug.Log("[OLED Touch Raw] " + rawJson);
        }

        if (!OledTouchJsonParser.TryParse(
                rawJson,
                out OledTouchEvent touchEvent,
                out string errorMessage
            ))
        {
            Debug.LogWarning("[OLED Touch] Parse failed: " + errorMessage + " | Raw: " + rawJson);
            return;
        }

        RouteTouchEvent(touchEvent);
    }

    public void RouteTouchEvent(OledTouchEvent touchEvent)
    {
        if (touchEvent == null)
        {
            return;
        }

        if (logParsedEvent)
        {
            Debug.Log("[OLED Touch] " + touchEvent);
        }

        TouchReceived?.Invoke(touchEvent);

        if (touchEvent.Source == OledTouchSource.Driver)
        {
            DriverTouchReceived?.Invoke(touchEvent);
            return;
        }

        if (touchEvent.Source == OledTouchSource.Passenger)
        {
            PassengerTouchReceived?.Invoke(touchEvent);
        }
    }
}
