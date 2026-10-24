using System;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Player;

namespace Game.Paint
{
    public sealed class PaintShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PaintProjectile projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private PlayerEquipment equipment;

        [Header("Paint")]
        [SerializeField] private Color paintColor = Color.red;

        private PlayerInputActions _inputActions;

        public event Action UnarmedPrimaryAction;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();

            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Shoot.performed += OnShoot;
        }

        private void OnDisable()
        {
            _inputActions.Player.Shoot.performed -= OnShoot;
            _inputActions.Player.Disable();
        }

        private void OnShoot(InputAction.CallbackContext context)
        {
            if (equipment != null && !equipment.IsPaintGunEquipped)
            {
                UnarmedPrimaryAction?.Invoke();
                return;
            }

            Shoot();
        }

        private void Shoot()
        {
            PaintProjectile projectile =
                Instantiate(
                    projectilePrefab,
                    projectileSpawnPoint.position,
                    Quaternion.identity
                );

            projectile.Launch(
                playerCamera.transform.forward,
                paintColor,
                transform.position
            );
        }
    }
}
