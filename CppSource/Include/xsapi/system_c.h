// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
#include "types_c.h"
#include "xsapi/errors_c.h"

#if defined(__cplusplus)
extern "C" {
#endif

#if !XDK_API

struct XSAPI_XBOX_LIVE_USER_IMPL;

typedef enum XSAPI_SIGN_IN_STATUS
{
    /// <summary>
    /// Signed in successfully.
    /// </summary>
    XSAPI_SIGN_IN_STATUS_SUCCESS = 0,

    /// <summary>
    /// Need to invoke the signin API (w/ UX) to let the user take necessary actions for the sign-in operation to continue.
    /// Can only be returned from signin_silently().
    /// </summary>
    XSAPI_SIGN_IN_STATUS_USER_INTERACTION_REQUIRED,

    /// <summary>
    /// The user decided to cancel the sign-in operation.
    /// Can only be returned from signin().
    /// </summary>
    XSAPI_SIGN_IN_STATUS_USER_CANCEL
} XSAPI_SIGN_IN_STATUS;

typedef struct XSAPI_XBOX_LIVE_USER
{
    PCSTR xboxUserId; 
    PCSTR gamertag;
    PCSTR ageGroup;
    PCSTR privileges;
    bool isSignedIn;
#if UWP_API
    PCSTR webAccountId;
    Windows::System::User^ windowsSystemUser;
#endif
    XSAPI_XBOX_LIVE_USER_IMPL *pImpl;
} XSAPI_XBOX_LIVE_USER;

typedef struct XSAPI_SIGN_IN_RESULT_PAYLOAD
{
    XSAPI_SIGN_IN_STATUS status;
} XSAPI_SIGN_IN_RESULT_PAYLOAD;

typedef struct XSAPI_SIGN_IN_RESULT
{
    XSAPI_RESULT_INFO result;
    XSAPI_SIGN_IN_RESULT_PAYLOAD payload;
} XSAPI_SIGN_IN_RESULT;

typedef struct XSAPI_TOKEN_AND_SIGNATURE_RESULT_PAYLOAD
{
    PCSTR token;
    PCSTR signature;
    PCSTR xboxUserId;
    PCSTR gamertag;
    PCSTR xboxUserHash;
    PCSTR ageGroup;
    PCSTR privileges;
    PCSTR webAccountId;
} XSAPI_TOKEN_AND_SIGNATURE_RESULT_PAYLOAD;

typedef struct XSAPI_TOKEN_AND_SIGNATURE_RESULT
{
    XSAPI_RESULT_INFO result;
    XSAPI_TOKEN_AND_SIGNATURE_RESULT_PAYLOAD payload;
} XSAPI_TOKEN_AND_SIGNATURE_RESULT;

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserCreate(
    _Out_ XSAPI_XBOX_LIVE_USER** ppUser
    ) XSAPI_NOEXCEPT;

#if UWP_API

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserCreateFromSystemUser(
    _In_ Windows::System::User^ creationContext,
    _Out_ XSAPI_XBOX_LIVE_USER** ppUser
    ) XSAPI_NOEXCEPT;

#endif

XSAPI_DLLEXPORT void XBL_CALLING_CONV
XboxLiveUserDelete(
    _In_ XSAPI_XBOX_LIVE_USER* pUser
    ) XSAPI_NOEXCEPT;

typedef void(*XSAPI_SIGN_IN_COMPLETION_ROUTINE)(
    _In_ XSAPI_SIGN_IN_RESULT result,
    _In_opt_ void* context
    );

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserSignIn(
    _Inout_ XSAPI_XBOX_LIVE_USER* pUser,
    _In_ XSAPI_SIGN_IN_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
    ) XSAPI_NOEXCEPT;

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserSignInSilently(
    _Inout_ XSAPI_XBOX_LIVE_USER* pUser,
    _In_ XSAPI_SIGN_IN_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
    ) XSAPI_NOEXCEPT;

#if WINAPI_FAMILY && WINAPI_FAMILY==WINAPI_FAMILY_APP

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserSignInWithCoreDispatcher(
    _Inout_ XSAPI_XBOX_LIVE_USER* pUser,
    _In_ Platform::Object^ coreDispatcher,
    _In_ XSAPI_SIGN_IN_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
    ) XSAPI_NOEXCEPT;

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserSignInSilentlyWithCoreDispatcher(
    _Inout_ XSAPI_XBOX_LIVE_USER* pUser,
    _In_ Platform::Object^ coreDispatcher,
    _In_ XSAPI_SIGN_IN_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
    ) XSAPI_NOEXCEPT;

#endif

typedef void(*XSAPI_GET_TOKEN_AND_SIGNATURE_COMPLETION_ROUTINE)(
    _In_ XSAPI_TOKEN_AND_SIGNATURE_RESULT result,
    _In_opt_ void* context
    );

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
XboxLiveUserGetTokenAndSignature(
    _In_ XSAPI_XBOX_LIVE_USER* pUser,
    _In_ PCSTR httpMethod,
    _In_ PCSTR url,
    _In_ PCSTR headers,
    _In_ PCSTR requestBodyString,
    _In_ XSAPI_GET_TOKEN_AND_SIGNATURE_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
    ) XSAPI_NOEXCEPT;

typedef void(*XSAPI_SIGN_OUT_COMPLETED_HANDLER)(
    _In_ XSAPI_XBOX_LIVE_USER* pUser
    );

XSAPI_DLLEXPORT FUNCTION_CONTEXT XBL_CALLING_CONV
AddSignOutCompletedHandler(
    _In_ XSAPI_SIGN_OUT_COMPLETED_HANDLER signOutHandler
    ) XSAPI_NOEXCEPT;

XSAPI_DLLEXPORT void XBL_CALLING_CONV
RemoveSignOutCompletedHandler(
    _In_ FUNCTION_CONTEXT context
    ) XSAPI_NOEXCEPT;

#endif //!XDK_API

#if defined(__cplusplus)
} // end extern "C"
#endif // defined(__cplusplus)