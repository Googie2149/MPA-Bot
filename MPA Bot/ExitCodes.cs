namespace MPA_Bot
{
    public static class ExitCodes
{
    // Magic numbers are great, but at least they can be consistent
    public enum ExitCode : int
    {
        Success = 0,
        GeneralError = 1,
        Restart = 4,
        RestartAndUpdate = 5,
        DeadlockEscape = 12
    }
}
}
