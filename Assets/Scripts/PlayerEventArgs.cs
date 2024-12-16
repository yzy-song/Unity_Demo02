using System;

public class PlayerEventArgs : EventArgs
{
    public string Response { get; }

    public PlayerEventArgs(string response)
    {
        Response = response;
    }
}
