using UnityEngine;
using System.Collections;

public enum PlayerState
{
    Idle,
    MovingToCoin,
    ReturningHome,
    Celebrating 
}

public class PlayerFSM : MonoBehaviour
{
    [Header("References")]
    public Transform homeTransform;
    public CoinManager coinManager;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f; 

    private PlayerState currentState;
    private Transform currentTargetCoin;
    private MeshRenderer meshRenderer;
    private Color originalColor;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalColor = meshRenderer.material.color;

        currentState = PlayerState.Idle;
        RequestNewCoin();
    }

    void Update()
    {
        // FSM Logic
        switch (currentState)
        {
            case PlayerState.Idle:
                break;

            case PlayerState.MovingToCoin:
                HandleMovementToCoin();
                break;

            case PlayerState.ReturningHome:
                HandleReturnHome();
                break;

            case PlayerState.Celebrating:
                break;
        }
    }


    void RequestNewCoin()
    {
        GameObject newCoin = coinManager.SpawnCoin();
        currentTargetCoin = newCoin.transform;
        ChangeState(PlayerState.MovingToCoin);
    }

    void HandleMovementToCoin()
    {
        if (currentTargetCoin == null) return;

        transform.position = Vector3.MoveTowards(transform.position, currentTargetCoin.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentTargetCoin.position) < 0.1f)
        {
            Destroy(currentTargetCoin.gameObject);
            ChangeState(PlayerState.ReturningHome);
        }
    }

    void HandleReturnHome()
    {
        transform.position = Vector3.MoveTowards(transform.position, homeTransform.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, homeTransform.position) < 0.1f)
        {
            ChangeState(PlayerState.Celebrating);
        }
    }

    IEnumerator CelebrateRoutine()
    {
        meshRenderer.material.color = Color.yellow;

        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        meshRenderer.material.color = originalColor;
        transform.rotation = Quaternion.identity;

        ChangeState(PlayerState.Idle);
        RequestNewCoin();
    }

    void ChangeState(PlayerState newState)
    {
        currentState = newState;

        if (currentState == PlayerState.Celebrating)
        {
            StartCoroutine(CelebrateRoutine());
        }
    }
}