using UnityEngine;

namespace ProjectScare.Core
{
    [CreateAssetMenu(menuName = "ProjectScare/Buses/Host Game Request Bus")]
    public class HostGameRequestBus : EventBus<HostGameRequest>
    {
    }
}
