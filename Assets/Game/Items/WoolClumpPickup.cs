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

        private SphereCollider _pickupCollider;
        private Renderer[] _visualRenderers;
        private MaterialPropertyBlock _propertyBlock;

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
