namespace CommunicationsApp.Models
{
    public class Enums
    {
        public enum ServerType
        {
            Public = 0,
            Private,
            Community,
        }

        public enum ServerUpdateType
        {
            MemberJoined = 0,
            MemberLeft,
            MemberUpdated,
        }
    }
}
