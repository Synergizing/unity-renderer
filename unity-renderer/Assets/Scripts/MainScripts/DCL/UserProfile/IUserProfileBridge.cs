﻿using Cysharp.Threading.Tasks;

public interface IUserProfileBridge
{
    void SaveUnverifiedName(string name);
    void SaveDescription(string description);
    void RequestFullUserProfile(string userId);
    UniTask<UserProfile> RequestFullUserProfileAsync(string userId);
    UserProfile GetOwn();
    void AddUserProfileToCatalog(UserProfileModel userProfileModel);
    UserProfile Get(string userId);
    UserProfile GetByName(string userNameOrId);
    void SignUp();
}
