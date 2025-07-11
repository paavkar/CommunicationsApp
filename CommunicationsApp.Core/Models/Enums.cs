﻿namespace CommunicationsApp.Core.Models
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
            ChannelClassAdded,
            ChannelClassRemoved,
            ChannelClassUpdated,
            ChannelAdded,
            ChannelRemoved,
            ChannelUpdated,
        }

        public enum ServerPermissionType
        {
            DisplayChannels = 0,
            ManageChannels,
            ManageRoles,
            ViewLogs,
            ManageServer,
            CreateInvite,
            ChangeNickname,
            ManageNicknames,
            KickApproveMembers,
            BanMembers,
            TimeoutMembers,
            SendMessages,
            SendMessagesToThreads,
            CreatePublicThreads,
            CreatePrivateThreads,
            EmbedLinks,
            AttachFiles,
            AddReactions,
            ManageMessages,
            ManageThreads,
            ReadMessageHistory,
            AdminPrivileges,
        }
    }
}
