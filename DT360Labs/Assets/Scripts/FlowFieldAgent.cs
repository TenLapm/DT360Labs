using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FlowFieldAgent : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. Ask the Manager which way we should be walking
        Vector3 flowDirection = FlowField.Instance.GetFlowDirection(transform.position);
        FlowField.Instance.PrintAgentStatus(transform.position);
        if (flowDirection != Vector3.zero)
        {
            // 2. Rotate to face the flow direction
            Quaternion targetRotation = Quaternion.LookRotation(flowDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 3. Move forward (SimpleMove automatically applies gravity and handles terrain slopes!)
            controller.SimpleMove(flowDirection * moveSpeed);
        }
        else
        {
            // Apply gravity even if standing still
            controller.SimpleMove(Vector3.zero);
        }
    }
}