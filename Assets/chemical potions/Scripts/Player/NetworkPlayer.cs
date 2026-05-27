using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _moveSpeed = 5f;

    [Header("Systems")]
    [SerializeField]
    private PlayerHeldItemVisual _heldItemVisual;

    [SerializeField]
    private PlayerInteractableDetector _detector;

    [SerializeField]
    private PlayerInteractionHandler _interactionHandler;

    private const float PickupCooldownDuration =
        0.25f;

    private CharacterController _characterController;

    private float _pickupCooldown;

    [Networked]
    private Vector3 NetworkedPosition
    {
        get;
        set;
    }

    [Networked]
    private Quaternion NetworkedRotation
    {
        get;
        set;
    }

    [Networked]
    public IngredientType HeldIngredient
    {
        get;
        set;
    }

    [Networked]
    public PotionState HeldPotionState
    {
        get;
        set;
    }

public override void Spawned()
{
    _characterController = GetComponent<CharacterController>();

    if (HasStateAuthority)
    {
        _characterController.enabled = false;
        transform.position = transform.position; 
        _characterController.enabled = true;

        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
    }

    _interactionHandler.Initialize(this, _detector);
    UpdateVisual();
}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!GetInput(out PlayerInputData input))
            return;

        HandleMovement(input);

        _detector.Detect();

        UpdateCooldown();

        if (CanInteract(input))
            Interact();
    }

    public override void Render()
    {
        SmoothTransform();
        UpdateVisual();
    }

    private void HandleMovement(
        PlayerInputData input
    )
    {
        Vector3 direction = new(
            input.MovementInput.x,
            0f,
            input.MovementInput.y
        );

        direction.Normalize();

        Vector3 movement =
            direction *
            _moveSpeed *
            Runner.DeltaTime;

        _characterController.Move(movement);

        NetworkedPosition =
            transform.position;

        if (direction != Vector3.zero)
        {
            NetworkedRotation =
                Quaternion.LookRotation(direction);
        }
    }

    private void SmoothTransform()
    {
        transform.position =
            Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * 15f
            );

        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                NetworkedRotation,
                Runner.DeltaTime * 15f
            );
    }

    private void UpdateCooldown()
    {
        _pickupCooldown -= Runner.DeltaTime;
    }

    private bool CanInteract(
        PlayerInputData input
    )
    {
        return input.PickupPressed &&
               _pickupCooldown <= 0f;
    }

    private void Interact()
    {
        _pickupCooldown =
            PickupCooldownDuration;

        _interactionHandler.Interact();
    }

    private void UpdateVisual()
    {
        _heldItemVisual.UpdateVisual(
            HeldIngredient,
            HeldPotionState
        );
    }

    public bool HasIngredient()
    {
        return HeldIngredient !=
               IngredientType.None;
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
}