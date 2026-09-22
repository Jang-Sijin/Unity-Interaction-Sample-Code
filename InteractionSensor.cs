using System.Collections.Generic;
using UnityEngine;

namespace BFX
{
    /// <summary>
    /// 상호작용 주체가 가진 감지 센서입니다.
    /// Trigger Collider로 주변 객체의 상호작용 타입과 ID를 확인한 뒤 UI 컨텍스트를 생성합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class InteractionSensor : MonoBehaviour
    {
        private readonly List<InteractionContextData> activeContexts = new();

        private Rigidbody sensorRigidbody;
        private SphereCollider sensorCollider;

        [Header("센서 탐색 설정")]
        [SerializeField] private LayerMask interactableLayer = ~0;
        private static readonly Collider[] overlapResults = new Collider[32];

        public Collider SensorCollider => sensorCollider;

        public void Configure(float radius, LayerMask layerMask)
        {
            interactableLayer = layerMask;

            if (sensorCollider == null)
            {
                sensorCollider = GetComponent<SphereCollider>();
            }

            sensorCollider.radius = Mathf.Max(0f, radius);
        }

        private void Awake()
        {
            sensorRigidbody = GetComponent<Rigidbody>();
            sensorCollider = GetComponent<SphereCollider>();

            sensorRigidbody.isKinematic = true;
            sensorCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            PulseManually();
        }

        private void OnDisable()
        {
            RemoveAllActiveContexts();
        }

        private void OnTriggerEnter(Collider other)
        {
            AddInteractionContext(other);
        }

        private void OnTriggerExit(Collider other)
        {
            RemoveInteractionContext(other);
        }

        public static void RefreshAllActiveSensors(bool republishExisting = false)
        {
            InteractionSensor[] sensors = FindObjectsByType<InteractionSensor>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < sensors.Length; i++)
            {
                sensors[i].PulseManually(republishExisting);
            }
        }

        /// <summary>
        /// 현재 시점에 주변 상호작용 객체를 다시 탐색하고 UI 상태를 동기화합니다.
        /// </summary>
        public void PulseManually(bool republishExisting = false)
        {
            if (sensorCollider == null || !sensorCollider.enabled)
            {
                return;
            }

            List<string> overlappedKeys = new();
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                sensorCollider.radius,
                overlapResults,
                interactableLayer);

            for (int i = 0; i < count; i++)
            {
                Collider detectedCollider = overlapResults[i];
                if (detectedCollider == null || detectedCollider == sensorCollider)
                {
                    continue;
                }

                if (TryBuildContext(detectedCollider, out InteractionContextData context))
                {
                    overlappedKeys.Add(context.ContextKey);
                    AddOrReplaceInteractionContext(context, republishExisting);
                }
            }

            for (int i = activeContexts.Count - 1; i >= 0; i--)
            {
                InteractionContextData context = activeContexts[i];
                if (context == null || !overlappedKeys.Contains(context.ContextKey))
                {
                    RemoveInteractionContextAt(i);
                }
            }
        }

        private void AddInteractionContext(Collider other)
        {
            if (TryBuildContext(other, out InteractionContextData context))
            {
                AddOrReplaceInteractionContext(context);
            }
        }

        private void AddOrReplaceInteractionContext(InteractionContextData context, bool republishExisting = false)
        {
            int existingIndex = FindContextIndex(context.ContextKey);
            if (existingIndex >= 0)
            {
                InteractionContextData existing = activeContexts[existingIndex];
                if (existing.ID == context.ID &&
                    existing.StackCount == context.StackCount &&
                    existing.IsStackable == context.IsStackable)
                {
                    if (republishExisting)
                    {
                        InteractionHandler.NotifyTargetAdded(existing);
                    }

                    return;
                }

                RemoveInteractionContextAt(existingIndex);
            }

            activeContexts.Add(context);
            InteractionHandler.NotifyTargetAdded(context);
        }

        private void RemoveInteractionContext(Collider other)
        {
            if (!TryGetDescriptor(other, out IInteractionTarget descriptor))
            {
                return;
            }

            int index = FindContextIndex(descriptor.InstanceID);
            if (index >= 0)
            {
                RemoveInteractionContextAt(index);
            }
        }

        private void RemoveInteractionContextAt(int index)
        {
            InteractionContextData context = activeContexts[index];
            activeContexts.RemoveAt(index);
            InteractionHandler.NotifyTargetRemoved(context);
        }

        private void RemoveAllActiveContexts()
        {
            for (int i = activeContexts.Count - 1; i >= 0; i--)
            {
                RemoveInteractionContextAt(i);
            }
        }

        private int FindContextIndex(string contextKey)
        {
            for (int i = 0; i < activeContexts.Count; i++)
            {
                if (activeContexts[i].ContextKey == contextKey)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryBuildContext(Collider other, out InteractionContextData context)
        {
            context = null;

            if (!TryGetDescriptor(other, out IInteractionTarget descriptor) ||
                !descriptor.IsInteractable)
            {
                return false;
            }

            context = new InteractionContextData(
                descriptor,
                GetInteractionId(descriptor),
                descriptor.InstanceID,
                descriptor.IsStackable,
                descriptor.StackCount);
            return true;
        }

        private static string GetInteractionId(IInteractionTarget descriptor)
        {
            if (descriptor is null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(descriptor.InteractionId))
                return $"{descriptor.GetInteractionType()}:{descriptor.InteractionId}";

            return descriptor.GetInteractionType().ToString();
        }

        private static bool TryGetDescriptor(Collider other, out IInteractionTarget descriptor)
        {
            descriptor = null;
            if (other == null)
            {
                return false;
            }

            if (other.TryGetComponent(out descriptor))
            {
                return true;
            }

            descriptor = other.GetComponentInParent<IInteractionTarget>();
            return descriptor != null;
        }
    }
}
