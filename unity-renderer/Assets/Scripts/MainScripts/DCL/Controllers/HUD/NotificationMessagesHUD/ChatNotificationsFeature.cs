using DCL.ProfanityFiltering;
using DCL.Social.Chat;
using DCL.Social.Friends;

namespace DCL.Chat.Notifications
{
    /// <summary>
    /// Plugin feature that initialize the chat notifications feature.
    /// </summary>
    public class ChatNotificationsFeature : IPlugin
    {
        private readonly ChatNotificationController chatNotificationController;

        public ChatNotificationsFeature()
        {
            chatNotificationController = CreateController();
        }

        private ChatNotificationController CreateController() => new ChatNotificationController(DataStore.i,
            MainChatNotificationsComponentView.Create(), TopNotificationComponentView.Create(),
            ChatController.i,
            FriendsController.i,
            new UserProfileWebInterfaceBridge(),
            Environment.i.serviceLocator.Get<IProfanityFilter>());

        public void Dispose()
        {
            chatNotificationController.Dispose();
        }
    }
}
