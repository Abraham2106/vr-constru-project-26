using System.Collections.Generic;
using UnityEngine;

namespace ProyectoVR
{
    /// <summary>
    /// Catálogo de habitaciones pendientes de cablear (puerta/ventana/interruptor).
    /// </summary>
    [CreateAssetMenu(fileName = "InteractablesRoomCatalog", menuName = "ProyectoVR/Room Catalog")]
    public class InteractablesRoomCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct RoomSlot
        {
            public string roomId;
            public bool hasDoor;
            public bool hasWindow;
            public bool hasLightSwitch;
            public Vector3 suggestedAnchor;
        }

        public List<RoomSlot> rooms = new()
        {
            new RoomSlot { roomId = "Apt1_Sala", suggestedAnchor = new Vector3(0f, 0f, 0f) },
            new RoomSlot { roomId = "Apt1_Cocina", suggestedAnchor = new Vector3(3f, 0f, 0f) },
            new RoomSlot { roomId = "Apt1_Dormitorio", suggestedAnchor = new Vector3(6f, 0f, 0f) },
            new RoomSlot { roomId = "Apt2_Sala", suggestedAnchor = new Vector3(0f, 0f, 4f) },
            new RoomSlot { roomId = "Apt2_Planta", suggestedAnchor = new Vector3(3f, 0f, 4f) },
            new RoomSlot { roomId = "Exterior_Patio", suggestedAnchor = new Vector3(-3f, 0f, 2f) }
        };

        public IEnumerable<RoomSlot> Unwired()
        {
            foreach (var room in rooms)
            {
                if (!room.hasDoor || !room.hasWindow || !room.hasLightSwitch)
                    yield return room;
            }
        }
    }
}
