using UnityEngine;

using Game.Paint;
using Game.Sheep.AI;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerSheepInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PaintShooter paintShooter;
        [SerializeField] private Camera playerCamera;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private void Awake()
        {
            if (paintShooter == null)
            {
                paintShooter = GetComponent<PaintShooter>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void OnEnable()
        {
            if (paintShooter != null)
            {
                paintShooter.UnarmedPrimaryAction += TryPetSheep;
            }
        }

        private void OnDisable()
        {
            if (paintShooter != null)
            {
                paintShooter.UnarmedPrimaryAction -= TryPetSheep;
            }
        }

        private void TryPetSheep()
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            SheepBrain sheep = hit.collider.GetComponentInParent<SheepBrain>();

            if (sheep != null)
            {
                sheep.TryPet(transform);
            }
        }
    }
}
