using AvatarSystem;
using DCL;
using DCL.Browser;
using DCL.Chat;
using DCL.Chat.HUD;
using DCL.HelpAndSupportHUD;
using DCL.Huds.QuestsPanel;
using DCL.Huds.QuestsTracker;
using DCL.ProfanityFiltering;
using DCL.SettingsCommon;
using DCL.SettingsPanelHUD;
using DCL.Social.Chat;
using DCL.Social.Friends;
using DCl.Social.Passports;
using DCL.Social.Passports;
using SignupHUD;
using SocialFeaturesAnalytics;
using UnityEngine;
using DCLServices.Lambdas.NamesService;
using DCLServices.Lambdas.LandsService;

public class HUDFactory : IHUDFactory
{
    private readonly DataStoreRef<DataStore_LoadingScreen> dataStoreLoadingScreen;

    public virtual IHUD CreateHUD(HUDElementID hudElementId)
    {
        IHUD hudElement = null;
        switch (hudElementId)
        {
            case HUDElementID.NONE:
                break;
            case HUDElementID.MINIMAP:
                hudElement = new MinimapHUDController(MinimapMetadataController.i, new WebInterfaceHomeLocationController(), DCL.Environment.i);
                break;
            case HUDElementID.PROFILE_HUD:
                hudElement = new ProfileHUDController(new UserProfileWebInterfaceBridge());
                break;
            case HUDElementID.NOTIFICATION:
                hudElement = new NotificationHUDController();
                break;
            case HUDElementID.AVATAR_EDITOR:
                hudElement = new AvatarEditorHUDController(DataStore.i.featureFlags,
                    Environment.i.platform.serviceProviders.analytics);
                break;
            case HUDElementID.SETTINGS_PANEL:
                hudElement = new SettingsPanelHUDController();
                break;
            case HUDElementID.PLAYER_INFO_CARD:
                if(DataStore.i.HUDs.enableNewPassport.Get())
                {
                    //TODO: this is temporary, once the old passport flow is removed
                    //this can be moved to the passport plugin
                    PlayerPassportHUDView view = PlayerPassportHUDView.CreateView();
                    hudElement = new PlayerPassportHUDController(
                        view,
                        new PassportPlayerInfoComponentController(
                            Resources.Load<StringVariable>("CurrentPlayerInfoCardId"),
                            view.PlayerInfoView,
                            DataStore.i,
                            Environment.i.serviceLocator.Get<IProfanityFilter>(),
                            FriendsController.i,
                            new UserProfileWebInterfaceBridge(),
                            new SocialAnalytics(
                                Environment.i.platform.serviceProviders.analytics,
                                new UserProfileWebInterfaceBridge())),
                        new PassportPlayerPreviewComponentController(view.PlayerPreviewView),
                        new PassportNavigationComponentController(
                            view.PassportNavigationView,
                            Environment.i.serviceLocator.Get<IProfanityFilter>(),
                            new WearableItemResolver(),
                            new WearablesCatalogControllerBridge(),
                            Environment.i.serviceLocator.Get<IEmotesCatalogService>(),
                            Environment.i.serviceLocator.Get<INamesService>(),
                            Environment.i.serviceLocator.Get<ILandsService>(),
                            new UserProfileWebInterfaceBridge(),
                            DataStore.i),
                        Resources.Load<StringVariable>("CurrentPlayerInfoCardId"),
                        new UserProfileWebInterfaceBridge(),
                        new WebInterfacePassportApiBridge(),
                        new SocialAnalytics(
                            Environment.i.platform.serviceProviders.analytics,
                            new UserProfileWebInterfaceBridge()),
                        DataStore.i);
                }
                else
                {
                    hudElement = new PlayerInfoCardHUDController(
                        FriendsController.i,
                        Resources.Load<StringVariable>("CurrentPlayerInfoCardId"),
                        new UserProfileWebInterfaceBridge(),
                        new WearablesCatalogControllerBridge(),
                        new SocialAnalytics(
                            Environment.i.platform.serviceProviders.analytics,
                            new UserProfileWebInterfaceBridge()),
                        Environment.i.serviceLocator.Get<IProfanityFilter>(),
                        DataStore.i,
                        CommonScriptableObjects.playerInfoCardVisibleState);
                }
                break;
            case HUDElementID.AIRDROPPING:
                hudElement = new AirdroppingHUDController();
                break;
            case HUDElementID.TERMS_OF_SERVICE:
                hudElement = new TermsOfServiceHUDController();
                break;
            case HUDElementID.FRIENDS:
                hudElement = new FriendsHUDController(DataStore.i,
                    FriendsController.i,
                    new UserProfileWebInterfaceBridge(),
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    ChatController.i,
                    SceneReferences.i.mouseCatcher);
                break;
            case HUDElementID.WORLD_CHAT_WINDOW:
                hudElement = new WorldChatWindowController(
                    new UserProfileWebInterfaceBridge(),
                    FriendsController.i,
                    ChatController.i,
                    DataStore.i,
                    SceneReferences.i.mouseCatcher,
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    Environment.i.serviceLocator.Get<IChannelsFeatureFlagService>(),
                    new WebInterfaceBrowserBridge(),
                    CommonScriptableObjects.rendererState);
                break;
            case HUDElementID.PRIVATE_CHAT_WINDOW:
                hudElement = new PrivateChatWindowController(
                    DataStore.i,
                    new UserProfileWebInterfaceBridge(),
                    ChatController.i,
                    FriendsController.i,
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    SceneReferences.i.mouseCatcher,
                    Resources.Load<InputAction_Trigger>("ToggleWorldChat"));
                break;
            case HUDElementID.PUBLIC_CHAT:
                hudElement = new PublicChatWindowController(
                    ChatController.i,
                    new UserProfileWebInterfaceBridge(),
                    DataStore.i,
                    Environment.i.serviceLocator.Get<IProfanityFilter>(),
                    SceneReferences.i.mouseCatcher,
                    Resources.Load<InputAction_Trigger>("ToggleWorldChat"));
                break;
            case HUDElementID.CHANNELS_CHAT:
                hudElement = new ChatChannelHUDController(
                    DataStore.i,
                    new UserProfileWebInterfaceBridge(),
                    ChatController.i,
                    SceneReferences.i.mouseCatcher,
                    Resources.Load<InputAction_Trigger>("ToggleWorldChat"),
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    Environment.i.serviceLocator.Get<IProfanityFilter>());
                break;
            case HUDElementID.CHANNELS_SEARCH:
                hudElement = new SearchChannelsWindowController(
                    ChatController.i,
                    SceneReferences.i.mouseCatcher,
                    DataStore.i,
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    Environment.i.serviceLocator.Get<IChannelsFeatureFlagService>());
                break;
            case HUDElementID.CHANNELS_CREATE:
                hudElement = new CreateChannelWindowController(ChatController.i, DataStore.i);
                break;
            case HUDElementID.CHANNELS_LEAVE_CONFIRMATION:
                hudElement = new LeaveChannelConfirmationWindowController(ChatController.i);
                break;
            case HUDElementID.TASKBAR:
                hudElement = new TaskbarHUDController(ChatController.i, FriendsController.i);
                break;
            case HUDElementID.OPEN_EXTERNAL_URL_PROMPT:
                hudElement = new ExternalUrlPromptHUDController();
                break;
            case HUDElementID.NFT_INFO_DIALOG:
                hudElement = new NFTPromptHUDController();
                break;
            case HUDElementID.TELEPORT_DIALOG:
                hudElement = new TeleportPromptHUDController();
                break;
            case HUDElementID.CONTROLS_HUD:
                hudElement = new ControlsHUDController();
                break;
            case HUDElementID.HELP_AND_SUPPORT_HUD:
                hudElement = new HelpAndSupportHUDController();
                break;
            case HUDElementID.USERS_AROUND_LIST_HUD:
                hudElement = new VoiceChatWindowController(
                    new UserProfileWebInterfaceBridge(),
                    FriendsController.i,
                    new SocialAnalytics(
                        Environment.i.platform.serviceProviders.analytics,
                        new UserProfileWebInterfaceBridge()),
                    DataStore.i,
                    Settings.i,
                    SceneReferences.i.mouseCatcher);
                break;
            case HUDElementID.GRAPHIC_CARD_WARNING:
                hudElement = new GraphicCardWarningHUDController();
                break;
            case HUDElementID.BUILDER_IN_WORLD_MAIN:
                break;
            case HUDElementID.QUESTS_PANEL:
                hudElement = new QuestsPanelHUDController();
                break;
            case HUDElementID.QUESTS_TRACKER:
                hudElement = new QuestsTrackerHUDController();
                break;
            case HUDElementID.SIGNUP:
                var analytics = Environment.i.platform.serviceProviders.analytics;
                hudElement = new SignupHUDController(analytics, dataStoreLoadingScreen.Ref);
                break;
            case HUDElementID.BUILDER_PROJECTS_PANEL:
                break;
            case HUDElementID.LOADING:
                hudElement = new LoadingHUDController();
                break;
        }

        return hudElement;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
    }
}
