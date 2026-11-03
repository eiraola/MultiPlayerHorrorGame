using UnityEngine;

namespace ProjectScare.Core
{
    [CreateAssetMenu(menuName = "ProjectScare/Buses/Join Game Request Bus")]
    public class JoinGameRequestBus : EventBus<JoinGameRequest>
    {
    }
}
