namespace dsproject
{
    internal class StateUpdateInfo
    {
        public StateUpdateResult Result { get; set; }
        public string ErrorString { get; set; }
    }

    internal enum StateUpdateResult
    {
        Ok, Error,
    }
}
