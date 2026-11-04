using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace ProjectScare.Network
{
    // Lists the public rooms through the Lobby REST API. Private rooms never show up in queries.
    public sealed class RoomListController : MonoBehaviour
    {
        private const string FailureMessage = "Could not load the rooms. Please try again.";
        private const int MaxRooms = 25;

        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);

        [SerializeField] private SessionServices sessionServices;
        [SerializeField] private StringEventBus roomListRequestBus;
        [SerializeField] private RoomListBus roomListBus;
        [SerializeField] private StringEventBus roomListFailedBus;

        // Only the latest request publishes, so an older answer cannot replace the list of the region now selected.
        private int latestRequest;

        private void Awake() => latestRequest = 0;

        private void OnEnable() => roomListRequestBus.Raised += HandleRoomListRequested;

        private void OnDisable() => roomListRequestBus.Raised -= HandleRoomListRequested;

        // A null region lists the rooms of every region.
        private void HandleRoomListRequested(string region) =>
            QueryRoomsAsync(region, ++latestRequest, destroyCancellationToken).Forget();

        private async UniTaskVoid QueryRoomsAsync(string region, int request, CancellationToken destroyToken)
        {
            using var queryCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = queryCts.CancelAfterSlim(QueryTimeout, DelayType.UnscaledDeltaTime);

            RoomSummary[] rooms;

            try
            {
                await sessionServices.EnsureSignedInAsync(queryCts.Token);

                QueryResponse response = await LobbyService.Instance
                    .QueryLobbiesAsync(CreateQueryOptions(region))
                    .AsUniTask()
                    .AttachExternalCancellation(queryCts.Token);

                rooms = response.Results.ConvertAll(ToRoomSummary).ToArray();
            }
            catch (Exception e)
            {
                if (destroyToken.IsCancellationRequested || request != latestRequest)
                    return;

                Debug.LogError($"Listing the rooms failed: {SessionServices.DescribeFailure(e)}", this);
                roomListFailedBus.Raise(FailureMessage);
                return;
            }

            if (request == latestRequest)
                roomListBus.Raise(rooms);
        }

        private static QueryLobbiesOptions CreateQueryOptions(string region)
        {
            var options = new QueryLobbiesOptions
            {
                Count = MaxRooms,
                Order = new List<QueryOrder> { new(asc: false, QueryOrder.FieldOptions.Created) },
            };

            if (!string.IsNullOrEmpty(region))
                options.Filters = new List<QueryFilter> { new(QueryFilter.FieldOptions.S1, region, QueryFilter.OpOptions.EQ) };

            return options;
        }

        private static RoomSummary ToRoomSummary(Lobby lobby) =>
            new(
                lobby.Id,
                lobby.Name,
                ReadData(lobby, SessionServices.OwnerNameKey),
                ReadData(lobby, SessionServices.RegionKey),
                lobby.MaxPlayers - lobby.AvailableSlots,
                lobby.MaxPlayers,
                lobby.HasPassword);

        // Rooms created before these fields existed have no data, so they show empty values.
        private static string ReadData(Lobby lobby, string key) =>
            lobby.Data != null && lobby.Data.TryGetValue(key, out DataObject data) ? data.Value : string.Empty;
    }
}
