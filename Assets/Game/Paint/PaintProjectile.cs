using UnityEngine;

using Game.Audio;
using Game.VFX;

namespace Game.Paint
{
    public sealed class PaintProjectile : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [Header("Movement")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float maxFlightDistance = 50f;

        [Header("Hit Detection")]
        [SerializeField] private float castRadius = 0.08f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [Tooltip("Новый Cue взрыва ягоды, запускаемый через общий VFX-сервис.")]
        [SerializeField] private ParticleEffectCue impactEffectCue;
        [Tooltip("Общий сервис визуальных эффектов. Если не задан, ищется один раз при столкновении.")]
        [SerializeField] private ParticleEffectService particleEffectService;
        [Tooltip("Старый prefab эффекта столкновения. Используется как резервный вариант во время миграции.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField, Min(0.1f)] private float impactEffectLifetime = 2f;

        [Header("Flight VFX")]
        [Tooltip("Cue следа, который прикрепляется к ягоде на время полёта.")]
        [SerializeField] private ParticleEffectCue flightEffectCue;

        [Header("Audio")]
        [Tooltip("Необязательный звук взрыва ягоды при столкновении с поверхностью.")]
        [SerializeField] private AudioCue impactCue;
        [Tooltip("Общий сервис воспроизведения звуков. Если не задан, ищется один раз при попадании.")]
        [SerializeField] private AudioService audioService;

        [Header("Visual")]
        [Tooltip("Renderer летящей ягоды. Если список пуст, Renderer ищутся внутри префаба.")]
        [SerializeField] private Renderer[] visualRenderers;

        private float _lifeTimer;
        private float _currentSpeed;
        private Vector3 _velocity;
        private Vector3 _launchPosition;
        private Color _color;
        private Vector3 _sourcePosition;
        private ParticleSystem _flightEffect;
        private MaterialPropertyBlock _propertyBlock;

        private static readonly RaycastHit[] HitBuffer =
            new RaycastHit[16];

        /// <summary>
        /// Находит Renderer летящей ягоды и подготавливает переиспользуемый блок свойств материала.
        /// </summary>
        private void Awake()
        {
            if (visualRenderers == null ||
                visualRenderers.Length == 0)
            {
                visualRenderers =
                    GetComponentsInChildren<Renderer>(true);
            }

            _propertyBlock =
                new MaterialPropertyBlock();
        }

        /// <summary>
        /// Запускает снаряд с его настроенной в Inspector скоростью.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color)
        {
            Launch(
                direction,
                color,
                transform.position,
                speed
            );
        }

        /// <summary>
        /// Запускает снаряд с указанной позицией источника и стандартной скоростью prefab.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color,
            Vector3 sourcePosition)
        {
            Launch(
                direction,
                color,
                sourcePosition,
                speed
            );
        }

        /// <summary>
        /// Запускает снаряд с указанной скоростью, цветом и позицией источника попадания.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color,
            Vector3 sourcePosition,
            float launchSpeed)
        {
            StopFlightEffect();

            Vector3 safeDirection =
                direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : transform.forward;

            transform.forward = safeDirection;

            _currentSpeed =
                Mathf.Max(0f, launchSpeed);
            _velocity =
                safeDirection * _currentSpeed;
            _launchPosition =
                transform.position;
            _color = color;
            _sourcePosition = sourcePosition;
            _lifeTimer = lifeTime;

            ApplyColor(_color);

            StartFlightEffect();
        }

        /// <summary>
        /// Применяет цвет выбранной ягоды к Renderer снаряда без создания нового материала.
        /// </summary>
        private void ApplyColor(
            Color color)
        {
            if (visualRenderers == null ||
                _propertyBlock == null)
            {
                return;
            }

            for (int index = 0; index < visualRenderers.Length; index++)
            {
                Renderer visualRenderer =
                    visualRenderers[index];

                if (visualRenderer == null)
                {
                    continue;
                }

                visualRenderer.GetPropertyBlock(
                    _propertyBlock);

                _propertyBlock.SetColor(
                    BaseColorId,
                    color);

                _propertyBlock.SetColor(
                    ColorId,
                    color);

                visualRenderer.SetPropertyBlock(
                    _propertyBlock);
            }
        }

        /// <summary>
        /// Перемещает снаряд, проверяет столкновение и удаляет его после истечения времени жизни.
        /// </summary>
        private void Update()
        {
            float deltaTime =
                Time.deltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 nextVelocity =
                _velocity +
                Physics.gravity *
                gravityMultiplier *
                deltaTime;

            Vector3 displacement =
                (_velocity + nextVelocity) *
                (0.5f * deltaTime);

            float distance =
                displacement.magnitude;

            Vector3 origin = transform.position;
            Vector3 direction =
                distance > 0.0001f
                    ? displacement / distance
                    : transform.forward;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                castRadius,
                direction,
                HitBuffer,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

            if (TryGetNearestCollision(hitCount, out RaycastHit hit))
            {
                HandleHit(hit);
                return;
            }

            transform.position += direction * distance;
            _velocity = nextVelocity;

            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward =
                    _velocity.normalized;
            }

            _lifeTimer -= deltaTime;

            bool exceededLifeTime =
                _lifeTimer <= 0f;

            bool exceededFlightDistance =
                (transform.position - _launchPosition).sqrMagnitude >=
                maxFlightDistance * maxFlightDistance;

            if (exceededLifeTime || exceededFlightDistance)
            {
                StopFlightEffect();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Передаёт подтверждённое попадание найденному получателю краски и удаляет снаряд.
        /// </summary>
        private void HandleHit(RaycastHit hit)
        {
            StopFlightEffect();
            PlayImpactAudio(hit.point);
            SpawnImpactEffect(hit);

            IPaintReceiver receiver =
                hit.collider.GetComponentInParent(
                    typeof(IPaintReceiver)
                ) as IPaintReceiver;

            if (receiver == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 paintUV =
                hit.collider is MeshCollider
                    ? hit.textureCoord2
                    : Vector2.zero;

            PaintImpactData impact =
                new PaintImpactData(
                    hit.point,
                    hit.normal,
                    paintUV,
                    _color,
                    _sourcePosition,
                    hit.collider
                );

            receiver.ReceivePaint(
                impact
            );

            Destroy(gameObject);
        }

        /// <summary>
        /// Запускает след как дочерний эффект снаряда после начала его полёта.
        /// </summary>
        private void StartFlightEffect()
        {
            if (flightEffectCue == null)
            {
                return;
            }

            ResolveParticleEffectService();

            if (particleEffectService == null)
            {
                return;
            }

            _flightEffect =
                particleEffectService.PlayAttached(
                    flightEffectCue,
                    transform);
        }

        /// <summary>
        /// Останавливает след и возвращает его экземпляр в пул перед завершением полёта.
        /// </summary>
        private void StopFlightEffect()
        {
            if (_flightEffect == null)
            {
                return;
            }

            ResolveParticleEffectService();

            if (particleEffectService != null)
            {
                particleEffectService.Stop(_flightEffect);
            }

            _flightEffect = null;
        }

        /// <summary>
        /// Останавливает дочерний след при отключении или уничтожении снаряда.
        /// </summary>
        private void OnDisable()
        {
            StopFlightEffect();
        }

        /// <summary>
        /// Воспроизводит звук взрыва ягоды в точке подтверждённого столкновения.
        /// </summary>
        private void PlayImpactAudio(
            Vector3 impactPosition)
        {
            if (impactCue == null)
            {
                return;
            }

            ResolveAudioService();

            if (audioService == null)
            {
                return;
            }

            audioService.TryPlay(
                impactCue,
                impactPosition
            );
        }

        /// <summary>
        /// Находит AudioService в родителях или сцене, если он не задан вручную.
        /// </summary>
        private void ResolveAudioService()
        {
            if (audioService != null)
            {
                return;
            }

            audioService = GetComponentInParent<AudioService>();

            if (audioService == null)
            {
                audioService = FindFirstObjectByType<AudioService>();
            }
        }

        /// <summary>
        /// Создаёт необязательный эффект столкновения и удаляет его после короткого времени жизни.
        /// </summary>
        private bool SpawnImpactEffect(
            RaycastHit hit)
        {
            Vector3 normal =
                hit.normal.sqrMagnitude > 0.0001f
                    ? hit.normal.normalized
                    : Vector3.up;

            if (impactEffectCue != null)
            {
                ResolveParticleEffectService();

                if (particleEffectService != null)
                {
                    ParticleSystem particleEffect =
                        particleEffectService.Play(
                            impactEffectCue,
                            hit.point,
                            Quaternion.LookRotation(normal),
                            _color);

                    if (particleEffect != null)
                    {
                        return true;
                    }
                }
            }

            if (impactEffectPrefab == null)
            {
                return false;
            }

            GameObject effect =
                Instantiate(
                    impactEffectPrefab,
                    hit.point,
                    Quaternion.LookRotation(normal)
                );

            Destroy(
                effect,
                impactEffectLifetime
            );

            return true;
        }

        /// <summary>
        /// Находит ParticleEffectService в родителях или сцене, если он не назначен вручную.
        /// </summary>
        private void ResolveParticleEffectService()
        {
            if (particleEffectService != null)
            {
                return;
            }

            particleEffectService =
                GetComponentInParent<ParticleEffectService>();

            if (particleEffectService == null)
            {
                particleEffectService =
                    FindFirstObjectByType<ParticleEffectService>();
            }
        }

        /// <summary>
        /// Находит ближайший подходящий Collider среди результатов SphereCast.
        /// </summary>
        private static bool TryGetNearestCollision(
            int hitCount,
            out RaycastHit surfaceHit)
        {
            surfaceHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = HitBuffer[i];

                if (candidate.collider == null)
                {
                    continue;
                }

                if (candidate.collider.GetComponentInParent<PaintShooter>() != null)
                {
                    continue;
                }

                if (candidate.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = candidate.distance;
                surfaceHit = candidate;
                found = true;
            }

            return found;
        }
    }
}
