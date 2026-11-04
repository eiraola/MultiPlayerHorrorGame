using UnityEngine;

namespace ProjectScare.Core
{
    [CreateAssetMenu(menuName = "ProjectScare/Buses/Room List Bus")]
    public class RoomListBus : EventBus<RoomSummary[]>
    {
    }
}
