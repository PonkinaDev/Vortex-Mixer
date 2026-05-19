using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _cc;

    [Networked]
    private Vector3 NetworkedPosition { get; set; }

    [Networked]
    private Quaternion NetworkedRotation { get; set; }

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        gameObject.name = HasInputAuthority
            ? "[Local] Player"
            : "[Remote] Player";
    }

    public override void FixedUpdateNetwork()
    {
        // IMPORTANTE:
        // SOLO el HOST mueve personajes
        if (!HasStateAuthority)
            return;

        // Obtenemos input del dueño
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

        // Movimiento real
        _cc.Move(movement);

        // Sincronizamos
        NetworkedPosition = transform.position;

        if (direction != Vector3.zero)
        {
            NetworkedRotation = Quaternion.LookRotation(direction);
        }
    }

    public override void Render()
    {
        // TODOS usan interpolación visual
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
    }
}