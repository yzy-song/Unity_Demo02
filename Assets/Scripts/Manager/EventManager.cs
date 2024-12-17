using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static readonly Dictionary<string, Delegate> eventDictionary = new Dictionary<string, Delegate>();

    public static void Subscribe<T>(string eventName, Action<T> listener)
    {
        if (!eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] = null;
        }
        eventDictionary[eventName] = (Action<T>)eventDictionary[eventName] + listener;
    }

    public static void Unsubscribe<T>(string eventName, Action<T> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] = (Action<T>)eventDictionary[eventName] - listener;
            if (eventDictionary[eventName] == null)
            {
                eventDictionary.Remove(eventName);
            }
        }
    }

    public static void Invoke<T>(string eventName, T eventData)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as Action<T>)?.Invoke(eventData);
        }
        else
        {
            Debug.LogWarning($"No listeners for event: {eventName}");
        }
    }


}
