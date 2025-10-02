using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class CharacterMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc; //kcc: kinematic character controller
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpPower = 10f;

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 2f);
    }

    public override void FixedUpdateNetwork() //executing logic that affects gameplay
    {
        if (GetInput(out NetInputPlayer input)) //gets the input of each client
        {
            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y); //take the kcc transform rotation and we multiply it by the direction of the input
            float jump = 0f;

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded) //check if player pressed jump button and is grounded
            {
                jump = jumpPower;
            }

            kcc.Move(worldDirection.normalized * speed, jump); //normalizing the wD vector to prevent cheating
            PreviousButtons = input.Buttons;
        }  
    }
}
