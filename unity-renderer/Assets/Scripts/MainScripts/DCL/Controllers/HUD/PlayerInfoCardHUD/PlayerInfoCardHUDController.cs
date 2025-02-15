using Cysharp.Threading.Tasks;
using DCL;
using DCL.Helpers;
using DCL.Interface;
using DCL.ProfanityFiltering;
using DCL.Social.Friends;
using SocialFeaturesAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public class PlayerInfoCardHUDController : IHUD
{
    internal readonly PlayerInfoCardHUDView view;
    internal readonly StringVariable currentPlayerId;
    internal UserProfile currentUserProfile;

    private UserProfile viewingUserProfile;
    private UserProfile ownUserProfile => userProfileBridge.GetOwn();

    private readonly IFriendsController friendsController;
    private readonly InputAction_Trigger toggleFriendsTrigger;
    private readonly InputAction_Trigger closeWindowTrigger;
    private readonly InputAction_Trigger toggleWorldChatTrigger;
    private readonly IUserProfileBridge userProfileBridge;
    private readonly IWearableCatalogBridge wearableCatalogBridge;
    private readonly IProfanityFilter profanityFilter;
    private readonly DataStore dataStore;
    private readonly BooleanVariable playerInfoCardVisibleState;
    private readonly List<string> loadedWearables = new List<string>();
    private readonly ISocialAnalytics socialAnalytics;

    private bool isNewFriendRequestsEnabled => dataStore.featureFlags.flags.Get().IsFeatureEnabled("new_friend_requests");
    private double passportOpenStartTime = 0;

    public PlayerInfoCardHUDController(IFriendsController friendsController,
        StringVariable currentPlayerIdData,
        IUserProfileBridge userProfileBridge,
        IWearableCatalogBridge wearableCatalogBridge,
        ISocialAnalytics socialAnalytics,
        IProfanityFilter profanityFilter,
        DataStore dataStore,
        BooleanVariable playerInfoCardVisibleState)
    {
        this.friendsController = friendsController;
        view = PlayerInfoCardHUDView.CreateView();
        view.Initialize(() => OnCloseButtonPressed(),
            ReportPlayer, BlockPlayer, UnblockPlayer,
            AddPlayerAsFriend, CancelInvitation, AcceptFriendRequest, RejectFriendRequest);
        currentPlayerId = currentPlayerIdData;
        this.userProfileBridge = userProfileBridge;
        this.wearableCatalogBridge = wearableCatalogBridge;
        this.socialAnalytics = socialAnalytics;
        this.profanityFilter = profanityFilter;
        this.dataStore = dataStore;
        this.playerInfoCardVisibleState = playerInfoCardVisibleState;
        currentPlayerId.OnSame += OnCurrentPlayerIdUpdated;
        currentPlayerId.OnChange += OnCurrentPlayerIdChanged;
        OnCurrentPlayerIdUpdated(currentPlayerId);

        toggleFriendsTrigger = Resources.Load<InputAction_Trigger>("ToggleFriends");
        toggleFriendsTrigger.OnTriggered -= OnCloseButtonPressed;
        toggleFriendsTrigger.OnTriggered += OnCloseButtonPressed;

        closeWindowTrigger = Resources.Load<InputAction_Trigger>("CloseWindow");
        closeWindowTrigger.OnTriggered -= OnCloseButtonPressed;
        closeWindowTrigger.OnTriggered += OnCloseButtonPressed;

        toggleWorldChatTrigger = Resources.Load<InputAction_Trigger>("ToggleWorldChat");
        toggleWorldChatTrigger.OnTriggered -= OnCloseButtonPressed;
        toggleWorldChatTrigger.OnTriggered += OnCloseButtonPressed;

        friendsController.OnUpdateFriendship -= OnFriendStatusUpdated;
        friendsController.OnUpdateFriendship += OnFriendStatusUpdated;
    }

    public void CloseCard()
    {
        currentPlayerId.Set(null);
    }

    private void OnCloseButtonPressed(DCLAction_Trigger action = DCLAction_Trigger.CloseWindow)
    {
        CloseCard();
    }

    private void AddPlayerAsFriend()
    {
        if (isNewFriendRequestsEnabled)
        {
            dataStore.HUDs.sendFriendRequest.Set(currentPlayerId, true);
            dataStore.HUDs.sendFriendRequestSource.Set((int)PlayerActionSource.Passport);
        }
        else
        {
            friendsController.RequestFriendship(currentPlayerId);
            socialAnalytics.SendFriendRequestSent(ownUserProfile.userId, currentPlayerId, 0, PlayerActionSource.Passport);
        }
    }

    private void CancelInvitation() =>
        CancelInvitationAsync().Forget();

    private async UniTaskVoid CancelInvitationAsync()
    {
        if (isNewFriendRequestsEnabled)
        {
            try
            {
                FriendRequest request = await friendsController.CancelRequestByUserIdAsync(currentPlayerId).Timeout(TimeSpan.FromSeconds(10));
                socialAnalytics.SendFriendRequestCancelled(request.From, request.To, PlayerActionSource.Passport.ToString());
            }
            catch (Exception e)
            {
                FriendRequest request = friendsController.GetAllocatedFriendRequestByUser(currentPlayerId);
                socialAnalytics.SendFriendRequestError(request?.From, request?.To,
                    PlayerActionSource.Passport.ToString(),
                    e is FriendshipException fe
                        ? fe.ErrorCode.ToString()
                        : FriendRequestErrorCodes.Unknown.ToString());
                throw;
            }
        }
        else
        {
            friendsController.CancelRequestByUserId(currentPlayerId);
            socialAnalytics.SendFriendRequestCancelled(ownUserProfile.userId, currentPlayerId, PlayerActionSource.Passport.ToString());
        }
    }

    private void AcceptFriendRequest() =>
        AcceptFriendRequestAsync().Forget();

    private async UniTaskVoid AcceptFriendRequestAsync()
    {
        if (isNewFriendRequestsEnabled)
        {
            try
            {
                FriendRequest request = friendsController.GetAllocatedFriendRequestByUser(currentPlayerId);
                request = await friendsController.AcceptFriendshipAsync(request.FriendRequestId).Timeout(TimeSpan.FromSeconds(10));
                socialAnalytics.SendFriendRequestApproved(request.From, request.To, PlayerActionSource.Passport.ToString(),
                    request.HasBodyMessage);
            }
            catch (Exception e)
            {
                FriendRequest request = friendsController.GetAllocatedFriendRequestByUser(currentPlayerId);
                socialAnalytics.SendFriendRequestError(request?.From, request?.To,
                    PlayerActionSource.Passport.ToString(),
                    e is FriendshipException fe
                        ? fe.ErrorCode.ToString()
                        : FriendRequestErrorCodes.Unknown.ToString());
                throw;
            }
        }
        else
        {
            friendsController.AcceptFriendship(currentPlayerId);
            socialAnalytics.SendFriendRequestApproved(ownUserProfile.userId, currentPlayerId,
                PlayerActionSource.Passport.ToString(),
                false);
        }
    }

    private void RejectFriendRequest() =>
        RejectFriendRequestAsync().Forget();

    private async UniTaskVoid RejectFriendRequestAsync()
    {
        if (isNewFriendRequestsEnabled)
        {
            try
            {
                FriendRequest request = friendsController.GetAllocatedFriendRequestByUser(currentPlayerId);
                request = await friendsController.RejectFriendshipAsync(request.FriendRequestId).Timeout(TimeSpan.FromSeconds(10));
                socialAnalytics.SendFriendRequestRejected(request.From, request.To,
                    PlayerActionSource.Passport.ToString(), request.HasBodyMessage);
            }
            catch (Exception e)
            {
                FriendRequest request = friendsController.GetAllocatedFriendRequestByUser(currentPlayerId);
                socialAnalytics.SendFriendRequestError(request?.From, request?.To,
                    PlayerActionSource.Passport.ToString(),
                    e is FriendshipException fe
                        ? fe.ErrorCode.ToString()
                        : FriendRequestErrorCodes.Unknown.ToString());
                throw;
            }
        }
        else
        {
            friendsController.RejectFriendship(currentPlayerId);
            socialAnalytics.SendFriendRequestRejected(ownUserProfile.userId, currentPlayerId,
                PlayerActionSource.Passport.ToString(), false);
        }
    }

    private void OnCurrentPlayerIdChanged(string current, string previous)
    {
        OnCurrentPlayerIdUpdated(current);
    }

    private void OnCurrentPlayerIdUpdated(string current)
    {
        if (currentUserProfile != null)
            currentUserProfile.OnUpdate -= SetUserProfile;

        currentUserProfile = string.IsNullOrEmpty(current)
            ? null
            : userProfileBridge.Get(current);

        if (currentUserProfile == null)
        {
            if (playerInfoCardVisibleState.Get())
                socialAnalytics.SendPassportClose(Time.realtimeSinceStartup - passportOpenStartTime);

            view.SetCardActive(false);
            wearableCatalogBridge.RemoveWearablesInUse(loadedWearables);
            loadedWearables.Clear();
        }
        else
        {
            currentUserProfile.OnUpdate += SetUserProfile;

            TaskUtils.Run(async () =>
                     {
                         await AsyncSetUserProfile(currentUserProfile);
                         view.SetCardActive(true);
                         socialAnalytics.SendPassportOpen();
                     })
                     .Forget();

            passportOpenStartTime = Time.realtimeSinceStartup;
        }
    }

    private void SetUserProfile(UserProfile userProfile)
    {
        Assert.IsTrue(userProfile != null, "userProfile can't be null");

        TaskUtils.Run(async () => await AsyncSetUserProfile(userProfile)).Forget();
    }
    private async UniTask AsyncSetUserProfile(UserProfile userProfile)
    {
        string filterName = await FilterName(userProfile);
        string filterDescription = await FilterDescription(userProfile);
        await UniTask.SwitchToMainThread();

        view.SetName(filterName);
        view.SetDescription(filterDescription);
        view.ClearCollectibles();
        view.SetIsBlocked(IsBlocked(userProfile.userId));
        LoadAndShowWearables(userProfile);
        UpdateFriendshipInteraction();

        if (viewingUserProfile != null)
            viewingUserProfile.snapshotObserver.RemoveListener(view.SetFaceSnapshot);

        userProfile.snapshotObserver.AddListener(view.SetFaceSnapshot);
        viewingUserProfile = userProfile;
    }

    public void SetVisibility(bool visible)
    {
        view.SetVisibility(visible);

        if (viewingUserProfile != null)
            viewingUserProfile.snapshotObserver.RemoveListener(view.SetFaceSnapshot);

        if (visible)
        {
            if (viewingUserProfile != null)
                viewingUserProfile.snapshotObserver.AddListener(view.SetFaceSnapshot);
        }
    }

    private void BlockPlayer()
    {
        if (ownUserProfile.IsBlocked(currentUserProfile.userId)) return;
        ownUserProfile.Block(currentUserProfile.userId);
        view.SetIsBlocked(true);
        WebInterface.SendBlockPlayer(currentUserProfile.userId);
        socialAnalytics.SendPlayerBlocked(friendsController.IsFriend(currentUserProfile.userId), PlayerActionSource.Passport);
    }

    private void UnblockPlayer()
    {
        if (!ownUserProfile.IsBlocked(currentUserProfile.userId)) return;
        ownUserProfile.Unblock(currentUserProfile.userId);
        view.SetIsBlocked(false);
        WebInterface.SendUnblockPlayer(currentUserProfile.userId);
        socialAnalytics.SendPlayerUnblocked(friendsController.IsFriend(currentUserProfile.userId), PlayerActionSource.Passport);
    }

    private void ReportPlayer()
    {
        WebInterface.SendReportPlayer(currentPlayerId, currentUserProfile?.name);
        socialAnalytics.SendPlayerReport(PlayerReportIssueType.None, 0, PlayerActionSource.Passport);
    }

    public void Dispose()
    {
        if (currentUserProfile != null)
            currentUserProfile.OnUpdate -= SetUserProfile;

        if (currentPlayerId != null)
        {
            currentPlayerId.OnSame -= OnCurrentPlayerIdUpdated;
            currentPlayerId.OnChange -= OnCurrentPlayerIdChanged;
        }

        if (closeWindowTrigger != null)
            closeWindowTrigger.OnTriggered -= OnCloseButtonPressed;

        if (closeWindowTrigger != null)
            closeWindowTrigger.OnTriggered -= OnCloseButtonPressed;

        if (toggleWorldChatTrigger != null)
            toggleWorldChatTrigger.OnTriggered -= OnCloseButtonPressed;

        if (toggleFriendsTrigger != null)
            toggleFriendsTrigger.OnTriggered -= OnCloseButtonPressed;

        if (viewingUserProfile != null)
            viewingUserProfile.snapshotObserver.RemoveListener(view.SetFaceSnapshot);

        if (view != null)
            Object.Destroy(view.gameObject);
    }

    private void OnFriendStatusUpdated(string userId, FriendshipAction action)
    {
        if (currentUserProfile == null)
            return;

        UpdateFriendshipInteraction();
    }

    private void UpdateFriendshipInteraction()
    {
        if (currentUserProfile == null)
        {
            view.HideFriendshipInteraction();
            return;
        }

        view.UpdateFriendshipInteraction(CanBeFriends(),
            friendsController.GetUserStatus(currentUserProfile.userId));
    }

    private bool CanBeFriends()
    {
        return friendsController != null && friendsController.IsInitialized && currentUserProfile.hasConnectedWeb3;
    }

    private void LoadAndShowWearables(UserProfile userProfile)
    {
        wearableCatalogBridge.RequestOwnedWearables(userProfile.userId)
                             .Then(wearables =>
                             {
                                 var wearableIds = wearables.Select(x => x.id).ToArray();
                                 userProfile.SetInventory(wearableIds);
                                 loadedWearables.AddRange(wearableIds);
                                 var containedWearables = wearables
                                     // this makes any sense?
                                     .Where(wearable => wearableCatalogBridge.IsValidWearable(wearable.id));
                                 view.SetWearables(containedWearables);
                             })
                             .Catch(Debug.LogError);
    }

    private bool IsBlocked(string userId)
    {
        return ownUserProfile != null && ownUserProfile.IsBlocked(userId);
    }

    private async UniTask<string> FilterName(UserProfile userProfile)
    {
        return IsProfanityFilteringEnabled()
            ? await profanityFilter.Filter(userProfile.userName)
            : userProfile.userName;
    }

    private async UniTask<string> FilterDescription(UserProfile userProfile)
    {
        return IsProfanityFilteringEnabled()
            ? await profanityFilter.Filter(userProfile.description)
            : userProfile.description;
    }

    private bool IsProfanityFilteringEnabled()
    {
        return dataStore.settings.profanityChatFilteringEnabled.Get();
    }
}
