﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.Xbox.Services;
using Microsoft.Xbox.Services.Social.Manager;

using UnityEngine;
using UnityEngine.UI;

public class UserProfile : MonoBehaviour
{
    public XboxLiveUserInfo XboxLiveUser;

    public string InputControllerButton;

    private bool AllowSignInAttempt = true;
    private bool ConfigAvailable = true;

    [HideInInspector]
    public GameObject signInPanel;

    [HideInInspector]
    public GameObject profileInfoPanel;

    [HideInInspector]
    public Image gamerpic;

    [HideInInspector]
    public Image gamerpicMask;

    [HideInInspector]
    public Text gamertag;

    [HideInInspector]
    public Text gamerscore;

    [HideInInspector]
    public XboxLiveUserInfo XboxLiveUserPrefab;

    public bool AllowGuestAccounts = false;

    public readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

    private XboxSocialUserGroup userGroup;

    public void Awake()
    {
        this.EnsureEventSystem();
        XboxLiveServicesSettings.EnsureXboxLiveServicesSettings();

        if (!XboxLiveUserManager.Instance.IsInitialized)
        {
            XboxLiveUserManager.Instance.Initialize();
        }
    }

    public void Start()
    {
        // Disable the sign-in button if there's no configuration available.
        if (XboxLive.Instance.AppConfig == null || XboxLive.Instance.AppConfig.ServiceConfigurationId == null)
        {
            this.ConfigAvailable = false;

            Text signInButtonText = this.signInPanel.GetComponentInChildren<Button>().GetComponentInChildren<Text>(true);
            if (signInButtonText != null)
            {
                signInButtonText.fontSize = 16;
                signInButtonText.text = "Xbox Live is not enabled.\nSee errors for detail.";
            }
        }
        this.Refresh();

        SocialManagerComponent.Instance.EventProcessed += SocialManagerEventProcessed;
        Microsoft.Xbox.Services.XboxLiveUser.SignOutCompleted += XboxLiveUserOnSignOutCompleted;

        if (XboxLiveUserManager.Instance.SingleUserModeEnabled)
        {
            if (XboxLiveUserManager.Instance.UserForSingleUserMode == null)
            {
                XboxLiveUserManager.Instance.UserForSingleUserMode = Instantiate(this.XboxLiveUserPrefab);
                this.XboxLiveUser = XboxLiveUserManager.Instance.UserForSingleUserMode;
                if (XboxLive.Instance.AppConfig != null && XboxLive.Instance.AppConfig.ServiceConfigurationId != null)
                {
                    this.SignIn();
                }
            }
            else
            {
                this.XboxLiveUser = XboxLiveUserManager.Instance.UserForSingleUserMode;
                this.LoadProfileInfo();
            }
        }
    }

    private void XboxLiveUserOnSignOutCompleted(object sender, SignOutCompletedEventArgs signOutCompletedEventArgs)
    {
        var xboxLiveUser = signOutCompletedEventArgs.User as XboxLiveUser;
        if (xboxLiveUser != null)
        {
            XboxLive.Instance.StatsManager.RemoveLocalUser(xboxLiveUser);
            XboxLive.Instance.SocialManager.RemoveLocalUser(xboxLiveUser);
        }

        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            // Refresh updates UX elements so that needs to be called on the App thread
            this.Refresh();
        }, false);
    }

    public void SignIn()
    {
        // Disable the sign-in button
        this.signInPanel.GetComponentInChildren<Button>().interactable = false;

        // Don't allow subsequent sign in attempts until the current attempt completes
        this.AllowSignInAttempt = false;
        this.StartCoroutine(this.InitializeXboxLiveUser());
    }

    public void Update()
    {
        while (ExecuteOnMainThread.Count > 0)
        {
            ExecuteOnMainThread.Dequeue().Invoke();
        }

        if (this.AllowSignInAttempt && !string.IsNullOrEmpty(this.InputControllerButton) && Input.GetKeyDown(this.InputControllerButton))
        {
            this.SignIn();
        }
    }

    public IEnumerator InitializeXboxLiveUser()
    {
        yield return null;

#if ENABLE_WINMD_SUPPORT
        if (!XboxLiveUserManager.Instance.SingleUserModeEnabled && this.XboxLiveUser != null && this.XboxLiveUser.WindowsSystemUser == null)
        {
            var autoPicker = new Windows.System.UserPicker { AllowGuestAccounts = this.AllowGuestAccounts };
            autoPicker.PickSingleUserAsync().AsTask().ContinueWith(
                    task =>
                        {
                            if (task.Status == TaskStatus.RanToCompletion)
                            {
                                this.XboxLiveUser.Initialize(task.Result);
                                this.ExecuteOnMainThread.Enqueue(() => { StartCoroutine(this.SignInAsync()); });
                            }
                            else
                            {
                                if (XboxLiveServicesSettings.Instance.DebugLogsOn)
                                {
                                    Debug.Log("Exception occured: " + task.Exception.Message);
                                }
                            }
                        });
        }
        else
        {
            if (this.XboxLiveUser == null)
            {
                this.XboxLiveUser = XboxLiveUserManager.Instance.UserForSingleUserMode;
            }
            if (this.XboxLiveUser.User == null)
            {
                this.XboxLiveUser.Initialize();
            }
            yield return this.SignInAsync();
        }
#else
        if (XboxLiveUserManager.Instance.SingleUserModeEnabled && this.XboxLiveUser == null)
        {
            this.XboxLiveUser = XboxLiveUserManager.Instance.GetSingleModeUser();
        }

        this.XboxLiveUser.Initialize();
        yield return this.SignInAsync();
#endif
    }

    public IEnumerator SignInAsync()
    {
        SignInStatus signInStatus;
        TaskYieldInstruction<SignInResult> signInSilentlyTask = this.XboxLiveUser.User.SignInSilentlyAsync().AsCoroutine();
        yield return signInSilentlyTask;

        signInStatus = signInSilentlyTask.Result.Status;
        if (signInSilentlyTask.Result.Status != SignInStatus.Success)
        {
            TaskYieldInstruction<SignInResult> signInTask = this.XboxLiveUser.User.SignInAsync().AsCoroutine();
            yield return signInTask;

            signInStatus = signInTask.Result.Status;
        }

        // Throw any exceptions if needed.
        if (signInStatus == SignInStatus.Success)
        {
            XboxLive.Instance.StatsManager.AddLocalUser(this.XboxLiveUser.User);
            XboxLive.Instance.SocialManager.AddLocalUser(this.XboxLiveUser.User, SocialManagerExtraDetailLevel.PreferredColorLevel);
        }
        this.Refresh();
    }

    private void LoadProfileInfo(bool userAdded = true)
    {
        this.gamertag.text = this.XboxLiveUser.User.Gamertag;

        if (userAdded)
        {
            userGroup = XboxLive.Instance.SocialManager.CreateSocialUserGroupFromList(this.XboxLiveUser.User, new List<string> { this.XboxLiveUser.User.XboxUserId });
        }
    }

    private void SocialManagerEventProcessed(object sender, SocialEvent socialEvent)
    {
        if (this.XboxLiveUser.User == null ||
            socialEvent.User.XboxUserId != this.XboxLiveUser.User.XboxUserId)
        {
            // Ignore the social event
            return;
        }
        
        if (socialEvent.EventType == SocialEventType.LocalUserAdded)
        {
            if (socialEvent.ErrorCode != 0 && XboxLiveServicesSettings.Instance.DebugLogsOn)
            {
                Debug.LogFormat("Failed to add local user to SocialManager: {0}", socialEvent.ErrorMessge);
                LoadProfileInfo(false);
            }
            else
            {
                LoadProfileInfo();
            }
        }
        else if (socialEvent.EventType == SocialEventType.SocialUserGroupLoaded &&
                 ((SocialUserGroupLoadedEventArgs)socialEvent.EventArgs).SocialUserGroup == userGroup)
        {
            if (socialEvent.ErrorCode != 0 && XboxLiveServicesSettings.Instance.DebugLogsOn)
            {
                Debug.LogFormat("Failed to load the SocialUserGroup: {0}", socialEvent.ErrorMessge);
            }
            else
            {
                StartCoroutine(FinishLoadingProfileInfo());
            }
        }
    }

    private IEnumerator FinishLoadingProfileInfo()
    {
        var socialUser = userGroup.GetUsersFromXboxUserIds(new List<string> { this.XboxLiveUser.User.XboxUserId })[0];

        var www = new WWW(socialUser.DisplayPicRaw + "&w=128");
        yield return www;

        try
        {

            if (www.isDone && string.IsNullOrEmpty(www.error))
            {
                var t = www.texture;
                var r = new Rect(0, 0, t.width, t.height);
                this.gamerpic.sprite = Sprite.Create(t, r, Vector2.zero);
            }

            this.gamerscore.text = socialUser.Gamerscore;

            if (socialUser.PreferredColor != null)
            {
                this.profileInfoPanel.GetComponent<Image>().color =
                    ColorFromHexString(socialUser.PreferredColor.PrimaryColor);
                this.gamerpicMask.color = ColorFromHexString(socialUser.PreferredColor.PrimaryColor);
            }

        }
        catch (Exception ex)
        {
            if (XboxLiveServicesSettings.Instance.DebugLogsOn)
            {
                Debug.Log("There was an error while loading Profile Info. Exception: " + ex.Message);
            }
        }
        this.Refresh();
    }

    public static Color ColorFromHexString(string color)
    {
        var r = (float)byte.Parse(color.Substring(0, 2), NumberStyles.HexNumber) / 255;
        var g = (float)byte.Parse(color.Substring(2, 2), NumberStyles.HexNumber) / 255;
        var b = (float)byte.Parse(color.Substring(4, 2), NumberStyles.HexNumber) / 255;

        return new Color(r, g, b);
    }

    private void Refresh()
    {
        var isSignedIn = this.XboxLiveUser != null && this.XboxLiveUser.User != null && this.XboxLiveUser.User.IsSignedIn;
        this.AllowSignInAttempt = !isSignedIn && this.ConfigAvailable;
        this.signInPanel.GetComponentInChildren<Button>().interactable = this.AllowSignInAttempt;
        this.signInPanel.SetActive(!isSignedIn);
        this.profileInfoPanel.SetActive(isSignedIn);
    }
    
}