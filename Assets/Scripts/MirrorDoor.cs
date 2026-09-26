using UnityEngine;

namespace GemRush
{
    /// A pair of mirror doors: walk into one, exit from the twin. The twin
    /// ignores you until you step out of it, so the doors never ping-pong.
    /// Both twins look identical — in the Mirror Skies you can't tell which
    /// side of the glass you're on. That's the point of them.
    public class MirrorDoor : MonoBehaviour
    {
        class DoorSide : MonoBehaviour
        {
            public MirrorDoor door;
            public DoorSide twin;
            public Transform exitAnchor;
            public bool playerInside;

            void OnTriggerEnter(Collider other)
            {
                PlayerController player =
                    other.GetComponentInParent<PlayerController>();
                if (player == null) return;
                bool wasInside = playerInside;
                playerInside = true;
                if (wasInside || twin.playerInside) return; // just arrived here
                door.Teleport(player, exitAnchor.position);
            }

            void OnTriggerExit(Collider other)
            {
                if (other.GetComponentInParent<PlayerController>() == null) return;
                playerInside = false;
            }
        }

        Transform sideA;
        Transform sideB;
        DoorSide doorA;
        DoorSide doorB;
        Material paneMat;
        Material paneMatB; // twin's pane, shimmering out of phase
        float shimmer;

        /// Builds a linked pair. Positions are the door bases (floor level).
        public static void Create(Transform parent, Vector3 a, Vector3 b)
        {
            GameObject go = new GameObject("MirrorDoor");
            go.transform.SetParent(parent, false);

            MirrorDoor door = go.AddComponent<MirrorDoor>();

            // Mirror architecture, not generic stone: the cooler violet-
            // shifted tint separates a magic doorway from the bell posts,
            // checkpoint posts and spinner pedestals that all share
            // plain ArtLib.Stone.
            Material frame = ArtLib.Solid(ArtLib.MirrorStone, 0f);
            Material gold = ArtLib.Solid(ArtLib.Gold, 0.5f);
            // Per-side pane materials: the twins shimmer OUT OF PHASE —
            // sharing one material made them pulse in perfect sync (the
            // doc comment promised phase, the code delivered a chorus).
            Material paneA = ArtLib.Solid(ArtLib.Air, 0.3f);
            ArtLib.SetFade(paneA, 0.4f);
            Material paneB = ArtLib.Solid(ArtLib.Air, 0.3f);
            ArtLib.SetFade(paneB, 0.4f);
            door.paneMat = paneA; // the Update pulse drives pane A
            door.paneMatB = paneB;

            door.sideA = BuildSide(go.transform, a, frame, gold, paneA);
            door.sideB = BuildSide(go.transform, b, frame, gold, paneB);

            door.doorA = door.sideA.GetComponentInChildren<DoorSide>();
            door.doorB = door.sideB.GetComponentInChildren<DoorSide>();
            door.doorA.door = door;
            door.doorB.door = door;
            door.doorA.twin = door.doorB;
            door.doorB.twin = door.doorA;
            door.doorA.exitAnchor = door.sideB;
            door.doorB.exitAnchor = door.sideA;
        }

        static Transform BuildSide(Transform parent, Vector3 position,
            Material frame, Material gold, Material pane)
        {
            GameObject side = new GameObject("Side");
            side.transform.SetParent(parent, false);
            side.transform.localPosition = position;

            // Door frame: two pillars and a lintel.
            ArtLib.DecorCube(side.transform, new Vector3(-0.85f, 1.5f, 0f),
                new Vector3(0.3f, 3f, 0.35f), Quaternion.identity, frame);
            ArtLib.DecorCube(side.transform, new Vector3(0.85f, 1.5f, 0f),
                new Vector3(0.3f, 3f, 0.35f), Quaternion.identity, frame);
            ArtLib.DecorCube(side.transform, new Vector3(0f, 3.1f, 0f),
                new Vector3(2.1f, 0.35f, 0.4f), Quaternion.identity, frame);
            // A gold keystone: these doors matter. Protrudes -Z (toward
            // the spawn camera), not +Z — the camera never sees +Z from
            // the approach angle, so the accent was invisible in play.
            ArtLib.DecorCube(side.transform, new Vector3(0f, 3.1f, -0.22f),
                new Vector3(0.4f, 0.4f, 0.12f), Quaternion.identity, gold);

            // The mirror pane.
            ArtLib.DecorCube(side.transform, new Vector3(0f, 1.5f, 0f),
                new Vector3(1.35f, 2.7f, 0.08f), Quaternion.identity, pane);

            // Trigger column.
            GameObject triggerGo = new GameObject("Trigger");
            triggerGo.transform.SetParent(side.transform, false);
            triggerGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            BoxCollider trigger = triggerGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.5f, 2.8f, 1.2f);
            triggerGo.AddComponent<DoorSide>();

            return side.transform;
        }

        void Teleport(PlayerController player, Vector3 exit)
        {
            player.TeleportTo(exit + new Vector3(0f, 0.2f, 1.7f));
            Fx.Burst(exit + new Vector3(0f, 1.2f, 0.8f),
                ArtLib.Air * 1.6f, 26);
            AudioManager.Instance.PlayMirror();
        }

        void Update()
        {
            // The panes shimmer out of phase with each other — twin A
            // breathes in while twin B breathes out, so standing between
            // them reads as a shared pulse splitting and rejoining.
            shimmer += Time.deltaTime;
            float a = 0.35f + Mathf.Sin(shimmer * 1.8f) * 0.12f;
            float b = 0.35f + Mathf.Sin(shimmer * 1.8f + Mathf.PI) * 0.12f;
            if (paneMat != null)
            {
                Color c = paneMat.color;
                c.a = a;
                paneMat.color = c;
            }
            if (paneMatB != null)
            {
                Color c = paneMatB.color;
                c.a = b;
                paneMatB.color = c;
            }
        }
    }
}
