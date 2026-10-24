using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private bool startWithPaintGun = true;

        public bool IsPaintGunEquipped { get; private set; }

        public event Action<bool> EquipmentChanged;

        private void Awake()
        {
            IsPaintGunEquipped = startWithPaintGun;
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                SetPaintGunEquipped(!IsPaintGunEquipped);
            }
        }

        public void SetPaintGunEquipped(bool equipped)
        {
            if (IsPaintGunEquipped == equipped)
            {
                return;
            }

            IsPaintGunEquipped = equipped;
            EquipmentChanged?.Invoke(IsPaintGunEquipped);
        }
    }
}
