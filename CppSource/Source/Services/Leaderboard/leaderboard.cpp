// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pch.h"
#include "leaderboard_helper.h"

// todo - move global variabes into a pimpl and store it in the xsapi singleton
std::vector<XSAPI_LEADERBOARD_QUERY *> m_queries;
xbox_live_result<leaderboard_query> cppLeaderboardQueryResult;

XSAPI_DLLEXPORT XSAPI_LEADERBOARD_QUERY* XBL_CALLING_CONV
LeaderboardQueryCreate(
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    auto query = new XSAPI_LEADERBOARD_QUERY();
    query->pImpl = new XSAPI_LEADERBOARD_QUERY_IMPL(leaderboard_query(), query);
    m_queries.push_back(query);

    return query;
}
CATCH_RETURN_WITH(nullptr)

XSAPI_DLLEXPORT void XBL_CALLING_CONV
LeaderboardQuerySetSkipResultToMe(
    _In_ XSAPI_LEADERBOARD_QUERY* leaderboardQuery,
    _In_ bool skipResultToMe
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    leaderboardQuery->pImpl->SetSkipResultToMe(skipResultToMe);
}
CATCH_RETURN_WITH(;)

XSAPI_DLLEXPORT void XBL_CALLING_CONV
LeaderboardQuerySetSkipResultToRank(
    _In_ XSAPI_LEADERBOARD_QUERY* leaderboardQuery,
    _In_ uint32_t skipResultToRank
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    leaderboardQuery->pImpl->SetSkipResultToRank(skipResultToRank);
}
CATCH_RETURN_WITH(;)

XSAPI_DLLEXPORT void XBL_CALLING_CONV
LeaderboardQuerySetMaxItems(
    _In_ XSAPI_LEADERBOARD_QUERY* leaderboardQuery,
    _In_ uint32_t maxItems
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    leaderboardQuery->pImpl->SetMaxItems(maxItems);
}
CATCH_RETURN_WITH(;)

XSAPI_DLLEXPORT void XBL_CALLING_CONV
LeaderboardQuerySetOrder(
    _In_ XSAPI_LEADERBOARD_QUERY* leaderboardQuery,
    _In_ XSAPI_SORT_ORDER order
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    leaderboardQuery->pImpl->SetOrder(order);
}
CATCH_RETURN_WITH(;)

XSAPI_DLLEXPORT bool XBL_CALLING_CONV
LeaderboardResultHasNext(
    _In_ XSAPI_LEADERBOARD_RESULT* leaderboardResult
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    return leaderboardResult->pImpl->cppLeaderboardResult().has_next();
}
CATCH_RETURN_WITH(false)


#if !defined(XBOX_LIVE_CREATORS_SDK)
HC_RESULT LeaderboardResultGetNextExecute(
    _In_opt_ void* context,
    _In_ HC_TASK_HANDLE taskHandle
)
{
    auto args = reinterpret_cast<leaderboard_result_get_next_taskargs*>(context);

    auto result = args->leaderboard->pImpl->cppLeaderboardResult().get_next(args->maxItems).get();
    
    args->resultErrorMsg = result.err_message();
    args->result.errorCode = utils::xsapi_result_from_xbox_live_result_err(result.err());
    args->result.errorMessage = args->resultErrorMsg.c_str();

    if (!result.err())
    {
        auto cppPayload = result.payload();
        XSAPI_GET_NEXT_RESULT_PAYLOAD& payload = args->completionRoutinePayload.payload;

        auto leaderboardResult = CreateLeaderboardResultFromCpp(cppPayload);

        args->nextResult = leaderboardResult;
        payload.nextResult = args->nextResult;
    }

    return HCTaskSetCompleted(taskHandle);
}

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
LeaderboardResultGetNext(
    _In_ XSAPI_LEADERBOARD_RESULT* leaderboardResult,
    _In_ uint32_t maxItems,
    _In_ GET_NEXT_COMPLETION_ROUTINE completionRoutine,
    _In_opt_ void* completionRoutineContext,
    _In_ uint64_t taskGroupId
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    auto args = new leaderboard_result_get_next_taskargs(
        leaderboardResult,
        maxItems
    );

    return utils::xsapi_result_from_hc_result(HCTaskCreate(
        taskGroupId,
        LeaderboardResultGetNextExecute,
        static_cast<void*>(args),
        utils::execute_completion_routine_with_payload<leaderboard_result_get_next_taskargs, GET_NEXT_COMPLETION_ROUTINE>,
        static_cast<void*>(args),
        static_cast<void*>(completionRoutine),
        completionRoutineContext,
        nullptr
    ));
}
CATCH_RETURN()
#endif

XSAPI_DLLEXPORT XSAPI_RESULT XBL_CALLING_CONV
LeaderboardResultGetNextQuery(
    _In_ XSAPI_LEADERBOARD_RESULT* leaderboardResult,
    _Out_ XSAPI_LEADERBOARD_QUERY** nextQuery,
    _Out_ PCSTR* errMessage
) XSAPI_NOEXCEPT
try
{
    verify_global_init();

    cppLeaderboardQueryResult = leaderboardResult->pImpl->cppLeaderboardResult().get_next_query();

    *nextQuery = CreateLeaderboardQueryFromCpp(cppLeaderboardQueryResult.payload());

    *errMessage = cppLeaderboardQueryResult.err_message().c_str();
    return utils::xsapi_result_from_xbox_live_result_err(cppLeaderboardQueryResult.err());
}
CATCH_RETURN()