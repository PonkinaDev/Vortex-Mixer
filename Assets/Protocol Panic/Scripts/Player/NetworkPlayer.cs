using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Hold")]
    [SerializeField] private Transform _holdPoint;

    [Header("Interaction")]
    [SerializeField] private float _interactionRadius = 2f;

    private CharacterController _cc;

    [Networked]
    private Vector3 NetworkedPosition { get; set; }

    [Networked]
    private Quaternion NetworkedRotation { get; set; }

    [Networked]
    public IngredientType HeldIngredient { get; set; }

    [Networked]
    public PotionState HeldPotionState { get; set; }

    private GameObject _heldVisual;

    private IngredientType _lastVisualIngredient =
        IngredientType.None;

    private PotionState _lastVisualPotionState =
        PotionState.Raw;

    private IngredientDispenser _nearbyDispenser;

    private PotionMixer _nearbyMixer;

    private PotionCauldron _nearbyCauldron;

    private DeliveryZone _nearbyDeliveryZone;

    private TrashBin _nearbyTrashBin;

    private float _pickupCooldown = 0f;

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        UpdateHeldVisual();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!GetInput(out PlayerInputData input))
            return;

        Vector3 direction = new Vector3(
            input.MovementInput.x,
            0f,
            input.MovementInput.y
        );

        direction.Normalize();

        Vector3 movement =
            direction *
            _moveSpeed *
            Runner.DeltaTime;

        _cc.Move(movement);

        NetworkedPosition = transform.position;

        if (direction != Vector3.zero)
        {
            NetworkedRotation =
                Quaternion.LookRotation(direction);
        }

        DetectNearbyDispenser();

        DetectNearbyMixer();

        DetectNearbyCauldron();

        DetectNearbyDeliveryZone();

        DetectNearbyTrashBin();

        _pickupCooldown -= Runner.DeltaTime;

        if (input.PickupPressed &&
            _pickupCooldown <= 0f)
        {
            _pickupCooldown = 0.25f;

            Interact();
        }
    }

    public override void Render()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            NetworkedPosition,
            Runner.DeltaTime * 15f
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            NetworkedRotation,
            Runner.DeltaTime * 15f
        );

        UpdateHeldVisual();
    }

    private void DetectNearbyDispenser()
    {
        _nearbyDispenser = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                _interactionRadius
            );

        float closestDistance = 999f;

        foreach (Collider hit in hits)
        {
            IngredientDispenser dispenser =
                hit.GetComponent<IngredientDispenser>();

            if (dispenser == null)
                continue;

            float dist =
                Vector3.Distance(
                    transform.position,
                    dispenser.transform.position
                );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                _nearbyDispenser = dispenser;
            }
        }
    }

    private void DetectNearbyMixer()
    {
        _nearbyMixer = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                _interactionRadius
            );

        float closestDistance = 999f;

        foreach (Collider hit in hits)
        {
            PotionMixer mixer =
                hit.GetComponent<PotionMixer>();

            if (mixer == null)
                continue;

            float dist =
                Vector3.Distance(
                    transform.position,
                    mixer.transform.position
                );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                _nearbyMixer = mixer;
            }
        }
    }

    private void DetectNearbyCauldron()
    {
        _nearbyCauldron = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                _interactionRadius
            );

        foreach (Collider hit in hits)
        {
            PotionCauldron cauldron =
                hit.GetComponent<PotionCauldron>();

            if (cauldron != null)
            {
                _nearbyCauldron = cauldron;
                return;
            }
        }
    }

    private void DetectNearbyDeliveryZone()
    {
        _nearbyDeliveryZone = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                _interactionRadius
            );

        foreach (Collider hit in hits)
        {
            DeliveryZone zone =
                hit.GetComponent<DeliveryZone>();

            if (zone != null)
            {
                _nearbyDeliveryZone = zone;
                return;
            }
        }
    }

    private void DetectNearbyTrashBin()
    {
        _nearbyTrashBin = null;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                _interactionRadius
            );

        foreach (Collider hit in hits)
        {
            TrashBin trash =
                hit.GetComponent<TrashBin>();

            if (trash != null)
            {
                _nearbyTrashBin = trash;
                return;
            }
        }
    }

    private void Interact()
    {
        if (_nearbyTrashBin != null)
        {
            if (HeldIngredient !=
                IngredientType.None)
            {
                ClearIngredient();
            }

            return;
        }

        if (_nearbyDeliveryZone != null)
        {
            if (HeldIngredient !=
                IngredientType.None)
            {
                if (OrderManager.Instance != null)
                {
                    bool success =
                        OrderManager.Instance.TryDeliver(
                            HeldIngredient,
                            HeldPotionState
                        );

                    if (success)
                    {
                        ClearIngredient();
                    }
                }
            }

            return;
        }

        if (_nearbyCauldron != null)
        {
            if (HeldIngredient ==
                IngredientType.None)
            {
                IngredientType potion;
                PotionState state;

                bool success =
                    _nearbyCauldron.TryTakePotion(
                        out potion,
                        out state
                    );

                if (success)
                {
                    HeldIngredient =
                        potion;

                    HeldPotionState =
                        state;
                }

                return;
            }

            bool placed =
                _nearbyCauldron.TryAddPotion(
                    HeldIngredient
                );

            if (placed)
            {
                ClearIngredient();
            }

            return;
        }

        if (_nearbyMixer != null)
        {
            if (HeldIngredient ==
                IngredientType.None)
            {
                if (_nearbyMixer.CurrentColor !=
                    IngredientType.None)
                {
                    HeldIngredient =
                        _nearbyMixer.CurrentColor;

                    HeldPotionState =
                        PotionState.Raw;

                    _nearbyMixer.CurrentColor =
                        IngredientType.None;
                }

                return;
            }

            bool success =
                _nearbyMixer.TryAddIngredient(
                    HeldIngredient
                );

            if (success)
            {
                ClearIngredient();
            }

            return;
        }

        if (_nearbyDispenser != null)
        {
            HeldIngredient =
                _nearbyDispenser.IngredientType;

            HeldPotionState =
                PotionState.Raw;
        }
    }

    public bool HasIngredient()
    {
        return HeldIngredient != IngredientType.None;
    }

    public void ClearIngredient()
    {
        if (!HasStateAuthority)
            return;

        HeldIngredient =
            IngredientType.None;

        HeldPotionState =
            PotionState.Raw;
    }

    private void UpdateHeldVisual()
    {
        if (_lastVisualIngredient ==
            HeldIngredient &&
            _lastVisualPotionState ==
            HeldPotionState)
            return;

        _lastVisualIngredient =
            HeldIngredient;

        _lastVisualPotionState =
            HeldPotionState;

        if (_heldVisual != null)
        {
            Destroy(_heldVisual);
        }

        if (HeldIngredient ==
            IngredientType.None)
            return;

        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        visual.transform.SetParent(
            _holdPoint
        );

        visual.transform.localPosition =
            Vector3.zero;

        visual.transform.localRotation =
            Quaternion.identity;

        visual.transform.localScale =
            Vector3.one * 0.4f;

        Collider col =
            visual.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        Renderer renderer =
            visual.GetComponent<Renderer>();

        Material mat =
            new Material(
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
            );

        renderer.material = mat;

        switch (HeldIngredient)
        {
            case IngredientType.Red:
                renderer.material.color =
                    Color.red;
                break;

            case IngredientType.Blue:
                renderer.material.color =
                    Color.blue;
                break;

            case IngredientType.Yellow:
                renderer.material.color =
                    Color.yellow;
                break;

            case IngredientType.Green:
                renderer.material.color =
                    Color.green;
                break;

            case IngredientType.Orange:
                renderer.material.color =
                    new Color(1f, 0.5f, 0f);
                break;

            case IngredientType.Purple:
                renderer.material.color =
                    new Color(0.5f, 0f, 1f);
                break;
        }

        if (HeldPotionState ==
            PotionState.Cooked)
        {
            renderer.material.color *= 0.7f;
        }

        if (HeldPotionState ==
            PotionState.Burned)
        {
            renderer.material.color =
                Color.black;
        }

        _heldVisual = visual;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            _interactionRadius
        );
    }
}