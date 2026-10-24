using System.Collections.Generic;
using UnityEngine;

namespace Game.Paint
{
    public sealed class PaintBlobRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SheepPaintController paintController;
        [SerializeField] private GameObject blobPrefab;

        [Header("Settings")]
        [SerializeField] private float surfaceOffset = 0.01f;
        [SerializeField] private float minSize = 0.15f;
        [SerializeField] private float maxSize = 0.3f;

        private readonly Dictionary<int, GameObject> _blobInstances = new();

        /// <summary>
        /// Находит контроллер покраски, если он не был назначен через Inspector.
        /// </summary>
        private void Awake()
        {
            if (paintController == null)
            {
                paintController =
                    GetComponent<SheepPaintController>();
            }
        }

        /// <summary>
        /// Подписывается на создание и удаление временных объектов пятен.
        /// </summary>
        private void OnEnable()
        {
            if (paintController != null)
            {
                paintController.BlobCreated += OnBlobCreated;
                paintController.BlobRemoved += OnBlobRemoved;
            }
        }

        /// <summary>
        /// Отписывается от событий контроллера при отключении рендерера.
        /// </summary>
        private void OnDisable()
        {
            if (paintController != null)
            {
                paintController.BlobCreated -= OnBlobCreated;
                paintController.BlobRemoved -= OnBlobRemoved;
            }
        }

        /// <summary>
        /// Создаёт временный визуальный объект для нового пятна.
        /// </summary>
        private void OnBlobCreated(
            PaintHit hit,
            PaintBlobData blob)
        {
            CreateBlob(hit, blob);
        }

        /// <summary>
        /// Удаляет временный визуальный объект пятна по его глобальному порядковому номеру.
        /// </summary>
        private void OnBlobRemoved(
            PaintBlobData blob)
        {
            if (!_blobInstances.TryGetValue(blob.Sequence, out GameObject instance))
            {
                return;
            }

            _blobInstances.Remove(blob.Sequence);

            if (instance != null)
            {
                Destroy(instance);
            }
        }

        /// <summary>
        /// Создаёт и настраивает временный объект пятна на поверхности овцы.
        /// </summary>
        private void CreateBlob(
            PaintHit hit,
            PaintBlobData blob)
        {
            if (blobPrefab == null)
            {
                Debug.LogError(
                    "PaintBlobRenderer: Blob Prefab is not assigned."
                );

                return;
            }

            GameObject instance = Instantiate(
                blobPrefab,
                blob.WorldPosition +
                blob.Normal * surfaceOffset,
                Quaternion.identity,
                transform
            );

            Quaternion surfaceRotation =
                Quaternion.LookRotation(blob.Normal);

            Quaternion randomRotation =
                Quaternion.AngleAxis(
                    blob.Rotation,
                    Vector3.forward
                );

            instance.transform.rotation =
                surfaceRotation * randomRotation;

            instance.transform.localScale =
                Vector3.one * blob.Size;

            Renderer renderer =
                instance.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color =
                    blob.Color;
            }

            _blobInstances[blob.Sequence] = instance;
        }
    }
}
