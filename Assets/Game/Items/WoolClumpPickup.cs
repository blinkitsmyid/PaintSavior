using System.Collections;

using UnityEngine;

using Game.Sheep.AI;

namespace Game.Items
{
    /// <summary>
    /// Хранит данные выпавшего клочка шерсти и подготавливает его визуал и Trigger-зону к подбору.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class WoolClumpPickup : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [Header("Wool Data")]
        [SerializeField] private WoolGrowthStage sourceStage =
            WoolGrowthStage.Full;

        [SerializeField] private Color woolColor = Color.white;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;

        [Header("Pickup")]
        [Tooltip("Скорость притягивания клочка к игроку после входа в Trigger.")]
        [SerializeField, Min(0.01f)] private float attractionSpeed = 8f;

        [Tooltip("Расстояние до точки игрока, при котором клочок считается собранным.")]
        [SerializeField, Min(0.01f)] private float collectionDistance = 0.1f;

        private SphereCollider _pickupCollider;
        private Renderer[] _visualRenderers;
        private MaterialPropertyBlock _propertyBlock;
        private bool _isCollecting;

        /// <summary>
        /// Возвращает стадию шерсти, из которой был создан клочок.
        /// </summary>
        public WoolGrowthStage SourceStage => sourceStage;

        /// <summary>
        /// Возвращает цвет шерсти, который должен отображаться на клочке.
        /// </summary>
        public Color WoolColor => woolColor;

        /// <summary>
        /// Находит визуальные компоненты и подготавливает Trigger-коллайдер клочка.
        /// </summary>
        private void Awake()
        {
            _pickupCollider =
                GetComponent<SphereCollider>();

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _visualRenderers =
                visualRoot.GetComponentsInChildren<Renderer>(true);

            _propertyBlock =
                new MaterialPropertyBlock();

            ConfigurePickupCollider();
            ApplyColor();
        }

        /// <summary>
        /// Поддерживает Trigger-настройку и ссылку на визуальный корень при изменении префаба в Inspector.
        /// </summary>
        private void OnValidate()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (_pickupCollider == null)
            {
                _pickupCollider =
                    GetComponent<SphereCollider>();
            }

            ConfigurePickupCollider();

            attractionSpeed =
                Mathf.Max(0.01f, attractionSpeed);
            collectionDistance =
                Mathf.Max(0.01f, collectionDistance);
        }

        /// <summary>
        /// Инициализирует стадию и цвет клочка перед его использованием спавнером.
        /// </summary>
        public void Initialize(
            WoolGrowthStage stage,
            Color color)
        {
            sourceStage = stage;
            woolColor = color;
            ApplyColor();
        }

        /// <summary>
        /// Начинает притягивание клочка к игроку, если в его иерархии найден WoolInventory.
        /// </summary>
        private void OnTriggerEnter(
            Collider other)
        {
            if (_isCollecting || other == null)
            {
                return;
            }

            WoolInventory inventory =
                other.GetComponentInParent<WoolInventory>();

            if (inventory == null)
            {
                return;
            }

            _isCollecting = true;

            if (_pickupCollider != null)
            {
                _pickupCollider.enabled = false;
            }

            StartCoroutine(
                CollectToInventory(
                    inventory
                )
            );
        }

        /// <summary>
        /// Перемещает клочок к игроку, добавляет его цвет в инвентарь и удаляет объект после сбора.
        /// </summary>
        private IEnumerator CollectToInventory(
            WoolInventory inventory)
        {
            if (inventory == null)
            {
                ResetCollectionState();
                yield break;
            }

            Transform target =
                inventory.transform;

            while (target != null)
            {
                Vector3 targetPosition =
                    target.position;

                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        attractionSpeed * Time.deltaTime
                    );

                float distance =
                    Vector3.Distance(
                        transform.position,
                        targetPosition
                    );

                if (distance <= collectionDistance)
                {
                    bool added =
                        inventory.TryAddWool(woolColor);

                    if (added)
                    {
                        Destroy(gameObject);
                        yield break;
                    }

                    ResetCollectionState();
                    yield break;
                }

                yield return null;
            }

            ResetCollectionState();
        }

        /// <summary>
        /// Возвращает клочок в состояние доступного подбора, если цель исчезла или сбор не завершился.
        /// </summary>
        private void ResetCollectionState()
        {
            _isCollecting = false;

            if (_pickupCollider != null)
            {
                _pickupCollider.enabled = true;
            }
        }

        /// <summary>
        /// Включает режим Trigger у зоны подбора, не меняя заданный дизайнером радиус.
        /// </summary>
        private void ConfigurePickupCollider()
        {
            if (_pickupCollider == null)
            {
                return;
            }

            _pickupCollider.isTrigger = true;
        }

        /// <summary>
        /// Передаёт цвет клочка всем Renderer визуальной модели через MaterialPropertyBlock.
        /// </summary>
        private void ApplyColor()
        {
            if (_visualRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                Renderer visualRenderer =
                    _visualRenderers[i];

                if (visualRenderer == null)
                {
                    continue;
                }

                visualRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, woolColor);
                _propertyBlock.SetColor(ColorId, woolColor);
                visualRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
