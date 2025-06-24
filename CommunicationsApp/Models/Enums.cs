namespace CommunicationsApp.Models
{
    public class Enums
    {
        public enum ServerType
        {
            Public = 0,
            Private = 1,
            Community = 2
        }

        public enum ServerUpdateType
        {
            MemberJoined = 0,
            MemberLeft = 1,
            MemberUpdated = 2,
        }
    }
}
